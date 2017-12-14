using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using emotitron.Network.NST.Internal;
using emotitron.SmartVars;

namespace emotitron.Network.NST

{
	public class FrameElements
	{
		public int packetid;
		public float endTime;
		public Vector3 rootPosition;
		public List<Vector3> positionsElements;
		public List<Quaternion> rotationElements;

		public FrameElements(int index, Vector3 _pos, List<PositionElement> positionElements, List<RotationElement> rotationElements)
		{
			packetid = index;
			rootPosition = _pos;
			positionsElements = new List<Vector3>(positionElements.Count);
			this.rotationElements = new List<Quaternion>(rotationElements.Count);

			for (int i = 0; i < positionElements.Count; i++)
				positionsElements.Add(positionElements[i].Localized);

			for (int i = 0; i < rotationElements.Count; i++)
				this.rotationElements.Add(rotationElements[i].Localized);
		}
	}

	public class Frame
	{
		public UnityEngine.Networking.NetworkConnection conn;
		public int packetid;
		public float packetArriveTime;
		public float appliedTime;
		public float endTime;
		public MsgType msgType;
		public CompressedElement compPos;
		public List<GenericX> positions;
		public List<GenericX> rotations;
		public ulong positionsMask;
		public ulong rotationsMask;
		public Vector3 pos;
		public byte[] customMsg;
		public int customMsgSize;


		// Construct
		public Frame(int index, Vector3 _pos, List<PositionElement> positionElements, List<RotationElement> rotationElements)
		{
			pos = _pos;
			compPos = pos.CompressPos();
			positions = new List<GenericX>(positionElements.Count);
			rotations = new List<GenericX>(rotationElements.Count);

			//Debug.LogWarning(" Construct Frame " + positionElements[i].LocalizedPos + " " + rotationElements[i].LocalizedRot);

			for (int i = 0; i < positionElements.Count; i++)
			{
				positions.Add(positionElements[i].Localized);
			}
			// Populate the rotation list so it can be used as an array.
			for (int i = 0; i < rotationElements.Count; i++)
			{
				rotations.Add(rotationElements[i].Localized);
			}

			packetid = index;
			customMsg = new byte[128];  //TODO: Make this size a user setting
		}

		public void ModifyFrame(MsgType _msgType, CompressedElement _compPos, Vector3 _pos, float _packetArrivedTime)
		{
			msgType = _msgType;
			compPos = _compPos;
			pos = _pos;
			packetArriveTime = _packetArrivedTime;
		}
		/// <summary>
		/// Guess the correct upperbits using the supplied frame for its compressedPos as a starting point. Will find the upperbits that result in the least movement from that pos. 
		/// </summary>
		public void CompletePosition(Frame frame)
		{
			if (msgType.IsPosLowerType())
				compPos = compPos.GuessUpperBits(frame.compPos);
			else if (!msgType.IsPosType())
				compPos = frame.compPos;

			//for (int i = 0l is < pre)

			pos = compPos.Decompress();
		}
	}


}
