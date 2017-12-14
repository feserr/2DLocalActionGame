using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using emotitron.Network.Compression;
using emotitron.SmartVars;
using emotitron.BitToolsUtils;


namespace emotitron.Network.NST.Internal
{
	public enum Compression { None, HalfFloat, LocalRange } //, LocalRange, HalfFloat }

	//[System.Serializable]
	//public class RootPosition : PositionElement
	//{
	//	//public bool isRoot;

	//	public RootPosition()
	//	{
	//		isRoot = true;
	//		compression = Compression.Global;
	//		xrange = new AxisRange(0, -10, 10, 100);
	//		yrange = new AxisRange(1, -10, 10, 100);
	//		zrange = new AxisRange(2, -10, 10, 100);
	//	}
	//}

	[System.Serializable]

	public class PositionElement : TransformElement
	{
		//[HideInInspector]
		//public bool isRoot;

		[SerializeField]
		protected Compression compression;

		//[Tooltip("Enabling this reduces position data by not sending the higher order bits of the compressed positions unless they have changed. This can greatly reduce data usage on larger maps (such as a battlefield), and is not recommended for small maps (such as Pong). It does introduce the possibility of odd behavior from bad connections though.")]
		//[SerializeField]
		//private bool cullUpperBits = false;

		//public AxisRange[] axisRanges = new AxisRange[3] { new AxisRange(0), new AxisRange(1), new AxisRange(2) };
		//[Tooltip("0 = No extrapolation. 1 = Full extrapolation. Extrapolation occurs when the buffer runs out of frames. Without extrapolation the object will freeze if no new position updates have arrived in time. With extrapolation the object will continue in the direction it was heading as of the last update until a new update arrives.")]
		//[Range(0, 1)]
		//public float extrapolation = .5f;

		public AxisRange xrange;
		public AxisRange yrange;
		public AxisRange zrange;
		// indexer for the above
		[HideInInspector]
		public AxisRange[] axisRanges = new AxisRange[3];

		//[HideInInspector] public GenericXForm targetTranslate;
		//protected Vector3 snapshot;

		//private Vector3 lastSentPos;
		//private CompressedV3 lastSentCompPos;

		// Constructor
		public PositionElement()
		{
			elementType = ElementType.Position;
			compression = Compression.LocalRange;
			xrange = new AxisRange(0, -10, 10, 100);
			yrange = new AxisRange(1, -10, 10, 100);
			zrange = new AxisRange(2, -10, 10, 100);
		}

		//public PositionElement(bool _isRoot)
		//{
		//	isRoot = _isRoot;
		//	compression = Compression.Global;
		//	xrange = new AxisRange(0, -10, 10, 100);
		//	yrange = new AxisRange(1, -10, 10, 100);
		//	zrange = new AxisRange(2, -10, 10, 100);
		//}

		public override GenericX Localized
		{
			// Can modify this in the future to handle local/global positions if need be.
			get { return gameobject.transform.localPosition; }
			set	{ gameobject.transform.localPosition = value; }
		}

		public override void Initialize(NetworkSyncTransform _nst)
		{
			base.Initialize(_nst);

			axisRanges[0] = xrange;
			axisRanges[1] = yrange;
			axisRanges[2] = zrange;

			//prevTarget = target;
			target = Localized;
			snapshot = Localized;

			// Run the range math in advane
			for (int i = 0; i < 3; i++)
			{
				axisRanges[i].axis = i;

				if (!axisRanges[i].useAxis)
					continue;

				axisRanges[i].CalculateEncoder();

				lastSentCompressed = CompressElement();
			}
		}

		public static int[] highestChangedBit = new int[3];
		
		public CompressedElement CompressElement()
		{
			return CompressElement(Localized);
		}

		public CompressedElement CompressElement(GenericX uncompressed)
		{
			CompressedElement newCPos = new CompressedElement();

			for (int axis = 0; axis < 3; axis++)
				newCPos[axis] = (axisRanges[axis].useAxis) ?
					(compression == Compression.HalfFloat) ? SlimMath.HalfUtilities.Pack(uncompressed[axis]) :
					//(compression == Compression.Global) ? uncompressed[axis].CompressAxis(axis) :
					(compression == Compression.LocalRange) ? axisRanges[axis].Encode(uncompressed[axis]) :
					((UdpKit.UdpByteConverter)uncompressed[axis]).Unsigned32 : 0;

			return newCPos;
		}

		public override void WriteToBitstream(ref UdpKit.UdpBitStream bitstream, MsgType msgType, bool forceUpdate, bool isKeyframe)
		{
			// Compress the current element rotation using the selected compression method.
			CompressedElement newCPos = CompressElement();
			
			// For frames between forced updates, we need to first send a flag bit for if this element is being sent
			if (!forceUpdate)
			{
				bool hasChanged = !CompressedElement.Compare(newCPos, lastSentCompressed);
				bitstream.WriteBool(hasChanged);
				// if no changes have occured we are done.
				if (!hasChanged)
					return;
			}

			//TODO insert haschanged tests here
			for (int axis = 0; axis < 3; axis++)
				if (axisRanges[axis].useAxis)
				{
					highestChangedBit[axis] = CompressedElement.HighestDifferentBit(newCPos[axis], lastSentCompressed[axis]);
				}

			for (int axis = 0; axis < 3; axis++)
				if (axisRanges[axis].useAxis)
				{
					if (compression == Compression.HalfFloat)
					{
						bitstream.WriteUInt(newCPos[axis], 16);
					}

					//else if (compression == Compression.Global)
					//{
					//	newCPos[axis].WriteCompressedAxisToBitstream(axis, ref bitstream, (cullUpperBits && !isKeyframe));
					//}

					else if (compression == Compression.LocalRange)
					{
						bitstream.WriteUInt(newCPos[axis], axisRanges[axis].bits);
					}

					else
					{
						bitstream.WriteUInt(newCPos[axis], 32);
					}
				}

			lastSentCompressed = newCPos;
		}

		protected float[] xyz = new float[3];

		/// <param name="forcedUpdate">Indicates that this update is expected, so there will be no bool bit before it indicating whether it was sent.</param>
		/// <returns></returns>
		public override bool ReadFromBitstream(ref UdpKit.UdpBitStream bitstream, MsgType msgType, Frame targetFrame, int i, bool forcedUpdate, bool isKeyframe)
		{
			// Only read for the sent bit if not forced, there is no check bit for forced updates (since all clients and server know it is forced)
			bool hasChanged = forcedUpdate || bitstream.ReadBool();

			if (!hasChanged)
			{
				targetFrame.positions[i] = GenericX.NULL;
				return false;
			}

			for (int axis = 0; axis < 3; axis++)
				if (axisRanges[axis].useAxis)
				{
					xyz[axis] = 
						(compression == Compression.HalfFloat) ? bitstream.ReadHalf() :
						//(compression == Compression.Global) ? NSTCompressVector.ReadAxisFromBitstream(ref bitstream, axis, (cullUpperBits && !isKeyframe)) :
						(compression == Compression.LocalRange) ? axisRanges[axis].Decode(bitstream.ReadUInt(axisRanges[axis].bits)) :
						bitstream.ReadFloat();
				}

			targetFrame.positions[i] = new Vector3
				(
				(axisRanges[0].useAxis) ? xyz[0] : Localized[0],
				(axisRanges[1].useAxis) ? xyz[1] : Localized[1],
				(axisRanges[2].useAxis) ? xyz[2] : Localized[2]
				);

			return true;
		}

		
		///// <summary>
		///// Update the snapshot for an empty frame. Uses the last current target as the new snapshot rather than current rotation.
		///// </summary>
		//public void SnapshotEmpty()
		//{
		//	snapshot = target;
		//	snapshotTime = Time.time - Time.deltaTime;
		//}

		//public override void Snapshot(List<GenericX> elements, int i, bool lateUpdate = false)
		//{
		//	hasReceivedInitial = true;
		//	snapshot = target; // LocalizedRot;
		//	prevTarget = target;
		//	target = elements[i];

		//	// if this snapshot is being taken due to an overwrite of the current frame mid lerp, leave the time as is.
		//	if (!lateUpdate)
		//		snapshotTime = Time.time - Time.deltaTime;

		//}

		public void UpdateInterpolation(float endtime)
		{
			if (!hasReceivedInitial)
				return;

			float lerpTime = Mathf.InverseLerp(snapshotTime, endtime, Time.time);

			ApplyPosition(Vector3.Lerp(snapshot, target, lerpTime));
		}

		// Apply position without overwriting unused axis with zero.
		public void ApplyPosition(Vector3 pos)
		{
			//gameobject.transform.localPosition = pos;
			Localized = new Vector3(
				(axisRanges[0].useAxis) ? pos[0] : Localized[0],
				(axisRanges[1].useAxis) ? pos[1] : Localized[1],
				(axisRanges[2].useAxis) ? pos[2] : Localized[2]);

		}

		public void ApplyPosition(Vector3 pos, GameObject targetGO)
		{
			//targetGO.transform.localPosition = pos;
			targetGO.transform.localPosition = new Vector3(
				(axisRanges[0].useAxis) ? pos[0] : targetGO.transform.localPosition[0],
				(axisRanges[1].useAxis) ? pos[1] : targetGO.transform.localPosition[1],
				(axisRanges[2].useAxis) ? pos[2] : targetGO.transform.localPosition[2]);

		}
	}
}

