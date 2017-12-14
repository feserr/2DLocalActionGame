//Copyright 2017, Davin Carten, All rights reserved

using System.Collections.Generic;
using UnityEngine;

namespace emotitron.Network.NST
{
	[AddComponentMenu("Network Sync Transform/NST Settings")]
	[System.Serializable]
	public class NSTSettings : MonoBehaviour
	{
		public static NSTSettings single;

		/// <summary>
		/// Call this at the awake of other NST components to make sure that NSTSettings exists in the scene.
		/// </summary>
		public static void EnsureSettingsExistsInScene()
		{
			if (single != null)
				return;

			var preexist = FindObjectOfType<NSTSettings>();

			if (preexist != null)
				return;

			DebugX.LogWarning("<b>No NSTSettings found in scene. Adding one with default settings.</b> You probably want to edit the settings yourself.");
			GameObject go = new GameObject("NST Settings");
			single = go.AddComponent<NSTSettings>();
		}

		#region Inspector Items

		[Header("Header Sizes")]

		[Range(1, 32)]
		[Tooltip("Set this to the smallest number that works for your project. 1 bit = 2 NST object max, 4 bits = 16 NST objects max, 5 bits = 32 NST objects max, 32 bits = Unlimited")]
		public int bitsForNstId = 5;
#if UNITY_EDITOR
		[ReadOnly]
#endif
		public uint MaxNSTObjects;

		// To make this go up to 6 will need to change masks to ulongs rather than uint.
		[Range(4, 6)]
		public int bitsForPacketCount = 5;
#if UNITY_EDITOR
		[ReadOnly]
#endif
		public int packetCounterRange;

		[Header("General Settings")]

		[Tooltip("Keeping your 'AddCustomEventToQueue' calls on the NST tick reduces traffic, but will induce latency. " +
			"Also, note that since events are placed on the tick, their position may differ slightly when the tick is sent from when this is called.")]
		public bool allowOfftick = true;

		[Range(0f, .5f)]
		[Tooltip("Target number of milliseconds to buffer. Higher creates more induced latency, lower smoother handling of network jitter and loss.")]
		public float desiredBufferMS = .15f;

		[Range(0f, 1f)]
		[Tooltip("How aggressively to try and maintain the desired buffer size.")]
		public float bufferDriftCorrectAmt = .5f;

		//[Range(1, 5)] [Tooltip("3 is the default. 1 sends updates every fixed update. 3 every 3rd. This effectively defines your tickrate.")]
		//public int sendEveryXFixed = 3;

		[Space]
		[Tooltip("Enabling rewind trigger colliders will create colliders on a dedicated rewind layer. These will be used by the rewind hit methods. The impact of these is not fully tested so keep this disabled unless you are using the rewind hitscan tests.")]
		public bool useRewindColliders = false;
		[HideInInspector] public LayerMask rewindLayer;
		[HideInInspector] public int rewindLayerMask;

		[Space]
		public bool overrideFixedTime = false;

		[Range(25, 100)]
		public int physicsTicksPerSec = 50;

		[Header("Vector Compression")]

		[Range(10, 1000)] [Tooltip("Indicate the minimum resolution of any axis of compressed positions (Subdivisions per 1 Unit). Increasing this needlessly will increase your network traffic.")]
		public float minPosResolution = 100;
		
		[Header("Debugging")]

		[Tooltip("Turn this off for your production build to reduce pointless cpu waste.")]
		[SerializeField]
		public bool logWarnings = true;

		[Tooltip("Spam your log with all kinds of info you may or may not care about. Turn this off for your production build to reduce pointless cpu waste.")]
		[SerializeField]
		public bool logTestingInfo = false;

		[Space]

		public bool enabledNetSimulation = false;

		[Tooltip("Seconds of latency in each direction (RTT will be double this number)")]
		[Range(0,1)]
		public float latencySimulation = .1f;

		[Tooltip("0 = 0% loss, 1 = 100% loss")]
		[Range(0,1)]
		public float packetLossSimulation = .1f;

		[Tooltip("Max seconds packet can randomly late by.")]
		[Range(0, 1)]
		public float jitterSimulation = .120f;

		#endregion

		private void Awake()
		{
			if (single != null)
			{
				Debug.LogWarning("Enforcing NSTSettings singleton. Multiples found.");
				Destroy(this);
			}
			single = this;

			// Tell DebugX what the logging choices were.
			DebugX.logInfo = logTestingInfo;
			DebugX.logWarnings = logWarnings;
			DebugX.logErrors = true;

			// Changes physics rate if setting calls for it
			if (overrideFixedTime)
				Time.fixedDeltaTime = 1f / physicsTicksPerSec;


			//Calculate the max objects at the current bits for NstId
			MaxNSTObjects = (uint)Mathf.Pow(2, bitsForNstId);

			packetCounterRange =
				(bitsForPacketCount == 4) ? 16 :
				(bitsForPacketCount == 5) ? 32 :
				(bitsForPacketCount == 6) ? 64 :
				0; // zero will break things, but it should never happen so breaking it would be good.

			// Set Rewind layer to ignore all other layers
			SetUpRewindLayer();
		}

		private void SetUpRewindLayer()
		{
			if (useRewindColliders)
			{
				// First see if there is a name layer the user has set up
				rewindLayer = LayerMask.NameToLayer("NSTRewind");
				rewindLayerMask = LayerMask.GetMask("NSTRewind");

				// If not, find the first empty layer
				if (rewindLayer.value == -1)
				{
					for (int i = 8; i < 32; i++)
					{
						DebugX.Log(" Testing " + i + " '" + LayerMask.LayerToName(i) + "'");
						if (LayerMask.LayerToName(i) == "")
						{
							rewindLayer = i;
							//rewindLayerMask = 
							BitToolsUtils.BitTools.SetBitInMask(i, ref rewindLayerMask, true);
							DebugX.Log("Rewind will use empty physics layer " + rewindLayer.value + " with a mask of " + rewindLayerMask + ". This was the first unused layer found. To specify the physics layer for rewind add a dedicated physics layer named <b>'NSTRewind'</b> to <b>Edit/Project Settings/Tags and Layers</b>.");
							break;
						}
					}

					// If no empty layers could be found, rewind is not possible.
					if (rewindLayer.value == -1)
					{
						DebugX.LogError("No 'NSTRewind' layer found in layers and no unused physics layer could be found. You must add a dedicated physics layer named <b>'NSTRewind'</b> to <b>Edit/Project Settings/Tags and Layers</b> in order to make use of rewind ray test methods.");
						return;
					}
				}

				// Clear all physics layers from interacting with NSTRewind layer
				for (int i = 0; i < 32; i++)
				{
					Physics.IgnoreLayerCollision(rewindLayer.value, i);
				}
			}
		}

		private void Start()
		{
			// Destroy any NSTs the developer may have left in the scene.
			List<NetworkSyncTransform> nsts = FindObjects.FindObjectsOfTypeAllInScene<NetworkSyncTransform>(true);
			for (int i = 0; i < nsts.Count; i++)
			{
				Destroy(nsts[i].gameObject);
			}
		}
	}
}
