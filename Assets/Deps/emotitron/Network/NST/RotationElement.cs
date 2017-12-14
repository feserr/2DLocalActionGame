using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UdpKit;
using System;
using emotitron.Network.Compression;
using emotitron.SmartVars;
using emotitron.Network.NST.Internal;
using emotitron.BitToolsUtils;


namespace emotitron.Network.NST
{

	[System.Serializable]

	public class RotationElement : TransformElement
	{
		// Common
		public XType rotationType = XType.Quaternion;

		// Quaterion only
		[Range(16, 64)]
		public int totalBitsForQuat = 40;

		// Euler only X
		[Range(4, 32)]
		public int xBits = 11;
		public bool xLimitRange = false;
		public float xMinValue = 0;
		public float xMaxValue = 360;

		// Euler only Y
		[Range(4, 32)]
		public int yBits = 10;
		public bool yLimitRange = true;
		public float yMinValue = -90;
		public float yMaxValue = 90;

		// Euler only Z
		[Range(4, 32)]
		public int zBits = 10;
		public bool zLimitRange = false;
		[Range(-360, 360)] public float zMinValue = -90;
		[Range(-360, 360)] public float zMaxValue = 90;

		// Arrays for the inspector elements for easier looping
		[HideInInspector] public int[] xyzBits;
		[HideInInspector] public bool[] xyzLimit;
		[HideInInspector] public float[] xyzMin;
		[HideInInspector] public float[] xyzMax;

		// derived values for compression
		[HideInInspector] public float[] xyzRange;
		[HideInInspector] public float[] xyzMult;
		[HideInInspector] public float[] xyzUnmult;
		[HideInInspector] public float[] xyzWrappoint;

		public bool useLocal;

		//[HideInInspector] public GenericXForm targetTranslate;

		//private CompressedElement lastSentCompressed;

		private static int[] maxValue = new int[23]
		{ 0, 1, 3, 7, 15, 31, 63, 127, 255, 511, 1023, 2047, 4095, 8191, 16383,
			32767, 65535, 131071, 262143, 524287, 1048575, 2097151, 4194303 };

		public override void Initialize(NetworkSyncTransform _nst)
		{
			base.Initialize(_nst);

			target = Localized;
			snapshot = Localized;

			// move all of the inspector xyz values into arrays for easy looping
			xyzBits = new int[3];
			xyzBits[0] = xBits;
			xyzBits[1] = yBits;
			xyzBits[2] = zBits;

			xyzLimit = new bool[3];
			xyzLimit[0] = xLimitRange;
			xyzLimit[1] = yLimitRange;
			xyzLimit[2] = zLimitRange;

			xyzMin = new float[3];
			xyzMin[0] = xMinValue;
			xyzMin[1] = yMinValue;
			xyzMin[2] = zMinValue;

			xyzMax = new float[3];
			xyzMax[0] = xMaxValue;
			xyzMax[1] = yMaxValue;
			xyzMax[2] = zMaxValue;

			xyzRange = new float[3];
			xyzMult = new float[3];
			xyzUnmult = new float[3];
			xyzWrappoint = new float[3];

			// Clean up the ranges
			for (int i = 0; i < 3; i++)
			{
				if (xyzLimit[i])
				{
					if (xyzMax[i] < xyzMin[i])
						xyzMax[i] += 360;
					// If the range is greater than 360, get the max down into range. Likely user selected bad min/max values.
					if (xyzMax[i] - xyzMin[i] > 360)
						xyzMax[i] -= 360;
				}
				else
				{
					xyzMin[i] = 0;
					xyzMax[i] = 360;
				}

				xyzRange[i] = xyzMax[i] - xyzMin[i];
				// Do the heavier division work here so only one multipy per encode/decode is needed
				xyzMult[i] = (maxValue[xyzBits[i]]) / xyzRange[i];
				xyzUnmult[i] = xyzRange[i] / (maxValue[xyzBits[i]]);
				xyzWrappoint[i] = xyzRange[i] + (360 - xyzRange[i]) / 2;
			}
		}

		// Constructor
		public RotationElement(XType _xType)
		{
			rotationType = _xType;
			elementType = ElementType.Rotation;
		}

		// Shorthand to the rotation that accounts for local vs global rotation
		public override GenericX Localized
		{
			get
			{
				return (useLocal) ? gameobject.transform.localRotation : gameobject.transform.rotation;
			}
			set
			{
				if (useLocal)
					gameobject.transform.localRotation = value;
				else
					gameobject.transform.rotation = value;
			}
		}

		public override void WriteToBitstream(ref UdpBitStream bitstream, MsgType msgType, bool forceUpdate, bool isKeyframe)
		{
			// Base class does some forceUpdate checking, keep it around.
			//forceUpdate = base.WriteToBitstream(ref bitstream, msgType, forceUpdate);
			//bool hasChanged = false;

			if (rotationType == XType.Quaternion)
			{
				ulong compressedQuat = QuatCompress.CompressQuatToBitsBuffer(Localized, totalBitsForQuat);

				// For frames between forced updates, we need to first send a flag bit for if this element is being sent
				if (!forceUpdate)
				{
					bool hasChanged = compressedQuat != lastSentCompressed;
					bitstream.WriteBool(hasChanged);

					// if no changes have occured we are done.
					if (!hasChanged)
						return;
				}

				bitstream.WriteULong(compressedQuat, totalBitsForQuat);
				lastSentCompressed = compressedQuat;
				return;
			}

			else
			{
				// Euler types...

				CompressedElement newValues = new CompressedElement(0, 0, 0);

				// populate the new compressed position, and test if any of the axes have changed.
				for (int axis = 0; axis < 3; axis++)
				{
					if (rotationType.IsXYZ(axis))
					{
						newValues[axis] = CompressFloat(((Vector3)Localized)[axis], axis);
					}
				}

				// For frames between forced updates, we need to first send a flag bit for if this element is being sent
				if (!forceUpdate)
				{
					bool hasChanged = !CompressedElement.Compare(newValues, lastSentCompressed);
					bitstream.WriteBool(hasChanged);

					// if no changes have occured we are done.
					if (!hasChanged)
						return;
				}
				
				for (int axis = 0; axis < 3; axis++)
				{
					if (rotationType.IsXYZ(axis))
					{
						bitstream.WriteUInt(newValues[axis], xyzBits[axis]);
						lastSentCompressed[axis] = newValues[axis];
					}
				}
				
			}


		}


		public uint CompressFloat(float f, int axis)
		{
			float adjusted = f - xyzMin[axis];

			if (adjusted < 0)
				adjusted += 360;
			if (adjusted > 360)
				adjusted -= 360;

			// if f is out of range - clamp it
			if (adjusted > xyzRange[axis] && adjusted > xyzWrappoint[axis])
				return 0;

			if (adjusted > xyzRange[axis] && adjusted < xyzWrappoint[axis])
				return (uint)maxValue[xyzBits[axis]];

			// Clamp values TODO: probably shoud generate a warning if this happens.
			return (uint)(adjusted * xyzMult[axis]);
		}

		private float DecompressFloat(uint val, int i)
		{
			return xyzUnmult[i] + xyzMin[i];
		}

		public override bool ReadFromBitstream(ref UdpBitStream bitstream, MsgType msgType, Frame targetFrame, int i, bool forcedUpdate, bool isKeyframe)
		{
			// Only read for the sent bit if not forced, there is no check bit for forced updates (since all clients and server know it is forced)
			bool hasChanged = forcedUpdate || bitstream.ReadBool();

			if (!hasChanged)
			{
				targetFrame.rotations[i] = GenericX.NULL;
			}

			else if (rotationType == XType.Quaternion)
			{
				targetFrame.rotations[i] =  bitstream.ReadULong(totalBitsForQuat).DecompressBitBufferToQuat(totalBitsForQuat);
			}
			else
			{
				targetFrame.rotations[i] = 
				new GenericX(
					(rotationType.IsX()) ?
					(bitstream.ReadUInt(xyzBits[0]) * xyzUnmult[0] + xyzMin[0]) : 0,

					(rotationType.IsY()) ?
					(bitstream.ReadUInt(xyzBits[1]) * xyzUnmult[1] + xyzMin[1]) : 0,

					(rotationType.IsZ()) ?
					(bitstream.ReadUInt(xyzBits[2]) * xyzUnmult[2] + xyzMin[2]) : 0,

					rotationType);
			}

			return hasChanged;
		}

		public XType GetTypeFromUsedAxis(bool x, bool y, bool z)
		{
			return
				(x & y & z) ? XType.XYZ :
				(x & y) ? XType.XY :
				(x & z) ? XType.XZ :
				(y & z) ? XType.YZ :
				(x) ? XType.X :
				(y) ? XType.Y :
				(z) ? XType.Z :
				XType.NULL;
		}

		public void UpdateInterpolation(float endtime)
		{
			if (!hasReceivedInitial)
				return;

			float lerpTime = Mathf.InverseLerp(snapshotTime, endtime, Time.time);

			//TEST
			//ApplyRotation(targetRot);
			//gameobject.transform.eulerAngles = targetRot;

			ApplyRotation(Quaternion.Slerp(snapshot, target, lerpTime));
		}

		public void ApplyRotation(GenericX rot)
		{
			rot.ApplyRotation(gameobject.transform, useLocal);
		}
		public void ApplyRotation(GenericX rot, GameObject targetGO)
		{
			rot.ApplyRotation(targetGO.transform, useLocal);
		}
	}
}

