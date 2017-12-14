using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using emotitron.Network.NST.Internal;

namespace emotitron.Network.NST
{


	[CustomPropertyDrawer(typeof(PositionElement))]
	[CanEditMultipleObjects]

	public class PositionElementEditor : TransformElementDrawer
	{
		//private const int LINEHEIGHT = 16;
		//private float rows = 16;

		//private GUIStyle lefttextstyle = new GUIStyle
		//{
		//	alignment = TextAnchor.UpperLeft,
		//	richText = true
		//};
		//private GUIStyle centertextstyle = new GUIStyle
		//{
		//	alignment = TextAnchor.UpperCenter
		//};
		//private GUIStyle righttextstyle = new GUIStyle
		//{
		//	alignment = TextAnchor.UpperRight
		//};


		public override void OnGUI(Rect r, SerializedProperty property, GUIContent label)
		{
			base.OnGUI(r, property, label);
			//SerializedProperty includeXYZ = property.FindPropertyRelative("includeXYZ");
			//SerializedProperty isRoot = property.FindPropertyRelative("isRoot");

			//SerializedProperty keyRate = property.FindPropertyRelative("keyRate");
			//SerializedProperty sendCulling = property.FindPropertyRelative("sendCulling");
			//SerializedProperty gameobject = property.FindPropertyRelative("gameobject");

			SerializedProperty compression = property.FindPropertyRelative("compression");
			//SerializedProperty extrapolation = property.FindPropertyRelative("extrapolation");
			SerializedProperty xrange = property.FindPropertyRelative("xrange");
			SerializedProperty yrange = property.FindPropertyRelative("yrange");
			SerializedProperty zrange = property.FindPropertyRelative("zrange");


			//float margin = 4;
			//float realwidth = r.width + 16 - 4;
			//float colwidths = realwidth / 4f;

			//colwidths = Mathf.Max(colwidths, 65); // limit the smallest size so things like sliders aren't shrunk too small to draw.

			////r = EditorGUI.PrefixLabel(r, GUIUtility.GetControlID(FocusType.Passive), label);

			//float currentLine = r.y + margin * 2;

			//EditorGUI.DrawRect(new Rect(margin, r.y + 2, realwidth, r.height - margin), new Color(.33f, .33f, .33f));
			//EditorGUI.DrawRect(new Rect(margin + 1, r.y + 2 + 1, realwidth - 2, r.height - margin - 2), new Color(.66f, .66f, .66f));
			//EditorGUI.DrawRect(new Rect(margin + 1, r.y + 2 + 1, realwidth - 2, LINEHEIGHT * 2 + 18), new Color(.33f, .33f, .33f));

			////if (isRoot.boolValue)
			//int savedIndentLevel = EditorGUI.indentLevel;
			//EditorGUI.indentLevel = 1; //(isRoot.boolValue) ? 1 : 2;

			//string headerLabel = "<color=white>" + ((isRoot.boolValue) ? "Root Position" : "Child Position Element:") + "</color>";
			//EditorGUI.LabelField(new Rect(margin, currentLine, colwidths * 4, LINEHEIGHT), new GUIContent(headerLabel), lefttextstyle);

			//if (GUI.Button(new Rect(r.width - 30, currentLine, 30, LINEHEIGHT), new GUIContent("X")))
			//	Debug.Log(property);

			//currentLine += LINEHEIGHT + 4;

			EditorGUI.PropertyField(new Rect(0, currentLine, 136, LINEHEIGHT), compression, GUIContent.none);
			currentLine += LINEHEIGHT + 10;

			//currentLine += LINEHEIGHT + 12;
			////EditorGUI.PropertyField(new Rect(8, currentLine, realwidth - 8, LINEHEIGHT), includeXYZ, GUIContent.none);
			////currentLine += LINEHEIGHT + 12;

			//EditorGUI.PropertyField(new Rect(0, currentLine, 136, LINEHEIGHT), sendCulling, GUIContent.none);
			//currentLine += LINEHEIGHT + 16;

			//if (((SendCulling)sendCulling.intValue).UsesKeyframe())
			//{
			//	//EditorGUI.LabelField(new Rect(margin, currentLine, 0, LINEHEIGHT), new GUIContent("Key Rate:"), lefttextstyle);
			//	EditorGUI.PropertyField(new Rect(0, currentLine, realwidth, LINEHEIGHT), keyRate, new GUIContent("Update Every:"));
			//	currentLine += LINEHEIGHT;
			//}

			//EditorGUI.PropertyField(new Rect(0, currentLine, realwidth, LINEHEIGHT), extrapolation, new GUIContent("Extrapolation:"));
			//currentLine += LINEHEIGHT + 16;
			float rangerowspace = ((Internal.Compression)compression.intValue == Internal.Compression.LocalRange) ? LINEHEIGHT : 0;

			EditorGUI.PropertyField(new Rect(margin + 7, currentLine - 3, realwidth - 16 + 2, LINEHEIGHT), xrange, new GUIContent(compression.enumValueIndex.ToString()));
			currentLine += LINEHEIGHT + rangerowspace + 12;
			EditorGUI.PropertyField(new Rect(margin + 7, currentLine - 3, realwidth - 16 + 2, LINEHEIGHT), yrange, new GUIContent(compression.enumValueIndex.ToString()));
			currentLine += LINEHEIGHT + rangerowspace + 12;
			EditorGUI.PropertyField(new Rect(margin + 7, currentLine - 3, realwidth - 16 + 2, LINEHEIGHT), zrange, new GUIContent(compression.enumValueIndex.ToString()));
			//currentLine += LINEHEIGHT + 4;
			//EditorGUI.PropertyField(new Rect(margin, currentLine, realwidth, LINEHEIGHT), yrange, GUIContent.none);

			//EditorGUI.BeginProperty(new Rect(0, r.yMax, r.width, 64), new GUIContent("asdfasdfs"), gameobject);
			//EditorGUI.PropertyField(new Rect())
			//EditorGUI.EndProperty();

			// revert to original indent level.
			EditorGUI.indentLevel = savedIndentLevel;

		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return base.GetPropertyHeight(property, label) * rows;  // assuming original is one row
		}
	}


	//[CustomPropertyDrawer(typeof(AxisRange))]
	//[CanEditMultipleObjects]

	//public class AxisRangeDrawer : PropertyDrawer
	//{
	//	SerializedProperty min, max, rez;

	//	public override void OnGUI(Rect r, SerializedProperty property, GUIContent label)
	//	{
	//		if (min == null) min = property.FindPropertyRelative("min");
	//		if (max == null) max = property.FindPropertyRelative("max");
	//		if (rez == null) rez = property.FindPropertyRelative("resolution");


	//		EditorGUI.BeginProperty(r, label, property);

	//		float realwidth = r.width + 16 - 4;
	//		float thirdwidth = (float)r.width / 2.4f;
	//		float split = Mathf.Max(130, thirdwidth);

	//		EditorGUI.LabelField(new Rect(16, r.y, 100, 16), new GUIContent("min:"));
	//		EditorGUI.PropertyField(new Rect(50, r.y, 64, 16), min, GUIContent.none);
	//		EditorGUI.LabelField(new Rect(16 + 50, r.y, 100, 16), new GUIContent("max:"));
	//		EditorGUI.PropertyField(new Rect(100 + 40, r.y, 64, 16), max, GUIContent.none);
	//		EditorGUI.LabelField(new Rect(16 + 100, r.y, 100, 16), new GUIContent("res:"));
	//		EditorGUI.PropertyField(new Rect(150 + 80, r.y, 64, 16), rez, GUIContent.none);

	//		//base.OnGUI(position, property, label);
	//		EditorGUI.EndProperty();
	//	}
	//}

	//[CustomEditor(typeof(PositionRange))]
	//[CanEditMultipleObjects]

	//public class PositionRangeDrawer : Editor
	//{
	//	//SerializedProperty xMin, xMax, yMin, yMax, zMin, zMax, resolution;
	//	SerializedProperty minRanges, maxRanges, resolution;

	//	void OnEnable()
	//	{
	//		minRanges = serializedObject.FindProperty("minRanges");
	//		maxRanges = serializedObject.FindProperty("maxRanges");
	//		resolution = serializedObject.FindProperty("resolution");
	//	}

	//	public override void OnInspectorGUI()
	//	{
	//		//if (xMin == null) xMin = property.FindPropertyRelative("xMin");
	//		//if (xMax == null) xMax = property.FindPropertyRelative("xMax");
	//		//if (yMin == null) yMin = property.FindPropertyRelative("yMin");
	//		//if (yMax == null) yMax = property.FindPropertyRelative("yMax");
	//		//if (zMin == null) zMin = property.FindPropertyRelative("zMin");
	//		//if (xMin == null) xMin = property.FindPropertyRelative("xMin");
	//		//if (zMax == null) zMax = property.FindPropertyRelative("zMax");
	//		//if (minRanges == null) minRanges = property.FindPropertyRelative("minRanges");
	//		//if (maxRanges == null) maxRanges = property.FindPropertyRelative("maxRanges");
	//		//if (resolution == null) resolution = property.FindPropertyRelative("resolution");

	//		EditorGUILayout.PropertyField(minRanges);
	//		EditorGUILayout.PropertyField(maxRanges);
	//		EditorGUILayout.PropertyField(resolution);
	//		//EditorGUILayout.MinMaxSlider(ref xMin.floatValue, ref xMax.floatValue, -100, 100);
	//	}
	//}



}
