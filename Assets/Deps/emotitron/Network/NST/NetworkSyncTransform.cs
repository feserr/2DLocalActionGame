//Copyright 2017, Davin Carten, All rights reserved

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using emotitron.Network.Compression;
using emotitron.BitToolsUtils;
using emotitron.SmartVars;
using emotitron.Network.NST.Internal;

namespace emotitron.Network.NST
{
	
	public enum DebugXform { None, LocalSend, RawReceive, Uninterpolated, RewindAdd, RewindOneSec, RewindHalfSec, RewindTenthSec, RewindHundrethSec, RewindZeroSec, RewindLastJoined }
	public enum RootPositionSync { Disabled, Enabled }

	[AddComponentMenu("Network Sync Transform/Network Sync Transform")]
	
	public class NetworkSyncTransform : NetworkBehaviour
	{
		//TEST
		private static NetworkConnection lastJoinedPlayerConn;

		#region Inspector Vars

		[Range(1, 5)]
		[Tooltip("3 is the default. 1 sends updates every fixed update. 3 every 3rd. This and the physics fixedtime effectively define your tickrate.")]
		public int sendEveryXTick = 3;


		[Tooltip("These layers will be cloned onto the rewind collision object. Only include layers that will act as hitboxes.")]
		[SerializeField]
		public LayerMask rewindHitLayers = ~0;

		//public Collider rewindCollider;
		[HideInInspector] public GameObject rewindGO;
		[Tooltip("Let NST guess the best settings for isKinematic and interpolation for your rigidbodies on server/client/localplayer. Turn this off if you want to set them yourself in your own code.")]
		[SerializeField]
		private bool autoKinematic = true;

		[Header("Root Position Updates")]


		[SerializeField]
		public RootPositionSync rootPosSync = RootPositionSync.Enabled;

		public XYZBool includeXYZ = new XYZBool { x = true, y = true, z = true };

		public SendCulling sendCulling = SendCulling.Always;


		[Range(0f, 16f)]
		[Tooltip("How often to force a position keyframe. These ensure that with network errors or newly joined players objects will not remain out of sync long.")]
		public int keyEvery = 5;

		[Tooltip("0 = No extrapolation. 1 = Full extrapolation. Extrapolation occurs when the buffer runs out of frames. Without extrapolation the object will freeze if no new position updates have arrived in time. With extrapolation the object will continue in the direction it was heading as of the last update until a new update arrives.")]
		[Range(0,1)]
		public float extrapolation = .5f;

		[Tooltip("A change in postion greater than this distance in units will treat the move as a teleport. This means the object will move to that location without any tweening.")]
		[SerializeField]
		private float teleportThreshold = 1;


		[Header("Root Position Upper Bit Culling")]
		[Tooltip("Enabling this reduces position data by not sending the higher order bits of the compressed positions unless they have changed. This can greatly reduce data usage on larger maps (such as a battlefield), and is not recommended for small maps (such as Pong). It does introduce the possibility of odd behavior from bad connections though.")]
		public bool cullUpperBits = false;

		[Tooltip("When using upper bit culling, this value dictates how many full frames in a row will be sent after upper bits have changed. The higher this number the lower the risk of lost packets creating mayhem. Too high and you will end up with nothing but keyframes.")]
		[SerializeField]
		[Range(1, 10)]
		private int sequentialKeys = 5;

		[Space]

		[SerializeField]
		public List<PositionElement> positionElements = new List<PositionElement>() ;
		public List<RotationElement> rotationElements = new List<RotationElement>() { new RotationElement(XType.Quaternion) };

		//[Space]


		[Header("Debuging")]
		[SerializeField] public DebugXform debugXform;
		[HideInInspector] public GameObject debugXformGO;

		#endregion

		#region NstId Syncvar and methods

		// There are two storage vehicles. The dictionary is used for Unlimited and the NST[] for smaller numbers (currently 5 bits / 32 objects)
		public static Dictionary<uint, NetworkSyncTransform> nstIdToNSTLookup = new Dictionary<uint, NetworkSyncTransform>();
		public static NetworkSyncTransform[] NstIds;
		public static NetworkSyncTransform lclNST;
		public static List<NetworkSyncTransform> allNsts = new List<NetworkSyncTransform>();

		// Public methods for looking up game objects by the NstID
		public static NetworkSyncTransform GetNstFromId(uint id)
		{
			// 5 bits (32 objects) is the arbitrary cutoff point for using an array for the lookup. For greater numbers the dictionary is used instead.
			if (NSTSettings.single.bitsForNstId > 5)
				return nstIdToNSTLookup[id];
			else
				return NstIds[(int)id];
		}

		[SyncVar] [HideInInspector]
		private uint _nstIdSyncvar;
		public uint NstId
		{
			get { return _nstIdSyncvar; }
			private set { _nstIdSyncvar = value; }
		}

		private Frame CurrentFrame
		{
			get { return buffer.currentFrame; }
			set { buffer.currentFrame = value; }
		}

		private int CurrentIndex
		{
			get { return buffer.currentFrame.packetid; }
		}

		#endregion

		#region Rewind

		/// <summary>
		/// Enter the number of seconds you want to look back in time for this object.
		/// </summary>
		/// <returns>Returns a recycled Frame object. Contents of this will be overwritten with the next rewind request for this object, so use the contents immediately or copy them.</returns>
		public FrameElements Rewind(float seconds)
		{
			return buffer.Rewind(seconds);
		}

		/// <summary>
		/// Returns a Frame object. The rewind time is calculated by using half of the RTT to that supplied networkconnection.
		/// </summary>
		/// <param name="conn"></param>
		/// <returns></returns>
		public FrameElements Rewind(NetworkConnection conn)
		{
			return buffer.Rewind(conn);
		}

		public bool TestHitscanAgainstRewind(NetworkSyncTransform rewindForNST, Ray ray) { return TestHitscanAgainstRewind(rewindForNST.NI.clientAuthorityOwner, ray); }
		public bool TestHitscanAgainstRewind(NetworkConnection rewindForConn, Ray ray)
		{
			if (NSTSettings.single.rewindLayer.value == -1)
			{
				DebugX.LogError("Attempting to use Rewind without a dedicated physics layer named NSTRewind. You must add a dedicated physics layer named <b>'NSTRewind'</b> to <b>Edit/Project Settings/Tags and Layers</b>, or make sure there is an unused physics layer in order to make use of rewind ray test methods.");
				return false;
			}

			FrameElements rewindFrame = Rewind(rewindForConn);

			if (rewindFrame == null)
				return false;
			
			TransformElement.ApplyRewindToElements(this, rewindFrame);

			// Conduct the hit test.
			rewindGO.SetActive(true);
			RaycastHit hit;
			bool wasHit = Physics.Raycast(ray, out hit, 100f,  NSTSettings.single.rewindLayerMask);
			rewindGO.SetActive(false);
			
			DebugX.Log("Rewind Raycast " + (wasHit ? ("Hit " + hit.collider.name) : ("No Hit")));

			return wasHit;
		}

		// TODO: Make rewind method for testing against all - will need some kind of prealloc for the hits
		public static void TestHitscanAgainstRewindAll(NetworkConnection conn, Ray ray)
		{
			for (int i = 0; i < allNsts.Count; i++)
			{
				allNsts[i].TestHitscanAgainstRewind(conn, ray);
			}
		}
		//TODO: move to transformelement base?
		//public void ApplyRewindToObject(RewindFrame rewindframe)
		//{
		//	rewindGO.transform.position = rewindframe.pos;

		//	for (int i = 0; i < positionElements.Count; i++)
		//	{
		//		//if (positionElements[i].rewindGO != null)
		//		//{
		//			positionElements[i].ApplyPosition(rewindframe.positions[i], positionElements[i].rewindGO);
		//		//}
		//	}

		//	for (int i = 0; i < rotationElements.Count; i++)
		//	{
		//		//if (rotationElements[i].rewindGO != null)
		//		//{
		//			rotationElements[i].ApplyRotation(rewindframe.rotations[i], rotationElements[i].rewindGO);
		//		//}
		//		//// if no rotation go is set, it defaults to rotating the root
		//		//else
		//		//	rewindframe.rotations[i].ApplyRotation(rewindGO.transform, rotationElements[i].useLocal);
		//	}
		//}

		#endregion

		#region Event Delegates

		// Generates this event when extra data events have been send. Use it to get actual send pos/rot when the tick happens, and with the compression errors accounted for.
		public delegate void OnCustomMsgSndEventDelegate(NetworkConnection nstOwnerConn, byte[] bytearray, NetworkSyncTransform nst, Vector3 pos, List<GenericX> positions, List<GenericX> rots);
		public static OnCustomMsgSndEventDelegate OnCustomMsgSndEvent;

		// Generates this event when extra data events have arrived (usually should be weapon fire)
		public delegate void OnCustomMsgRcvEventDelegate(NetworkConnection nstOwnerConn, byte[] bytearray, NetworkSyncTransform nst, Vector3 pos, List<GenericX> positions, List<GenericX> rots);
		public static OnCustomMsgRcvEventDelegate OnCustomMsgRcvEvent;

		// Generates this event when frame containing custom data is applied from the buffer queue (or if offtick just when it arrives). 
		// Unlike OnCustomMsgRcvEvent this happens after the frame buffer.
		public delegate void OnCustomMsgBeginInterpolationDelegate(NetworkConnection nstOwnerConn, byte[] bytearray, NetworkSyncTransform nst, Vector3 pos, List<GenericX> positions, List<GenericX> rots);
		public static OnCustomMsgBeginInterpolationDelegate OnCustomMsgBeginInterpolationEvent;

		// Generates event when custom message frame reaches the end of its interpolation (or if offtick just when it arrives).
		public delegate void OnCustomMsgEndInterpolationDelegate(NetworkConnection nstOwnerConn, byte[] bytearray, NetworkSyncTransform nst, Vector3 pos, List<GenericX> positions, List<GenericX> rots);
		public static OnCustomMsgEndInterpolationDelegate OnCustomMsgEndInterpolationEvent;

		#endregion



		#region RTT Tracker

		//private RTT_Tracker rttTracker = new RTT_Tracker();

		#endregion

		#region Startup and Initialization

		// Cached Components
		[HideInInspector] public Rigidbody rb;
		[HideInInspector] public NetworkIdentity NI;

		private void Awake()
		{
			// Ensure an NSTSettings exists in case the developer completely missed this step somehow.
			NSTSettings.EnsureSettingsExistsInScene();

			// Cache components
			NI = GetComponent<NetworkIdentity>();
			rb = GetComponent<Rigidbody>();

			// initialize the RotationElements since they can't construct properly from this monobehavior
			for (int i = 0; i < positionElements.Count; i++)
			{
				positionElements[i].Initialize(this);
			}

			for (int i = 0; i < rotationElements.Count; i++)
			{
				rotationElements[i].Initialize(this);
			}

			buffer = new FrameBuffer(this, NSTSettings.single.packetCounterRange, transform.position);

			// determine the update interval based on the current physics clock rate.
			frameUpdateInterval = Time.fixedDeltaTime * sendEveryXTick;
		}

		public override void OnStartServer()
		{
			Initialize();
			InitializeServerMsgHandlers();

			lastJoinedPlayerConn = connectionToClient;

			// Create Rewind object (TODO: move this to helper?)
			if (NSTSettings.single.useRewindColliders)
			{
				if (NSTSettings.single.rewindLayer.value == -1)
				{
					DebugX.LogError("Rewind Colliders is enabled in NSTSettings, but no 'NSTRewind' layer has been created for it. You must add a dedicated physics layer named <b>'NSTRewind'</b> to <b>Edit/Project Settings/Tags and Layers</b> in order to make use of rewind ray test methods.");
					return;
				}

				rewindGO = NSTHelper.CreateRewindObject(this, gameObject);
				LayerAndTagTools.SetLayerRecursively(rewindGO, NSTSettings.single.rewindLayer);
					
				//}
			}

		}

		public override void OnStartClient()
		{
			if (!isServer)
				Initialize();

			// Be super sure the HLAPI didn't take a dump and set the nstID on clients if in unlimited mode.
			if (NSTSettings.single.bitsForNstId == 32)// MaxNstObjects.Unlimited)
				_nstIdSyncvar = NI.netId.Value;

			InitializeClientMsgHandlers();
		}

		private void Initialize()
		{
			// If the nstid array is null - it need to be created. Leave the nst array null for unlimited - that uses the dictionary.
			if (NstIds == null && NSTSettings.single.bitsForNstId < 6)
				NstIds = new NetworkSyncTransform[NSTSettings.single.MaxNSTObjects];

			// Server needs to set the syncvar
			if (isServer)
			{
				if (NSTSettings.single.bitsForNstId < 6)
					_nstIdSyncvar = (uint)NSTHelper.GetFreeNstId();
				else
					_nstIdSyncvar = NI.netId.Value;
			}

			if (NSTSettings.single.bitsForNstId < 6)
				NstIds[_nstIdSyncvar] = this;

			else if (!nstIdToNSTLookup.ContainsKey(_nstIdSyncvar))
				nstIdToNSTLookup.Add(_nstIdSyncvar, this);

			allNsts.Add(this);

			if (debugXform != DebugXform.None)
				debugXformGO = DebugWidget.CreateDebugCross();

			ApplyTeleportLocally(transform.position);
		}

		public override void OnStartLocalPlayer()
		{
			lclNST = this;
			
		}

		private void Start()
		{
			// Automatically determine what the kinematic and interpolations settings should be
			if (autoKinematic && rb != null)
			{
				rb.isKinematic = (!hasAuthority);
				rb.interpolation = (rb.isKinematic) ? RigidbodyInterpolation.None : RigidbodyInterpolation.Interpolate;
			}
			
		}

		private void OnDestroy()
		{
			// Likely this never even initialized and is being destroyed at startup because it shouldn't be in the scene
			if (NI == null)
				return;

			if (nstIdToNSTLookup != null && nstIdToNSTLookup.ContainsKey(NI.netId.Value))
				nstIdToNSTLookup.Remove(NI.netId.Value);

			if (NstIds != null)
				NstIds[_nstIdSyncvar] = null;

			if (allNsts != null && allNsts.Contains(this))
				allNsts.Remove(this);
		}
		
		#endregion

		#region Updates

		private int fixedUpdateSkipsCount;
		private bool frameTransmitDue;

		// Local Player sends its transform updates based on the fixed update
		void FixedUpdate()
		{
			if (!hasAuthority)
				return;

			// flag an update as being due every X number of fixedUpdates
			fixedUpdateSkipsCount++;
			if (fixedUpdateSkipsCount >= sendEveryXTick)
			{
				frameTransmitDue = true;
				fixedUpdateSkipsCount = 0;
			}
		}

		private void LateUpdate()
		{
			if (!hasAuthority)
			{
				InterpolateTransform();
			}

			// this is in lateupdate to make sure all incoming msgs and fire commands have resolved
			if (hasAuthority)
			{
				if (frameTransmitDue)
					ClientGenerateRegularUpdate();
				// if update is not due but there is a weaponfire on the queue, send an offtick fire event (if NSTSettings allow for that)
				else if (NSTSettings.single.allowOfftick && customEventQueue.Count > 0)
					ClientGenerateRegularUpdate(true);
			}

			UpdateRewindDebug();
		}

		private void UpdateRewindDebug()
		{
			//if (!hasAuthority)
			//	return;
			if (NetworkServer.active == false)
				return;


			if ((int)debugXform >= 5) // == DebugXform.RewindHalfSec || debugXform == DebugXform.RewindTenthSec || debugXform == DebugXform.RewindTenthSec || debugXform == DebugXform.RewindHundrethSec || debugXform == DebugXform.RewindZeroSec)
			{
				FrameElements rframe = 
				(debugXform == DebugXform.RewindOneSec) ? buffer.Rewind(1f) :
				(debugXform == DebugXform.RewindHalfSec) ? buffer.Rewind(.5f) :
				(debugXform == DebugXform.RewindTenthSec) ? buffer.Rewind(.1f) :
				(debugXform == DebugXform.RewindHundrethSec) ? buffer.Rewind(.01f) :
				(debugXform == DebugXform.RewindZeroSec) ? buffer.Rewind(.0f) : 
				buffer.Rewind(lastJoinedPlayerConn, true, true);
				
				if (rframe == null || debugXformGO == null)
					return;

				debugXformGO.transform.position = rframe.rootPosition;
				debugXformGO.transform.rotation = rframe.rotationElements[0];

				// TODO: this is just here to test load from moving a collider by its transform often
				//if (rewindGO != null)
				//{
				//	Debug.Log("CRUNCH - TESTING MOVING THE REWIND COLLIDER A LOT !!");
				//	//rewindCollider.enabled = true;
				//	rewindGO.SetActive(true);
				//	rewindGO.transform.position = rframe.pos;
				//	rewindGO.transform.rotation = rframe.rotations[0];
				//	rewindGO.SetActive(false);
				//	//rewindCollider.enabled = false;
				//}
			}
		}

		#endregion

		#region Interpolation

		public FrameBuffer buffer;
		// calculated at startup based on number of skipped frames
		public static float frameUpdateInterval;
		private bool waitingForFirstFrame = true;

		// interpolation vars
		private Vector3 posSnapshot, lastSentPos;
		private CompressedElement lastSentCompPos, lastSentCompPosKey;

		private float posSnapshotTime;        // used by interpolation lerp to determine when lerp began

		private void InterpolateTransform()
		{
			// If we need a new Frame (lerped to end of last one or waiting at start)
			if (Time.time >= CurrentFrame.endTime)
			{
				// Still no Frame has arrived - nothing to do yet
				if (waitingForFirstFrame == true && CurrentIndex == 0 && buffer.validFrameMask <= 1)
				{
					// Reset end time while waiting for else numOfFramesOverdue will be enourmous.

					DebugX.Log(Time.time + " " + name + " <color=black>Still waiting for first frame update." + "</color>");
					CurrentFrame.endTime = Time.time;
					return;
				}

				waitingForFirstFrame = false;
				// If the frame that just completed lerping contained a custom message - send out its event
				if (CurrentFrame.msgType == MsgType.Cust_Msg)
				{
					if (OnCustomMsgEndInterpolationEvent != null)
						OnCustomMsgEndInterpolationEvent(NI.clientAuthorityOwner, CurrentFrame.customMsg, this, CurrentFrame.pos, CurrentFrame.positions, CurrentFrame.rotations);
				}

				// Testing for very low framerates
				int numOfFramesOverdue = (int)((Time.time - CurrentFrame.endTime) / frameUpdateInterval);

				if (numOfFramesOverdue > 2)
				{
					// Get the real number of frames we seem to be overdue, which is the current buffer size - the desired size.
					numOfFramesOverdue = Mathf.Max(0, (int)((buffer.CurrentBufferSize - NSTSettings.single.desiredBufferMS) / frameUpdateInterval));
					CurrentFrame.endTime = Time.time;
				}

				// For loop is to catch up on frames if the screen update is slower than the fixed update.
				for (int overduecount = numOfFramesOverdue; overduecount >= 0; overduecount--)
				{
					DebugX.Log(" <color=black><b>Finding Next Frame To Interpolate.</b></color> " +
						Time.time + " NST:" + NstId + " " + name +
					"\nValid Frames: " + buffer.PrintBufferMask(CurrentIndex));

					// THIS IS THE MAIN FIND SECTION WHERE NEW FRAMES ARE FOUND
					
					// Call the GetNextFrame function and apply it as current
					buffer.SnapshotToRewind(CurrentIndex);

					Frame next = buffer.DetermineNextFrame();
					buffer.prevAppliedFrame = CurrentFrame;
					CurrentFrame = next;

					DebugX.Log(Time.time + " NST:" + NstId + " <b> Last Frame was " + buffer.prevAppliedFrame.packetid + ".  New Frame is: " + CurrentFrame.packetid + "</b> type:" + (MsgType)CurrentFrame.msgType);

					// ROOT POSITION STUFF
					if (rootPosSync != RootPositionSync.Disabled)
					{
						// this is the new snapshot
						posSnapshot = transform.position;
						posSnapshotTime = Time.time - Time.deltaTime; // this snapshot is actually last updates position

						// Treat a move greater than the threshold apply a soft teleport.

						if (CurrentFrame.msgType.IsPosType())
						{
							if ((CurrentFrame.pos - posSnapshot).SqrMagnitude(includeXYZ) > (teleportThreshold * teleportThreshold))
							{
								ApplyTeleportLocally(CurrentFrame.pos, false);
							}
						}

						if (debugXform == DebugXform.Uninterpolated && !hasAuthority)
						{
							debugXformGO.transform.position = CurrentFrame.pos;
						}
					}

					// TODO: move these loops to the TransformElements base class.
					// CHILD POSITIONS STUFF
					for (int i = 0; i < positionElements.Count; i++)
					{
						if (CurrentFrame.positionsMask.GetBitInMask(i))
						{
							positionElements[i].Snapshot(CurrentFrame.positions, i);
							i.SetBitInMask(ref CurrentFrame.positionsMask, false);
						}
						else
						{
							positionElements[i].SnapshotEmpty();
						}
					}
					// ROTATION STUFF
					for (int i = 0; i < rotationElements.Count; i++)
					{
						if (CurrentFrame.rotationsMask.GetBitInMask(i))
						{
							rotationElements[i].Snapshot(CurrentFrame.rotations, i);
							i.SetBitInMask(ref CurrentFrame.rotationsMask, false);
						}
						else
						{
							rotationElements[i].SnapshotEmpty();
						}
					}

					// NEW FRAME FOUND - DO SOME SETUP AND CLEANUP ON IT

					// If the dequeue'd frame contained a custom message - send out an event
					if (CurrentFrame.msgType == MsgType.Cust_Msg)
					{
						if (OnCustomMsgBeginInterpolationEvent != null)
							OnCustomMsgBeginInterpolationEvent(NI.clientAuthorityOwner, CurrentFrame.customMsg, this, CurrentFrame.pos, CurrentFrame.positions, CurrentFrame.rotations);
					}

					// Recalculate the buffer size (This may need to happen AFTER the valid flag is set to false to be accurate) - Not super efficient doing this test too often
					buffer.UpdateBufferAverage();

					float nudge = (overduecount > 0) ? 0 :
						(NSTSettings.single.desiredBufferMS - buffer.bufferAverageSize) * NSTSettings.single.bufferDriftCorrectAmt;

					//Debug.Log("nudge " + nudge + " avg: " + buffer.bufferAverageSize  + "  desiredms: " + NSTSettings.single.desiredBufferMS);
					DebugX.Log("nudge " + nudge + "buffer.bufferAverageSize " + buffer.bufferAverageSize  + "  desiredms: " + NSTSettings.single.desiredBufferMS);

					CurrentFrame.endTime = buffer.prevAppliedFrame.endTime + frameUpdateInterval + nudge;

					// Mark the current frame as no longer pending in the mask
					buffer.SetBitInValidFrameMask(CurrentIndex, false);
					
				}
			}
			// End getting next frame from buffer... now do the Lerping.

			float lerpTime = Mathf.InverseLerp(posSnapshotTime, CurrentFrame.endTime, Time.time);
			// if this frame contains either pos or posDelta  - lerp to that.
			if (CurrentFrame.msgType.IsPosType())
			{
				if (rb == null || rb.isKinematic)
					gameObject.transform.position = gameObject.Lerp(posSnapshot, CurrentFrame.pos, includeXYZ, lerpTime);
				else
					rb.MovePosition(gameObject.Lerp(posSnapshot, CurrentFrame.pos, includeXYZ, lerpTime));
			}

			for (int i = 0; i < positionElements.Count; i++)
			{
				positionElements[i].UpdateInterpolation(CurrentFrame.endTime);
			}

			for (int i = 0; i < rotationElements.Count; i++)
			{
				rotationElements[i].UpdateInterpolation(CurrentFrame.endTime);
			}

		}

		#endregion

		#region Custom Events

		private Queue<byte[]> customEventQueue = new Queue<byte[]>();

		/// <summary>
		/// This static method assumes you have ONLY ONE NST on this client. If you have more than one use the non-static AddCustomEventToQueue method to specify which NST you mean. 
		/// Tack your own data on the end of the NST syncs. This can be weapon fire or any other custom action. 
		/// This will trigger the OnCustomMsgSndEvent on the sending machine and OnCustomMsgRcvEvent on receiving machines.
		/// </summary>
		/// <param name="userData"></param>
		public static void SendCustomEventSimple(byte[] userData)
		{
			if (lclNST != null)
				lclNST.customEventQueue.Enqueue(userData);
		}

		/// <summary>
		/// This static method assumes you have ONLY ONE NST on this client. If you have more than one use the non-static AddCustomEventToQueue method to specify which NST you mean.
		/// This overlad will accept just about anything you put into a struct, so be careful. Limit your datatypes to JUST the smallest compressed primatives and don't include methods 
		/// or properties in your custom struct. Otherwise this could bloat your net traffic fast.
		/// This will trigger the OnCustomMsgSndEvent on the sending machine and OnCustomMsgRcvEvent on receiving machines.
		/// </summary>
		/// <typeparam name="T">A custom struct of your own making.</typeparam>
		public static void SendCustomEventSimple<T>(T userData) where T : struct
		{
			if (lclNST != null)
				lclNST.customEventQueue.Enqueue(userData.SerializeToByteArray());
		}

		/// <summary>
		/// Tack your own data on the end of the NST syncs. This can be weapon fire or any other custom action.
		/// This will trigger the OnCustomMsgSndEvent on the sending machine and OnCustomMsgRcvEvent on receiving machines.
		/// </summary>
		/// <param name="userData"></param>
		public void SendCustomEvent(byte[] userData)
		{
			customEventQueue.Enqueue(userData);
		}

		/// <summary>
		/// This overlad will accept just about anything you put into a struct, so be careful. Limit your datatypes to JUST the smallest compressed primatives and don't include methods 
		/// or properties in your custom struct. Otherwise this could bloat your net traffic fast.
		/// This will trigger the OnCustomMsgSndEvent on the sending machine and OnCustomMsgRcvEvent on receiving machines.
		/// </summary>
		/// <typeparam name="T">A custom struct of your own making.</typeparam>
		public void SendCustomEvent<T>(T userData) where T : struct
		{
			customEventQueue.Enqueue(userData.SerializeToByteArray());
		}

		#endregion

		#region Teleport

		//private bool waitingForTeleportConfirm;
		private bool clientTeleportConfirmationNeeded; // a teleport has occurred, next outgoing packet needs to indicate that.

		/// <summary>
		/// Public method for initiating a Teleport. Works for the localplayer with authority, or for the server. For localplayer this generates a TELEPORT msgtype.
		/// If server initiated (and not the local player) this generates a SVRTELEPORTCOMMAND msgtype, to which the localplayer needs to acknowledge with a TELEPORT msgtype.
		/// Non-local player machines will ignore any messages until they receive this TELEPORT confirmation. Currently Teleport is only for position. Rotation handlers will
		/// be a bit more complicated now with Rotation Elements, so I need some time to figure out how to do this correctly.
		/// </summary>
		public void Teleport(Vector3 pos)
		{
			//Debug.Log(Time.time + " <color=green>TELEPORT called : isServer?" + isServer + " isLcLPlayer?" + hasAuthority + " </color>");
			if (!isServer && !hasAuthority)
			{
				DebugX.LogWarning("You are trying to teleport an object from a client without authority over that Network Sync Transform. Only the server or the client with Authority may teleport NST objects.");
				return;
			}

			// On the server, send the teleport command to clients, and set to confirmation waiting condition.
			// This will ignore all incoming messages from this object until a confirmation arrives.
			if (isServer)
			{
				SvrCommandTeleport(this, 0, pos);
				DebugX.Log(Time.time + " <color=red>Teleport() SET WAITING TRUE  " + "</color>");
			}

			// if the player initiated this with local authority, the next outgoing message will be of the Teleport msgtype.
			if (hasAuthority)
			{
				ApplyTeleportLocally(pos);
				clientTeleportConfirmationNeeded = true;
			}
		}

		// Code for the server side portion of a teleport.
		private static void SvrCommandTeleport(NetworkSyncTransform nst, byte packetcounter, Vector3 pos)
		{
			if (nst.NI == null)
			{
				DebugX.LogWarning("Network object has been destroyed before Teleport could happen.");
				return;
			}

			// Teleport with SVRTPORT id indicates that this is the original teleport command - will be wanting a response teleport.
			WriteGenericTransMessage(nst, MsgType.SvrTPort, pos.CompressPos(), null, true);
		}
		/// <summary>
		/// Calls TeleportToLocation on the local NST object. This is a conveinience method and will only work reliably if you only have one NST object with local authority.
		/// </summary>
		public static void TeleportLclPlayer(Vector3 pos)
		{
			if (lclNST != null)
				lclNST.Teleport(pos);
		}


		// Called on clients when the Server teleport msgtype arrives. Currently this is only being called on the player with authority
		private void ClientSvrTeleportCommandHandler(Vector3 pos, List<GenericX> rots)
		{
			// no need to run these again for server.
			if (!NetworkServer.active)
			{
				ApplyTeleportLocally(pos);
			}

			// if this is the the local player, a teleport confirmation is needed. Clients will ignore updates until a confirm arrives
			// this is to ignore any pre-teleport positions still crossing the internet.
			if (hasAuthority)
				clientTeleportConfirmationNeeded = true;

			return;
		}
		/// <summary>
		/// Apply teleport using current position as overload
		/// </summary>
		public void ApplyTeleportLocally(bool hardTeleport = true) { ApplyTeleportLocally(transform.position); }

		/// <summary>
		/// Move object LOCALLY to pos/rot without lerping or interpolation. Clears all buffers and snapshots. 
		/// HardTeleport will clear the buffer, soft teleport just disables the RB to avoid lerping.
		/// </summary>
		public void ApplyTeleportLocally(Vector3 pos, bool hardTeleport = true)
		{
			bool wasKinematic = false;

			DebugX.Log(Time.time + " <color=red><b>Teleport</b></color> hard:" + hardTeleport + " + Distance: " + Vector3.Distance(pos, transform.position));

			if (rb != null)
			{
				rb.Sleep();
				wasKinematic = rb.isKinematic;
				rb.isKinematic = true;
			}

			// Clear ALL old frame buffer items to stop warping. They are all invalid now anyway.
			//frameQueue.Clear();
			if (hardTeleport)
			{
				buffer.validFrameMask = 0;

				CurrentFrame.pos = pos;
				CurrentFrame.compPos = pos.CompressPos();
				lastSentPos = pos;
				lastSentCompPos = pos.CompressPos();
			}

			posSnapshot = pos;
			gameObject.SetPosition(pos, includeXYZ); //transform.position = pos;

			if (rb != null)
				rb.isKinematic = wasKinematic;
		}

		#endregion

		#region Register UNET Handlers

		public static void InitializeClientMsgHandlers()
		{
			// NST doesn't run host as a separate client. All work that would be done on client is done when host receives

			// Only register handlers if this is client... NOT for host.
			if (NetworkServer.active)
				return;

			NetworkManager.singleton.client.RegisterHandler((short)MsgType.Position, ReceieveGeneric);
			NetworkManager.singleton.client.RegisterHandler((short)MsgType.Low_Bits, ReceieveGeneric);
			NetworkManager.singleton.client.RegisterHandler((short)MsgType.No_Positn, ReceieveGeneric);
			NetworkManager.singleton.client.RegisterHandler((short)MsgType.Cust_Msg, ReceieveGeneric);
			NetworkManager.singleton.client.RegisterHandler((short)MsgType.Teleport, ReceieveGeneric);
			NetworkManager.singleton.client.RegisterHandler((short)MsgType.SvrTPort, ReceieveGeneric);
		}

		public static void InitializeServerMsgHandlers()
		{
			NetworkServer.RegisterHandler((short)MsgType.Position, ReceieveGeneric);
			NetworkServer.RegisterHandler((short)MsgType.Low_Bits, ReceieveGeneric);
			NetworkServer.RegisterHandler((short)MsgType.Cust_Msg, ReceieveGeneric);
			NetworkServer.RegisterHandler((short)MsgType.Teleport, ReceieveGeneric);
			NetworkServer.RegisterHandler((short)MsgType.SvrTPort, ReceieveGeneric);
			NetworkServer.RegisterHandler((short)MsgType.No_Positn, ReceieveGeneric);
		}

		#endregion
		
		#region Message Transmission

		private static NetworkWriter writer = new NetworkWriter();

		// TODO: make this grow as needed
		private static byte[] reusableByteArray = new byte[256];

		private int _msgCount = 1;
		public int PacketCount
		{
			get { return _msgCount; }
			set
			{
				_msgCount = value;
				if (_msgCount == NSTSettings.single.packetCounterRange) _msgCount = 1;
			}
		}

		int sequentialKeyCount = 0;

		//PLAYER WITH AUTHORITY RUNS THIS EVERY TICK
		/// <summary>Determine which msgType is needed and then call Generate using that type</summary>
		private void ClientGenerateRegularUpdate(bool isOfftick = false)
		{
			CompressedElement compressedPos = transform.position.CompressPos();
			byte[] customMsg = null;

			MsgType msgType = MsgType.No_Positn;

			// If a teleport has occurred, indicate that with next position update.
			if (clientTeleportConfirmationNeeded)
			{
				msgType = MsgType.Teleport;
				customMsg = null;
				clientTeleportConfirmationNeeded = false;
			}

			// if a custom message is que'd, send custom data along with the full transform
			else if (customEventQueue.Count > 0)
			{
				msgType = MsgType.Cust_Msg;
				customMsg = customEventQueue.Dequeue();
			}
			else
			{
				// Check for what kind of msg needs to be sent
				if (rootPosSync != RootPositionSync.Disabled)
				{
					bool hasMoved = (CompressedElement.Compare(compressedPos, lastSentCompPos) == false);
					bool posKeyDue =
						(
						clientTeleportConfirmationNeeded ||
						//sendCulling == SendCulling.Always ||
						//Time.time - lastSentKeyframeTime >= keyRate
						((keyEvery != 0) && (PacketCount % keyEvery == 0))
						);

					//test to make sure movement doesn't exceed keyframe limits - don't use a keyframe if to does
					if (cullUpperBits)
					{
						if (!NSTCompressVector.TestMatchingUpper(lastSentCompPosKey, compressedPos))
						{
							DebugX.Log(Time.time + "NST:" + NstId + " Position upper bits have changed. Sending full pos for next " + sequentialKeys + " updates");
							sequentialKeyCount = sequentialKeys;
						}
					}
					// Brute for send x number of keyframes after an upperbit change to overcome loss problems.
					if (sequentialKeyCount != 0)
					{
						posKeyDue = true;
						sequentialKeyCount--;
					}

					// Determine if this frame is being forced
					bool notCulled = sendCulling == SendCulling.Always || (sendCulling.ChangesType() && hasMoved);

					msgType =
						posKeyDue ? MsgType.Position :
						notCulled ? cullUpperBits ? MsgType.Low_Bits : MsgType.Position :
						MsgType.No_Positn;
				}
			}

			// Generate an update with determined msgtype
			WriteGenericTransMessage(this, msgType, compressedPos, customMsg, isOfftick);
			
			if (!isOfftick)
			{
				frameTransmitDue = false;
				// Add local authority objects to rewind here.
				buffer.SnapshotToRewind(PacketCount);
			}
		}

		/// <summary>
		/// Serialize the appropriate data the NetworkWriter.
		/// </summary>
		private static void WriteGenericTransMessage(NetworkSyncTransform nst, MsgType msgType, CompressedElement compPos, byte[] externalData, bool isOffTick)
		{
			UdpKit.UdpBitStream bitstream = new UdpKit.UdpBitStream(reusableByteArray, 32);

			// Update the debug transform if it is being used
			if (nst.debugXform == DebugXform.LocalSend)
				NSTHelper.UpdateLclTransformDebug(nst, compPos, nst.lastSentCompPos);

			bitstream.WriteUInt(nst._nstIdSyncvar, NSTSettings.single.bitsForNstId);

			// Get new packetID and write to bitstream
			int packetid = isOffTick ? 0 : nst.PacketCount++;

			bitstream.WriteInt(packetid, NSTSettings.single.bitsForPacketCount);

			// Send position key or lowerbits
			if (msgType.IsPosType())
			{
				compPos.WriteCompPosToBitstream(ref bitstream, nst.includeXYZ , msgType.IsPosLowerType());
				nst.lastSentPos = compPos.Decompress();
				nst.lastSentCompPos = compPos;

				// If this is a key, log it
				if (!isOffTick && msgType.IsPosKeyType())
				{
					nst.lastSentCompPosKey = compPos;
				}
			}

			// Write the transform elements to the buffer
			TransformElement.WriteAllElements(ref bitstream, msgType, nst.buffer, packetid, nst.positionElements);
			TransformElement.WriteAllElements(ref bitstream, msgType, nst.buffer, packetid, nst.rotationElements);

			// If this is flagged as a weapon fire, then pass along the extra byte[] array for weapon fire
			if (msgType == MsgType.Cust_Msg)
			{
				bitstream.WriteByteArray(externalData, externalData.Length);

				// Notify the server/client generating this message that the custom event is now being sent.
				if (OnCustomMsgSndEvent != null)
					OnCustomMsgSndEvent(nst.NI.clientAuthorityOwner, externalData, nst, nst.lastSentPos, nst.buffer.frames[packetid].positions, nst.buffer.frames[packetid].rotations);
			}

			int qosChannel = msgType == MsgType.SvrTPort || msgType == MsgType.Teleport ? Channels.DefaultReliable : Channels.DefaultUnreliable;

			// If network sim, send a big GC bloated version to the newtwork sim coroutine
			if (NSTSettings.single.enabledNetSimulation)
			{
				nst.StartCoroutine(NetworkSim(nst, msgType, bitstream.Data, bitstream.Ptr, qosChannel));
			}
			else
			{
				SendBuffer(nst, msgType, ref bitstream, qosChannel);
			}

			// Fire the receive events now if this is a host. They will never be received.
			if (NetworkServer.active && msgType == MsgType.Cust_Msg)
			{
				if (OnCustomMsgRcvEvent != null)
					OnCustomMsgRcvEvent(nst.NI.clientAuthorityOwner, externalData, nst, compPos.Decompress(), nst.buffer.frames[packetid].positions, nst.buffer.frames[packetid].rotations);

				if (OnCustomMsgBeginInterpolationEvent != null)
					OnCustomMsgBeginInterpolationEvent(nst.NI.clientAuthorityOwner, externalData, nst, compPos.Decompress(), nst.buffer.frames[packetid].positions, nst.buffer.frames[packetid].rotations);

				if (OnCustomMsgEndInterpolationEvent != null)
					OnCustomMsgEndInterpolationEvent(nst.NI.clientAuthorityOwner, externalData, nst, compPos.Decompress(), nst.buffer.frames[packetid].positions, nst.buffer.frames[packetid].rotations);
			}



			DebugX.Log(
				(msgType.IsPosLowerType()) ?
				(Time.time + " NST:" + nst.NstId + "<b> sent packet:" + packetid + "</b> msg: " + msgType + " <color=blue>Sending LOWER POS BITS ONLY - you want to see lots of these or else skipping upper bits isn't very effective for your map size/movement speed.</color>") :
				(msgType.IsPosKeyType()) ?
				(Time.time + " NST:" + nst.NstId + "<b> sent packet:" + packetid + "</b> msg: " + msgType + " <color=green>sending ALL POS BITS</color>") :
				(Time.time + " NST:" + nst.NstId + "<b> sent packet:" + packetid + "</b> msg: " + msgType + " <color=purple>sending ROT ONLY BITS</color>"));
		}

		/// <summary>
		/// The network sim generates a lot of garbage. But it will never run in production code so not a concern.
		/// </summary>
		private static IEnumerator NetworkSim(NetworkSyncTransform nst, MsgType msgType, byte[] _data, int _Ptr, int qosChannel = Channels.DefaultUnreliable)
		{
			// need to copy the bitstream's parts to make a copy.
			byte[] delayedByteArray = new byte[_data.Length];
			_data.CopyTo(delayedByteArray, 0);
			UdpKit.UdpBitStream copyBitstream = new UdpKit.UdpBitStream(delayedByteArray, _data.Length)
			{
				Ptr = _Ptr,
				Length = _data.Length
			};
			// If packetloss simulation wants packet lost, stop here (unless its teleport which is sent RUDP)
			if (UnityEngine.Random.Range(0f, 1f) < NSTSettings.single.packetLossSimulation)
			{
				// Teleport is a reliable message - so resend if it failed here for testing purposes (a very bad recration of RUDP at work)
				if (qosChannel == Channels.DefaultReliable)
				{
					nst.StartCoroutine(NetworkSim(nst, msgType, _data, _Ptr, qosChannel));
				}
				yield break;
			}

			float waittime = NSTSettings.single.latencySimulation + UnityEngine.Random.Range(0f, NSTSettings.single.jitterSimulation);
			yield return new WaitForSeconds(waittime);

			SendBuffer(nst, msgType, ref copyBitstream, qosChannel);
		}

		private static void SendBuffer(NetworkSyncTransform nst, MsgType msgType, ref UdpKit.UdpBitStream bitstream, int qosChannel = Channels.DefaultUnreliable)
		{
			writer.StartMessage((short)msgType);
			writer.WriteUncountedByteArray(bitstream.Data, bitstream.BytesUsed);
			writer.FinishMessage();

			// if this is the server - send to all.
			if (nst.isServer)
			{
				if (msgType == MsgType.SvrTPort) // send Server teleport command only to owner for now - will expand later if svr authority teleport is added
					nst.connectionToClient.SendWriter(writer, qosChannel);
				else
					writer.SendPayloadArrayToAllClients((short)msgType, qosChannel);

			}
			// if this is a client send to server.

			else
				NetworkManager.singleton.client.SendWriter(writer, qosChannel);
		}

		#endregion

		#region Message Reception

		/// <summary>Deserialize the reader into the appropriate fields, and add Frame to buffer Queue (if applicable)</summary>
		private static void ReceieveGeneric(NetworkMessage msg)
		{
			MsgType msgType = (MsgType)msg.msgType;

			DebugX.Log(Time.time + " Received msg size: " + msg.reader.Length);

			UdpKit.UdpBitStream bitstream = new UdpKit.UdpBitStream(msg.reader.ReadBytesNonAlloc(reusableByteArray, msg.reader.Length), msg.reader.Length);

			// Deserialize the correct header (based on the NSTSettings for max objects
			uint nid;
			int packetid;

			nid = bitstream.ReadUInt(NSTSettings.single.bitsForNstId);
			packetid = bitstream.ReadInt(NSTSettings.single.bitsForPacketCount);

			// The NST is stored differently depending on the developer selected size.
			NetworkSyncTransform nst =
				(NSTSettings.single.bitsForNstId >= 6) ? nst = nstIdToNSTLookup[nid] :
				nst = NstIds[nid];

			// Discard if this NST has gone away (removed from game)
			if (nst == null)
			{
				DebugX.LogWarning("NST received but no object exists with msgtype:" + msg.msgType + " index: " + packetid);
				return;
			}
			
			Frame frame = nst.buffer.frames[packetid];

			CompressedElement compPos =
				msgType.IsPosLowerType() ? NSTCompressVector.ReadCompressedPosFromBitstream(ref bitstream, nst.includeXYZ, true) :
				msgType.IsPosKeyType()   ? NSTCompressVector.ReadCompressedPosFromBitstream(ref bitstream, nst.includeXYZ) :
				CompressedElement.zero;

			// This will only be a valid position if the was a pos key. TODO: Move this inside an if so this only happens for Teleport/CustomMessges
			Vector3 pos = compPos.Decompress();

			DebugX.Log(Time.time + " NST:" + nst.NstId + " " + nst.name + " <color=green><b>Received Update " + packetid + " </b></color>\n" + pos + " " + (MsgType)msg.msgType + " size:" + msg.reader.Length + "bytes");

			// Read any incoming transform elements into their appropriate lists
			TransformElement.ReadAllElements(ref bitstream, msgType, nst.buffer, packetid, nst.positionElements);
			TransformElement.ReadAllElements(ref bitstream, msgType, nst.buffer, packetid, nst.rotationElements);

			if(nst.debugXform == DebugXform.RawReceive && !nst.hasAuthority)
			{
				if (nst.rootPosSync != RootPositionSync.Disabled)
				nst.debugXformGO.transform.position = 
					(msgType.IsPosLowerType()) ? 
					NSTCompressVector.GuessUpperBits(compPos, nst.buffer.BestPreviousKeyframe(packetid).compPos).Decompress() : 
					pos;

				if (BitTools.GetBitInMask(frame.rotationsMask, 0))
					nst.debugXformGO.transform.rotation = frame.rotations[0];
			}

			//if this a server teleport command from server to clients - TODO: MAY NOT BE NEEDED
			if (msg.msgType == (short)MsgType.SvrTPort)
			{
				nst.ClientSvrTeleportCommandHandler(pos, frame.rotations);
			}
			// if this is a Custom message, pass it along as an event.
			else if (msg.msgType == (short)MsgType.Cust_Msg)
			{
				int remainingbuffer = (bitstream.Length - bitstream.Ptr) / 8;
				
				// Read the custom data directly into the frame buffer to skip some steps. 
				bitstream.ReadByteArray(frame.customMsg, remainingbuffer);

				frame.customMsgSize = remainingbuffer;

				// send out the event that a weapon fire message has arrived
				if (OnCustomMsgRcvEvent != null)
					OnCustomMsgRcvEvent(msg.conn, frame.customMsg, nst, pos, frame.positions, frame.rotations);

				// if this is an offtick custom msg, it will never be on the queue so fire the begin and end interp messages now.
				if (OnCustomMsgBeginInterpolationEvent != null && packetid == 0)
					OnCustomMsgBeginInterpolationEvent(msg.conn, frame.customMsg, nst, pos, frame.positions, frame.rotations);

				if (OnCustomMsgEndInterpolationEvent != null && packetid == 0)
					OnCustomMsgEndInterpolationEvent(msg.conn, frame.customMsg, nst, pos, frame.positions, frame.rotations);
			}

			// Write a clone message and pass it to all the clients if this is the server
			if (NetworkServer.active && msg.conn == nst.NI.clientAuthorityOwner)
			{
				writer.StartMessage(msg.msgType);
				writer.WriteUncountedByteArray(bitstream.Data, msg.reader.Length);
				writer.SendPayloadArrayToAllClients(msg.msgType);
			}

			// if update is required for this pass (client or headless server) Update pos - add to lastpos in the case of pos_delta
			if (!nst.hasAuthority)
			{
				nst.buffer.AddFrameToBuffer(msgType, compPos, packetid);
			}
		}

		#endregion

	}
}
