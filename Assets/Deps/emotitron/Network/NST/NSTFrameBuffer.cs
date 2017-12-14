//Copyright 2017, Davin Carten, All rights reserved

using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using emotitron.SmartVars;
using emotitron.Network.Compression;
using emotitron.BitToolsUtils;
using emotitron.Network.NST;

namespace emotitron.Network.NST.Internal
{
	public enum EType { Pos, Rot }

	

	// buffer array tied to an NST object
	public class FrameBuffer
	{
		public NetworkSyncTransform nst; // the owner of this buffer

		public Frame[] frames;
		private FrameElements[] rewind;

		public int bufferSize;
		public int halfBufferSize;
		public int quaterBufferSize;
		public ulong validFrameMask;

		public Frame currentFrame;
		public Frame prevAppliedFrame;
		public FrameElements latestRewindFrame;

		public float bufferAverageSize;
		public int numberOfSamplesInBufferAvg;

		// Construct
		public FrameBuffer(NetworkSyncTransform _nst, int _size, Vector3 _pos)
		{
			nst = _nst;

			bufferSize = _size;
			halfBufferSize = (bufferSize - 1) / 2;
			quaterBufferSize = bufferSize / 3;

			frames = new Frame[bufferSize];
			rewind = new FrameElements[bufferSize];

			for (int i = 0; i < bufferSize; i++)
			{
				frames[i] = new Frame(i, _pos, nst.positionElements, nst.rotationElements);
				rewind[i] = new FrameElements(i, _pos, nst.positionElements, nst.rotationElements); // currently unused
			}

			currentFrame = frames[0];
			prevAppliedFrame = frames[0];
			latestRewindFrame = rewind[0];
		}

		#region Rewind



		public FrameElements Rewind(float t)
		{
			if (NetworkServer.active == false)
			{
				DebugX.LogWarning("Attempting to use Rewind on a client. Are you sure this was your intention? Usually rewind is done on the server.");
			}

			float timeSinceRewindSnapshot = Time.time - latestRewindFrame.endTime;
			float timeBeforeSnapshot = t - timeSinceRewindSnapshot;
			float rewindByXFrames = timeBeforeSnapshot / NetworkSyncTransform.frameUpdateInterval;

			//TEST + 1
			int rewindByXWholeFrames = (int)rewindByXFrames;
			float remainder = rewindByXFrames - (int)rewindByXFrames;

			Vector3 targetPos;
			FrameElements lerpStartFrame;
			FrameElements lerpEndFrame;
			if (timeBeforeSnapshot > 0)
			{
				lerpStartFrame = rewind[Increment(latestRewindFrame.packetid, -(rewindByXWholeFrames + 1))];
				lerpEndFrame = rewind[Increment(lerpStartFrame.packetid, 1)];
				targetPos = Vector3.Lerp(lerpEndFrame.rootPosition, lerpStartFrame.rootPosition, remainder);
			}
			else
			{
				lerpStartFrame = latestRewindFrame;
				lerpEndFrame = null;
				targetPos = Vector3.Lerp(latestRewindFrame.rootPosition, nst.transform.position, -remainder);
			}
			
			rewind[0].rootPosition = targetPos;

			for (int i = 0; i < rewind[0].positionsElements.Count; i++)
			{
				rewind[0].positionsElements[i] = (timeBeforeSnapshot > 0) ?
					Vector3.Lerp(lerpEndFrame.positionsElements[i], lerpStartFrame.positionsElements[i], remainder) :
					Vector3.Lerp(latestRewindFrame.positionsElements[i], nst.positionElements[i].Localized, -remainder);
			}

			for (int i = 0; i < rewind[0].rotationElements.Count; i++)
			{
				rewind[0].rotationElements[i] = (timeBeforeSnapshot > 0) ?
					Quaternion.Slerp(lerpEndFrame.rotationElements[i], lerpStartFrame.rotationElements[i], remainder) :
					Quaternion.Slerp(latestRewindFrame.rotationElements[i], nst.rotationElements[i].Localized, -remainder);
			}

			return rewind[0];
		}

		public FrameElements Rewind(NetworkConnection conn, bool includeClientBuffer = true, bool includeClientInterpolation = true)
		{
			byte error = 0;

			// return half the current RTT for this connect. Hostid of -1 should mean that this is the host, so there should be no latency.
			float RTT = (conn == null || conn.hostId == -1) ? 0 :
				.001f * NetworkTransport.GetCurrentRTT(conn.hostId, conn.connectionId, out error);


			// TODO: these may already be factored in, think about this.
			float clientOffset =
				((includeClientBuffer && nst.hasAuthority) ? NSTSettings.single.desiredBufferMS : 0) +
				((includeClientInterpolation && nst.hasAuthority) ? NetworkSyncTransform.frameUpdateInterval : 0);

			return Rewind(RTT + clientOffset);
		}

		/// <summary>
		/// Apply this when the end of an interpolation has happened, or when the local object sends out its regular frame update.
		/// </summary>
		public void SnapshotToRewind(int index)
		{
			FrameElements frame = rewind[index];
			frame.endTime = Time.time;
			//frame.endTime = currentFrame.endTime; // Time.time;
			frame.rootPosition = nst.transform.position;

			for (int i = 0; i < frame.positionsElements.Count; i++)
			{
				frame.positionsElements[i] = nst.positionElements[i].Localized;
			}

			for (int i = 0; i < frame.rotationElements.Count; i++)
			{
				frame.rotationElements[i] = nst.rotationElements[i].Localized;
			}

			latestRewindFrame = frame;
		}

		#endregion

		/// <summary>
		/// Copy the current frame to the next frame - used when buffer is empty.
		/// </summary>
		public void CopyCurrentFrameToNext()
		{
			CopyFrame(currentFrame, NextFrame);
			//for (int i = 0; i < NextFrame.positions.Count; i++)
			//{
			//	NextFrame.positionsMask = 0;
			//	GenericX.Copy(nst.positionElements[i].targetGenX, NextFrame.positions[i]);
			//}

			//for (int i = 0; i < NextFrame.rotations.Count; i++)
			//{
			//	NextFrame.rotationsMask = 0;
			//	GenericX.Copy(nst.rotationElements[i].targetGenX, NextFrame.rotations[i]);
			//}
		}

		/// <summary>
		/// Copy one frame to another. Used when the buffer is empty and either need to copy the current frame or resort to the offtick frame.
		/// </summary>
		public void CopyFrame(Frame sourceFrame, Frame targetFrame, bool includePositions = true, bool includeRotations = true)
		{
			targetFrame.pos = sourceFrame.pos;

			targetFrame.compPos = sourceFrame.compPos;
			targetFrame.endTime = sourceFrame.endTime;
			targetFrame.appliedTime = sourceFrame.appliedTime;
			// Strip the custom message flag from copies, or they will fire twice.
			targetFrame.msgType = (sourceFrame.msgType == MsgType.Cust_Msg) ? MsgType.Position : sourceFrame.msgType;

			if (includePositions)
			{
				targetFrame.positionsMask = sourceFrame.positionsMask;
				for (int i = 0; i < targetFrame.positions.Count; i++)
				{
					GenericX.Copy(sourceFrame.positions[i], targetFrame.positions[i]);
					DebugX.Log(Time.time + " " + i + " Copy Source Pos " + sourceFrame.positions[i]);
				}
			}

			if (includeRotations)
			{
				targetFrame.rotationsMask = sourceFrame.rotationsMask;
				for (int i = 0; i < targetFrame.rotations.Count; i++)
				{
					GenericX.Copy(sourceFrame.rotations[i], targetFrame.rotations[i]);
					DebugX.Log(Time.time + " " + i + " Copy Source Rot " + sourceFrame.rotations[i]);
				}
			}
			targetFrame.packetArriveTime = Time.time;
		}

		public void ExtrapolateNextFrame() { ExtrapolateNextFrame(currentFrame, prevAppliedFrame, NextFrame); }

		public void ExtrapolateNextFrame(Frame curr, Frame prev, Frame target)
		{
			//target.compPos = CompressedV3.Extrapolate(curr.compPos, prev.compPos, nst.extrapolation);
			target.pos = Vector3.Lerp(curr.pos, curr.pos + (curr.pos - prev.pos), nst.extrapolation);

			DebugX.Log(Time.time + " " + nst.name + " <color=black>Extrapolated Missing Next Frame targ:" + target.compPos + " curr:" + curr.compPos + " prev" + prev.compPos + "</color>");

			target.compPos = target.pos.CompressPos();
			target.msgType = (curr.msgType == MsgType.Cust_Msg) ? MsgType.Position : curr.msgType;
			

			//TODO: need to limit the number of extrapolation iterations can occur.

			// Position Elements
			target.positionsMask = curr.positionsMask;
			for (int i = 0; i < target.positions.Count; i++)
			{
				GenericX currTarget = nst.positionElements[i].target;
				GenericX prevTarget = nst.positionElements[i].snapshot;

				target.positions[i] = nst.positionElements[i].Extrapolate(currTarget, prevTarget);
				//i.SetBitInMask(ref target.positionsMask, false);
			}
		
			// TODO: extrapolate rotations?
			target.rotationsMask = curr.rotationsMask;
			for (int i = 0; i < target.rotations.Count; i++)
			{
				GenericX currTarget = nst.rotationElements[i].target;
				GenericX prevTarget = nst.rotationElements[i].snapshot;
				//Debug.Log(" ExtrapolateNextFrame " + curr.rotations[i] + " -- > " + target.rotations[i]);
				target.rotations[i] = nst.rotationElements[i].ExtrapolateRotation(currTarget, prevTarget);
				//target.rotations[i] = nst.rotationElements[i].Extrapolate(curr.rotations[i], prev.rotations[i]);
				//TEST
				target.rotations[i] = QuaternionUtils.ExtrapolateQuaternion(prevTarget, currTarget, 2f);
				i.SetBitInMask(ref target.rotationsMask, true);

			}
		}

		public void SetBitInValidFrameMask(int bit, bool b)
		{
			bit.SetBitInMask(ref validFrameMask, b);
		}



		public bool GetBitInValidFrameMask(int bit)
		{
			return validFrameMask.GetBitInMask(bit);
		}

		public void AddFrameToBuffer(MsgType msgType, CompressedElement compPos, int packetid)
		{
			// If we are writing to the frame as it is mid-lerp - apply the correct upperbits.
			if (packetid == CurrentIndex && msgType.IsPosLowerType())
			{
				compPos = NSTCompressVector.GuessUpperBits(compPos, currentFrame.compPos); 
			}

			int numOfFramesFromCurrent = CountFrames(CurrentIndex, packetid);
			// is this frame still a future event for interpolation, or has it already just guessed it?
			bool isStillPending = numOfFramesFromCurrent < halfBufferSize && numOfFramesFromCurrent > 0;
			bool isCurrentFrame = CurrentIndex == packetid;
			
			frames[packetid].ModifyFrame(msgType, compPos, compPos.Decompress(), Time.time);

			// Set as valid if 1. is not the frame currently rendering 2. is not in the past, unless the buffer is empty then we need to rewind
			SetBitInValidFrameMask(packetid, !isCurrentFrame && (isStillPending || validFrameMask == 0));
		}

		/// <summary>
		/// Determine the difference in count between two packet counts - accounting for the range being 1-X
		/// </summary>
		/// <returns> </returns>
		public int CountFrames(int firstIndex, int secondIndex)
		{
			// zero packets are reserved for indicating a teleport/fire event
			if (secondIndex == 0 || firstIndex == 0)
				return 1;

			// if the new index is lower, convert it to what it would have been had it not wrapped back around.
			if (secondIndex < firstIndex)
				secondIndex += (frames.Length - 1); // zero is skipped so we account for that here with the -1

			int numOfIndexes = secondIndex - firstIndex;

			return numOfIndexes;
		}

		public const int AVG_BUFFER_MAX_SAMPLES = 10;

		//TODO: need to make this try and assess missing frames as best as possible to survive loss and out of order packets.
		public void AddTimeToBufferAverage(float newTime)
		{
			bufferAverageSize = (bufferAverageSize * numberOfSamplesInBufferAvg + newTime) / (numberOfSamplesInBufferAvg + 1);
			numberOfSamplesInBufferAvg = Mathf.Min(numberOfSamplesInBufferAvg + 1, AVG_BUFFER_MAX_SAMPLES);
		}

		public void UpdateBufferAverage()
		{
			if (CurrentIndex == 0)
				return;

			//int oldest = 0;
			//int newest = 0;

			//for (int i = halfBufferSize - 1; i > 0; i --)
			//{
			//	if (GetBitInValidFrameMask(IncrementFrame(CurrentIndex, i).packetid))
			//	{
			//		newest = i;
			//		break;
			//	}
			//}
			//for (int i = - (halfBufferSize - 1); i < 0; i++)
			//{
			//	if (GetBitInValidFrameMask(IncrementFrame(CurrentIndex, i).packetid))
			//	{
			//		oldest = i;
			//		break;
			//	}
			//}
			//int steps = newest - oldest;

			//float buffersize = steps * NetworkSyncTransform.frameUpdateInterval + Mathf.Clamp(currentFrame.endTime - Time.time, 0, NetworkSyncTransform.frameUpdateInterval);

			// add items in queue with remaining seconds left in the current frame
			AddTimeToBufferAverage(CurrentBufferSize);
		}

		public float CurrentBufferSize
		{
			get
			{
				//int oldest = 0;
				//int newest = 0;

				//for (int i = halfBufferSize - 1; i > 0; i--)
				//{
				//	if (GetBitInValidFrameMask(IncrementFrame(CurrentIndex, i).packetid))
				//	{
				//		newest = i;
				//		break;
				//	}
				//}
				//for (int i = -(halfBufferSize - 1); i < 0; i++)
				//{
				//	if (GetBitInValidFrameMask(IncrementFrame(CurrentIndex, i).packetid))
				//	{
				//		oldest = i;
				//		break;
				//	}
				//}
				//int steps = newest - oldest;

				int steps = validFrameMask.CountTrueBits();
				return steps * NetworkSyncTransform.frameUpdateInterval + Mathf.Clamp(currentFrame.endTime - Time.time, 0, NetworkSyncTransform.frameUpdateInterval);
			}
		}

		public int CurrentIndex
		{
			get { return currentFrame.packetid; }
		}

		public int GetNextIndex
		{
			get
			{
				int next = currentFrame.packetid + 1;
				if (next >= frames.Length)
					next -= frames.Length - 1;

				return next;
			}
		}

		public int GetPrevIndex
		{
			get
			{
				int previndex = currentFrame.packetid - 1;
				if (previndex < 1)
					previndex = frames.Length - 1;

				return previndex;
			}
		}

		public Frame NextFrame { get { return frames[GetNextIndex]; } }
		public Frame PrevFrame { get { return frames[GetPrevIndex]; } }

		public Frame IncrementFrame(int startingId, int increment)
		{
			return frames[Increment(startingId, increment)];
		}
		
		public int Increment(int startIndex, int increment)
		{
			int newIndex = startIndex + increment;

			while (newIndex >= bufferSize)
				newIndex -= (bufferSize - 1);

			while (newIndex < 1)
				newIndex += (bufferSize - 1);

			return newIndex;
		}

		/// <summary>
		/// Find frame in buffer x increments from the given frame.
		/// </summary>
		public Frame IncrementFrame(Frame startingFrame, int increment)
		{
			return IncrementFrame(startingFrame.packetid, increment);
		}

		/// <summary>
		/// Returns the previous keyframe closest to the specified frame, if none can be found returns the current frame.
		/// </summary>
		/// <param name="index"></param>
		/// <returns>Returns current frame if no keyframes are found.</returns>
		public Frame BestPreviousKeyframe(int index)
		{
			//// First try to get best keyframe
			for (int i = 1; i < halfBufferSize; i++)
			{
				int offsetIndex = index - i;
				int correctedi = (offsetIndex < 1) ? offsetIndex + frames.Length - 1 : offsetIndex;
				Frame frame = frames[correctedi];

				if (frame.msgType.IsPosKeyType() &&
					Time.time - frame.packetArriveTime < Time.fixedDeltaTime * nst.sendEveryXTick * (i + quaterBufferSize)) // rough estimate that the frame came in this round and isn't a full buffer cycle old
				{
					return frame;
				}
			}

			DebugX.LogWarning(Time.time + " NST:" + nst.NstId + " " + nst.name + " <color=black>Could not find a recent keyframe in the buffer history, likely very bad internet loss is responsible. Some erratic player movement is possible.</color>");

			return currentFrame;
		}

		public Frame DetermineNextFrame()
		{
			// buffer is empty, no point looking for any frames
			if (validFrameMask <= 1)
			{
				DebugX.Log(Time.time + " NST " + nst.NstId + " " + nst.name + " <color=red><b> empty buffer, copying current frame </b></color>" + NextFrame.packetid);
				ExtrapolateNextFrame();
				return NextFrame;
			}

			// First see if there is a future frame ready
			Frame nextValid = GetFirstFutureValidFrame();

			// if not see if there is an older frame that arrived late, if so we will jump back to that as current
			if (nextValid == null)
			{
				nextValid = GetOldestPastValidFrame();

				// Valid frames are only in the past, we need to jump back to that packetindex
				if (nextValid != null)
				{
					DebugX.Log(Time.time + " NST " + nst.NstId + " " + nst.name + " <color=red><b> Skipping back to older frame </b></color> " + nextValid.packetid);
					nextValid.CompletePosition(currentFrame);
					return nextValid;
				}
				// No future or past frames found - Look everywhere as a last ditch check in case we are way out of sync

				nextValid = GetOldestValidFrame();
				if (nextValid != null)
				{
					DebugX.Log(Time.time + " NST " + nst.NstId + " " + nst.name + " <color=red><b> Skipping to out of sequence frame </b></color> " + nextValid.packetid);
					nextValid.CompletePosition(currentFrame); // of questionable value for a frame this out of sequence, but better than 0 position.
					return nextValid;
				}
			}

			// Find out how far in the future the next valid frame is, need to know this for the reconstruction lerp.
			int stepsFromLast = CountFrames(CurrentIndex, nextValid.packetid);

			// The next frame is the next valid... not much thinking required... just use it.
			if (stepsFromLast == 1)
			{
				InvalidateOldFrames(NextFrame);
				NextFrame.CompletePosition(currentFrame);
				return NextFrame;
			}

			if (stepsFromLast > 2) // arbitrary number... should refine this
			{
				InvalidateOldFrames(nextValid);
				nextValid.CompletePosition(currentFrame);
				return nextValid;
			}
			
			//All other cases we Reconstruct missing next frame
			Frame next = NextFrame;

			DebugX.Log(Time.time +  " NST:" + nst.NstId + " <color=black><b>Reconstructing missing packet " + next.packetid + " </b></color>");

			float t = 1f / stepsFromLast;

			nextValid.CompletePosition(currentFrame);
			CompressedElement lerpedCompPos = Vector3.Lerp(currentFrame.pos, nextValid.pos, t).CompressPos(); // TODO: should this be v3 lerps instead to avoid the rounding error?
			// TEST
			//CompressedPos lerpedCompPos = nextValid.compPos; // TODO: should this be v3 lerps instead to avoid the rounding error?

			float lerpedStartTime = Mathf.Lerp(currentFrame.packetArriveTime, nextValid.packetArriveTime, t);
			next.ModifyFrame(currentFrame.msgType, lerpedCompPos, lerpedCompPos.Decompress(), lerpedStartTime);

			// Reconstruct missing Position Element
			for (int i = 0; i < next.positions.Count; i++)
			{
				GenericX currentTargetX = nst.positionElements[i].target;

				if (BitTools.GetBitInMask(nextValid.positionsMask, i))
				{

					next.positions[i] = (Vector3.Lerp(currentTargetX, nextValid.positions[i], t));
					i.SetBitInMask(ref next.positionsMask, true);
				}
				else
				{
					next.positions[i] = currentTargetX;
					i.SetBitInMask(ref next.rotationsMask, false);
				}
			}

			// Reconstruct missing Rotation Element
			for (int i = 0; i < next.rotations.Count; i++)
			{
				GenericX currentTargetX = nst.rotationElements[i].target;

				if (BitTools.GetBitInMask(nextValid.rotationsMask, i))
				{

					if (currentTargetX.type == XType.NULL)
						Debug.LogError(nst.name + "  Cloning NULL GenX element " + i + "  " + currentTargetX + " - which should never happen, this would be a bug.");

					next.rotations[i] = (Quaternion.Slerp(currentTargetX, nextValid.rotations[i], t));

					//TEST
					next.rotations[i] = currentTargetX;
					i.SetBitInMask(ref next.rotationsMask, true);
				}
				else
				{
					next.rotations[i] = nst.rotationElements[i].target; // maybe unneeded
					i.SetBitInMask(ref next.rotationsMask, false);
				}
			}
			
			return next;
		}

		public void InvalidateOldFrames (Frame startingframe)
		{
			for (int i = -quaterBufferSize; i < 0; i++)
			{
				SetBitInValidFrameMask(IncrementFrame(startingframe, i).packetid, false);
			}
		}

		/// <summary>
		/// Checks ENTIRE buffer for the oldest arriving frame. Used for the starting up.
		/// </summary>
		/// <returns>Returns null if no valid frames are found.</returns>
		public Frame GetOldestValidFrame()
		{
			if (validFrameMask <= 1)
				return null;

			float timetobeat = Time.time;
			int winnerwinnerchickendinner = 0;

			// First look forward
			for (int i = 1; i < bufferSize; i++)
			{
				//Frame testframe = IncrementFrame(CurrentIndex, i);
				if (GetBitInValidFrameMask(i) && frames[i].packetArriveTime < timetobeat )
				{
					winnerwinnerchickendinner = i;
					timetobeat = frames[i].packetArriveTime;
				}
			}
			return frames[winnerwinnerchickendinner];
		}

		/// <summary>
		/// Looks for farthest back valid frame before the current frame, starting with a quater buffer length behind working up to the current frame.
		/// </summary>
		/// <returns>Returns null if none found.</returns>
		public Frame GetOldestPastValidFrame()
		{
			for (int i = -quaterBufferSize; i < 0; i++)
			{
				Frame testframe = IncrementFrame(CurrentIndex, i);
				if (GetBitInValidFrameMask(testframe.packetid))
				{
					return testframe;
				}
			}
			return null;
		}

		/// <summary>
		/// Get the first valid frame BEFORE the current frame.
		/// </summary>
		public Frame GetNewestPastValidFrame()
		{
			for (int i = -1; i >= -quaterBufferSize; i--)
			{
				Frame testframe = IncrementFrame(CurrentIndex, i);
				if (GetBitInValidFrameMask(testframe.packetid))
				{
					return testframe;
				}
			}
			return null;
		}

		/// <summary>
		/// Looks for first valid frame AFTER the current frame.
		/// </summary>
		/// <returns>Returns null if none found.</returns>
		public Frame GetFirstFutureValidFrame()
		{
			for (int i = 1; i <= quaterBufferSize; i++)
			{
				Frame testframe = IncrementFrame(CurrentIndex, i);
				if (GetBitInValidFrameMask(testframe.packetid))
				{
					return testframe;
				}
			}
			return null;
		}

		public Frame GetFurthestFutureValidFrame(Frame startingframe = null)
		{
			if (startingframe == null)
				startingframe = currentFrame;

			for (int i = quaterBufferSize; i > 0; i++)
			{
				Frame testframe = IncrementFrame(startingframe, i);
				if (GetBitInValidFrameMask(testframe.packetid))
				{
					return testframe;
				}
			}
			return null;
		}
	}
}
