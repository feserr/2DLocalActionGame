//using System.Collections;
//using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;

namespace emotitron.Network.NST
{
	[CustomEditor(typeof(NSTSettings))]
	[CanEditMultipleObjects]
	public class NSTSettingsEditor : Editor
	{
		//public static NSTSettings single;
		GUIStyle bold;

		
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			var nstsettings = (NSTSettings)target;
			nstsettings.MaxNSTObjects = (uint)System.Math.Pow(2, nstsettings.bitsForNstId);
			nstsettings.packetCounterRange = (int)System.Math.Pow(2, nstsettings.bitsForPacketCount);

			float adjustedFixedTime = (nstsettings.overrideFixedTime) ?  (1f / nstsettings.physicsTicksPerSec) : Time.fixedDeltaTime;

			EditorGUILayout.Space();
			EditorGUILayout.LabelField(new GUIContent("Summary:"), bold);

			string str =
				"Physics Rate: " + adjustedFixedTime.ToString("0.000") + "ms (" + (1 / adjustedFixedTime).ToString("0.0") + " ticks/sec)\n" +
				//"Update every " + nstsettings.sendEveryXFixed + " ticks\n" +
				//"Network Rate: " + (adjustedFixedTime * nstsettings.sendEveryXFixed).ToString("0.000") + "ms (" + (1 / (adjustedFixedTime * nstsettings.sendEveryXFixed)).ToString("0.0") + " ticks/sec)\n" +
				"\n" +
				//"Buffer length is " + (nstsettings.packetCounterRange * adjustedFixedTime * nstsettings.sendEveryXFixed).ToString("0.00") + "secs. \n" +
				"\n" +
				"You can change the physics rate by changing the Edit/Project Settings/Time/Fixed Step value.";

			EditorGUILayout.HelpBox(str, MessageType.None);
		}

		private void OnEnable()
		{
			bold = new GUIStyle() { fontStyle = FontStyle.Bold };

			if (NSTSettings.single != null && (NSTSettings)target != NSTSettings.single)
			{
				DestroyImmediate(target);
				Debug.LogWarning("Enforcing NSTSettings singleton. Deleting newly created NSTSettings. \n" +
					"Existing NSTSettings is in scene object <b>" + NSTSettings.single.name + "</b>");
			}
			else
				NSTSettings.single = (NSTSettings)target;
		}

		private void OnDisable()
		{
			if (NSTSettings.single == target)
				NSTSettings.single = null;
		}


		[MenuItem("NST/Step 1. Add Settings Singleton To Scene (Just click this)")]
		private static void AddSettings()
		{
			AddNSTSettingsGOFromMenu();
		}
		[MenuItem("NST/Step 2. Add One Or More Map Bounds To Map GameObjects")]
		private static void MenuAddMapBounds()
		{
			AddMapBounds();
		}
		[MenuItem("NST/Step 3. Add NST To Network Objects You Want To Sync")]
		private static void MenuAddNST()
		{
			AddNST();
		}

		[MenuItem("GameObject/Network Sync Transform/NST Settings", false, 10)]
		static void CreateCustomGameObject(MenuCommand menuCommand)
		{
			AddNSTSettingsGOFromMenu();
		}

		// Add NSTSettings to scene with default settings if it doesn't already contain one.
		private static void AddNSTSettingsGOFromMenu()
		{
			var preexist = FindObjectOfType<NSTSettings>();
			if (preexist != null)
			{
				Debug.LogWarning("There is already a NSTSettings singleton in the scene on object " + preexist.name);
				return;
			}

			GameObject go = new GameObject("NST Settings");
			Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
			Selection.activeObject = go;
			go.AddComponent<NSTSettings>();
		}

		private static void AddMapBounds()
		{
			MeshRenderer[] renderers = Selection.activeGameObject.GetComponents<MeshRenderer>();
			if (renderers.Length == 0)
			{
				Debug.LogWarning("NSTMapBounds added to an item that has no Mesh Renderers in its tree.");
			}
			Selection.activeGameObject.AddComponent<NSTMapBounds>();
		}

		private static void AddNST()
		{
			if (Selection.activeGameObject.GetComponent<NetworkIdentity>() == null)
				Selection.activeGameObject.AddComponent<NetworkIdentity>();

			if (Selection.activeGameObject.GetComponent<NetworkSyncTransform>() == null)
				Selection.activeGameObject.AddComponent<NetworkSyncTransform>();

			Debug.LogWarning("Be sure to register this object with the NetworkManager either as the player or a spawnable object (whichever the case may be)");
		}
	}
}

