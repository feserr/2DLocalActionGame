using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace emotitron.Network.NST
{

	[CustomEditor(typeof(NetworkSyncTransform))]
	[CanEditMultipleObjects]
	public class NSTEditor : Editor
	{

		public void OnEnable()
		{
			NSTSettings.EnsureSettingsExistsInScene();

		}
		public override void OnInspectorGUI()
		{
			//serializedObject.Update();

			base.OnInspectorGUI();
			//SerializedProperty test = serializedObject.FindProperty("test");
			NetworkSyncTransform nst = (NetworkSyncTransform)target;


			//LayerMask tempMask = EditorGUILayout.MaskField(InternalEditorUtility.LayerMaskToConcatenatedLayersMask(myLayerMask), InternalEditorUtility.layers);

			//myLayerMask = (int)InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);

			//	EditorGUILayout.PropertyField(posSendType);

			//	EditorGUILayout.PropertyField(cullUpperBits);

			//	if (cullUpperBits.boolValue == true)
			//	{
			//		EditorGUILayout.PropertyField(sequentialKeys);
			//		EditorGUILayout.PropertyField(keyRate);
			//	}

			//	EditorGUILayout.PropertyField(rotationElements, true);



			// Make sure the object is active, to prevent users from spawning inactive gameobjects (which will break things)
			if (!nst.gameObject.activeSelf)// && AssetDatabase.Contains(target))
			{
				Debug.LogWarning("Prefabs with NetworkSyncTransform on them MUST be enabled. If you are trying to disable this so it isn't in your scene when you test it, no worries - NST destroys all scene objects with the NST component at startup.");
				nst.gameObject.SetActive(true);
			}

			//serializedObject.ApplyModifiedProperties();
		}
	}

}