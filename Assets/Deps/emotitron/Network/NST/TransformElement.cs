using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using emotitron.Network.Compression;
using emotitron.Network.NST.Internal;
using emotitron.BitToolsUtils;
using emotitron.Network.NST;
using emotitron.SmartVars;

public enum SendCulling { Always, ChangesOnly, ChangesAndEvents, EventsOnly }

public static class SendCullingExt
{
	public static bool ChangesType(this SendCulling v)
	{
		return (v == SendCulling.ChangesAndEvents || v == SendCulling.ChangesOnly);
	}
	public static bool EventsType(this SendCulling v)
	{
		return (v == SendCulling.ChangesAndEvents || v == SendCulling.EventsOnly);
	}
	public static bool UsesKeyframe(this SendCulling v)
	{
		return (v > 0);
	}
}

public enum ElementType { Position, Rotation }

[System.Serializable]
public abstract class TransformElement
{
	[SerializeField]
	protected bool isRoot;

	[HideInInspector] public NetworkSyncTransform nst;
	[HideInInspector] public ElementType elementType;

	public SendCulling sendCulling = SendCulling.ChangesAndEvents;

	#region Inspector Values

	[Range(0, 32)]
	public int keyRate = 5;
	public GameObject gameobject;

	[Tooltip("0 = No extrapolation. 1 = Full extrapolation. Extrapolation occurs when the buffer runs out of frames. Without extrapolation the object will freeze if no new position updates have arrived in time. With extrapolation the object will continue in the direction it was heading as of the last update until a new update arrives.")]
	[Range(0, 1)]
	public float extrapolation = .5f;

	#endregion

	[HideInInspector] public GameObject rewindGO;
	[HideInInspector] public float lastSentKeyTime;
	[HideInInspector] public float snapshotTime;
	[HideInInspector] public GenericX snapshot;

	//[HideInInspector] public GenericX prevTarget;
	[HideInInspector] public GenericX target;

	protected CompressedElement lastSentCompressed;

	protected bool hasReceivedInitial;

	public abstract GenericX Localized { get; set; }

	public virtual void Initialize(NetworkSyncTransform _nst)
	{
		nst = _nst;

		// if the gameobject wasn't set, use the root gameobject
		if (gameobject == null)
			gameobject = nst.gameObject;

	}

	/// <summary>
	/// Update the snapshot for an empty frame. Uses the last current target as the new snapshot rather than current rotation.
	/// </summary>
	public void SnapshotEmpty()
	{
		//prevTarget = target;
		target = Localized;
		snapshot = target;
		snapshotTime = Time.time - Time.deltaTime;
	}

	public virtual void Snapshot(List<GenericX> elements, int frameid, bool lateUpdate = false)
	{
		// This safety may no longer be needed. Watch for these errors.
		if (elements[frameid].type == XType.NULL)
		{
			target = Localized;
			snapshot = target;
			Debug.Log("Trying to snapshot a null element genericx ??? ");
			return;
		}

		// First run set both target and snapshot to the incoming.
		if (hasReceivedInitial == false)
		{
			target = elements[frameid];
			hasReceivedInitial = true;
		}

		snapshot = target; // LocalizedRot;
		target = elements[frameid];

		// if this snapshot is being taken due to an overwrite of the current frame mid lerp, leave the time as is.
		if (!lateUpdate)
			snapshotTime = Time.time - Time.deltaTime;
	}

	public abstract void WriteToBitstream(ref UdpKit.UdpBitStream bitstream, MsgType msgType, bool forceUpdate, bool keyframe);

	/// <summary>
	/// This is the logic for when a frame must be sent using info available to all clients/server, so in these cases elements do not need to send a "used" bit
	/// ahead of each element, since an update is required.
	/// </summary>
	protected bool IsUpdateForced (MsgType msgType, int packetId)
	{
		return
			sendCulling == SendCulling.Always ||
			(sendCulling == SendCulling.ChangesAndEvents && (msgType == MsgType.Cust_Msg || msgType == MsgType.Teleport)) ||
			(keyRate != 0 && packetId % keyRate == 0);
		//lastSentKeyTime + keyRate > Time.time;
	}
	protected bool IsKeyframe(int packetId)
	{
		return (keyRate != 0 && packetId % keyRate == 0);
	}

	public static void WriteAllElements<T>(ref UdpKit.UdpBitStream bitstream, MsgType msgType, FrameBuffer buffer, int packetId, List<T> elements) where T : TransformElement
	{
		Frame frame = buffer.frames[packetId];

		for (int i = 0; i < elements.Count; i++)
		{

			TransformElement e = elements[i];
			bool forcedUpdate = e.IsUpdateForced(msgType, packetId);
			bool isKeyframe = e.IsKeyframe(packetId);

			// Write this element to the stream if it is expected
			if (forcedUpdate || e.sendCulling.ChangesType())
				e.WriteToBitstream(ref bitstream, msgType, forcedUpdate, isKeyframe);

			// Store the current transform in the frame buffer if this has local authority TODO: (may not actually be used anywhere)
			if ((typeof(T) == typeof(PositionElement)))
				frame.positions[i] = (e as PositionElement).Localized;
			else
				frame.rotations[i] = (e as RotationElement).Localized;
		}
	}

	public abstract bool ReadFromBitstream(ref UdpKit.UdpBitStream bitstream, MsgType msgType, Frame targetframe, int i, bool forcedUpdate, bool keyframe);

	public static void ReadAllElements<T>(ref UdpKit.UdpBitStream bitstream, MsgType msgType, FrameBuffer buffer, int packetId, List<T> elements) where T : TransformElement
	{
		Frame frame = buffer.frames[packetId];
		bool isCurrentFrame = (packetId == buffer.CurrentIndex);
		bool isPos = ((typeof(T) == typeof(PositionElement)));

		for (int i = 0; i < elements.Count; i++)
		{
			TransformElement e = elements[i];

			bool forcedUpdate = e.IsUpdateForced(msgType, packetId);
			bool isKeyframe = e.IsKeyframe(packetId);
			
			// Read element from the buffer if it is expected
			if (forcedUpdate || e.sendCulling.ChangesType())
			{
				bool hasChanged = e.ReadFromBitstream(ref bitstream, msgType, frame, i, forcedUpdate, isKeyframe);

				// reapply snapshot if this frame is mid lerping (current), this is more accurate than the reconstructed rot being used.
				if (isCurrentFrame && hasChanged)
				{
					DebugX.Log("Mid lerp child update");
					if (isPos)
						e.Snapshot(frame.positions, i, true);
					else
						e.Snapshot(frame.rotations, i, true);
				}

				// Set the mask if this element sent an update
				if (isPos)
					i.SetBitInMask(ref frame.positionsMask, hasChanged);
				else
					i.SetBitInMask(ref frame.rotationsMask, hasChanged);
			}

		}
	}


	public GenericX Extrapolate(GenericX curr, GenericX prev)
	{
		//Debug.Log(Time.time +  " Extrapolating element for missing frame prev:" + prev + " curr " + curr + " res " + (curr + (curr - prev) * extrapolation));

		//if (prev == curr)
		//	Debug.Log(nst.name + " NO CHANGE ");

		return
			(extrapolation == 0) ? curr :
			curr + (curr - prev) * extrapolation;
	}
	public GenericX ExtrapolateRotation(GenericX curr, GenericX prev)
	{
		return new GenericX(
			(extrapolation == 0) ? (Quaternion)curr : QuaternionUtils.ExtrapolateQuaternion(prev, curr, 1 + extrapolation),
			//(extrapolation == 0) ? (Quaternion)curr : Quaternion.SlerpUnclamped(prev, curr, 1),// + extrapolation),
			curr.type);
	}

	//public static Quaternion ExtrapolateQuaternion (Quaternion a, Quaternion b, float t)
	//{
	//	Quaternion rot = b * Quaternion.Inverse(a);
		
	//	float ang = 0.0f;

	//	Vector3 axis = Vector3.zero;

	//	rot.ToAngleAxis(out ang, out axis);

	//	if (ang > 180)
	//		ang -= 360;

	//	ang = ang * t % 360;

	//	return Quaternion.AngleAxis(ang, axis) * a;
	//}

	public static void ApplyRewindToElements(NetworkSyncTransform nst, FrameElements rewind)
	{
		nst.rewindGO.transform.position = rewind.rootPosition;

		for (int i = 0; i < nst.positionElements.Count; i++)
		{
			nst.positionElements[i].ApplyPosition(rewind.positionsElements[i], nst.positionElements[i].rewindGO);
		}

		for (int i = 0; i < nst.rotationElements.Count; i++)
		{
			nst.rotationElements[i].ApplyRotation(rewind.rotationElements[i], nst.rotationElements[i].rewindGO);
		}
	}
}
