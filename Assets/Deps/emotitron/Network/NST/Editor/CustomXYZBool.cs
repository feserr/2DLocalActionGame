using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace emotitron.Network.NST
{
	[CustomPropertyDrawer(typeof(XYZBool))]
	[CanEditMultipleObjects]

	public class XYZBoolDrawer : PropertyDrawer
	{

		private GUIContent[] xyzconent = new GUIContent[3] { new GUIContent("X"), new GUIContent("Y"), new GUIContent("Z") };
		private GUIStyle lefttextstyle = new GUIStyle
		{
			alignment = TextAnchor.UpperLeft
		};

		public override void OnGUI(Rect r, SerializedProperty property, GUIContent label)
		{
			SerializedProperty x = property.FindPropertyRelative("x");
			//SerializedProperty y = property.FindPropertyRelative("y");
			//SerializedProperty z = property.FindPropertyRelative("z");

			//float margin = 4;
			float realwidth = r.width;
			//float halfwidth = r.width / 2;
			float thirdwidth = (float)r.width / 2.4f;
			float split = Mathf.Max(130, thirdwidth);

			EditorGUI.LabelField(new Rect(16, r.yMin, 40, 0), new GUIContent("Include Axis:"), lefttextstyle);
			EditorGUI.MultiPropertyField(new Rect(split, r.yMin, Mathf.Min(realwidth - split, 140) , 16), xyzconent, x);

			////base.OnGUI(position, property, label);
			//EditorGUI.LabelField(new Rect(16, r.yMin, 40, 0), new GUIContent("Include"), lefttextstyle);

			//EditorGUI.LabelField(new Rect(midwidth, r.yMin, 40, 0), new GUIContent("X"), lefttextstyle);
			//EditorGUI.PropertyField(new Rect(midwidth + 40, r.yMin, 1, 10), x, GUIContent.none);

			//EditorGUI.LabelField(new Rect(midwidth + 80, r.yMin, 40, 0), new GUIContent("X"), lefttextstyle);
			//EditorGUI.PropertyField(new Rect(midwidth + 120, r.yMin, 10, 10), y, GUIContent.none);

			//EditorGUI.LabelField(new Rect(midwidth + 160, r.yMin, 40, 0), new GUIContent("X"), lefttextstyle);
			//EditorGUI.PropertyField(new Rect(midwidth + 200, r.yMin, 10, 10), z, GUIContent.none);

		}

	}

}
