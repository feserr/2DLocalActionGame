using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using emotitron.SmartVars;
//using emotitron.Network.NST;

namespace emotitron.Network.NST
{
	[CustomPropertyDrawer(typeof(RotationElement))]
	[CanEditMultipleObjects]

	public class RotationElementDrawer : TransformElementDrawer
	{
		//private const int LINEHEIGHT = 16;
		//private float rows = 16;

		//float currentLine, margin, realwidth, colwidths, col0; //, col1; //, col2; //, col3;

		//public bool showElement = true;

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

			SerializedProperty rotationType = property.FindPropertyRelative("rotationType");
			
			SerializedProperty useLocal = property.FindPropertyRelative("useLocal");
			SerializedProperty totalBitsForQuat = property.FindPropertyRelative("totalBitsForQuat");

			SerializedProperty xBits = property.FindPropertyRelative("xBits");
			SerializedProperty xLimitRange = property.FindPropertyRelative("xLimitRange");
			SerializedProperty xMinVal = property.FindPropertyRelative("xMinValue");
			SerializedProperty xMaxVal = property.FindPropertyRelative("xMaxValue");

			SerializedProperty yBits = property.FindPropertyRelative("yBits");
			SerializedProperty yLimitRange = property.FindPropertyRelative("yLimitRange");
			SerializedProperty yMinVal = property.FindPropertyRelative("yMinValue");
			SerializedProperty yMaxVal = property.FindPropertyRelative("yMaxValue");

			SerializedProperty zBits = property.FindPropertyRelative("zBits");
			SerializedProperty zLimitRange = property.FindPropertyRelative("zLimitRange");
			SerializedProperty zMinVal = property.FindPropertyRelative("zMinValue");
			SerializedProperty zMaxVal = property.FindPropertyRelative("zMaxValue");


			EditorGUI.PropertyField(new Rect(0, currentLine, 136, LINEHEIGHT), rotationType, GUIContent.none);
			EditorGUI.PropertyField(new Rect(margin + 120, currentLine, colwidths, LINEHEIGHT), useLocal, GUIContent.none);
			EditorGUI.LabelField(new Rect(24 + 120, currentLine, colwidths, LINEHEIGHT), new GUIContent("Lcl Rotation"), lefttextstyle);


			//if (sendCulling.intValue > 0)
			//{
			//	currentLine += LINEHEIGHT + margin;
			//	EditorGUI.LabelField(new Rect(margin, currentLine, 0, LINEHEIGHT), new GUIContent("Key Rate:"), lefttextstyle);
			//	EditorGUI.PropertyField(new Rect(margin + 90, currentLine, realwidth - 90 - 8, LINEHEIGHT), keyRate, GUIContent.none);
			//}

			//currentLine += LINEHEIGHT + 4;

			currentLine += LINEHEIGHT + 8;

			if (rotationType.intValue == (int)XType.Quaternion)
			{
				EditorGUI.LabelField(new Rect(margin, currentLine, colwidths * 4, LINEHEIGHT), new GUIContent("Total Bits Used:"), lefttextstyle);
				currentLine += LINEHEIGHT;
				EditorGUI.PropertyField(new Rect(margin, currentLine, colwidths * 4 - 16, LINEHEIGHT), totalBitsForQuat, GUIContent.none);
				currentLine += LINEHEIGHT + margin;
				string helpstr = "Some reference values for bit rates:\n" + "40 bits = avg err ~0.04° / max err ~0.1°\n" + "48 bits = avg err ~0.01° / max err ~0.07°";
				EditorGUI.HelpBox(new Rect(margin + 12, currentLine, realwidth - 24, LINEHEIGHT * 4), helpstr, MessageType.None);
			}

			if (((XType)rotationType.intValue).IsX())
			{
				DrawAxis("X", r, red, xBits, xLimitRange, xMinVal, xMaxVal);
			}

			if (((XType)rotationType.intValue).IsY())
			{
				DrawAxis("Y", r, green, yBits, yLimitRange, yMinVal, yMaxVal);
			}

			if (((XType)rotationType.intValue).IsZ())
			{
				DrawAxis("Z", r, blue, zBits, zLimitRange, zMinVal, zMaxVal);
			}


			//EditorGUI.EndProperty();
		EditorGUI.indentLevel = savedIndentLevel;

		}

		public void DrawAxis(string axisStr, Rect r, Color color,
			SerializedProperty bits, SerializedProperty limitRange, SerializedProperty minVal, SerializedProperty maxVal)
		{
			EditorGUI.DrawRect(new Rect(margin + 7, currentLine - 3, realwidth - 16 + 2, LINEHEIGHT * 2 + 10), Color.black);
			EditorGUI.DrawRect(new Rect(margin + 8, currentLine - 2, realwidth - 16, LINEHEIGHT * 2 + 8), color);
			EditorGUI.DrawRect(new Rect(margin + 8, currentLine - 2, realwidth - 16, LINEHEIGHT + 4), color * .5f);

			EditorGUI.LabelField(new Rect(margin, currentLine - 1, colwidths, LINEHEIGHT), new GUIContent(axisStr + " bits:"), lefttextstyle);
			EditorGUI.PropertyField(new Rect(margin + 56, currentLine - 1, realwidth - 52 - 16, LINEHEIGHT), bits, GUIContent.none); // new GUIContent("x Bits"));
			currentLine += LINEHEIGHT + 4;

			EditorGUI.PropertyField(new Rect(margin, currentLine, 32, LINEHEIGHT), limitRange, GUIContent.none);
			if (limitRange.boolValue == true)
			{
				EditorGUI.PropertyField(new Rect(30, currentLine, 52, LINEHEIGHT), minVal, GUIContent.none, true); // new GUIContent("x Bits"));
				EditorGUI.PropertyField(new Rect(realwidth - 62, currentLine, 52, LINEHEIGHT), maxVal, GUIContent.none, true); // new GUIContent("x Bits"));

				float tempfloat = minVal.floatValue;
				float tempfloat2 = maxVal.floatValue;
				EditorGUI.MinMaxSlider(new Rect(80, currentLine, realwidth - 80 - 60, LINEHEIGHT), ref tempfloat, ref tempfloat2, -360, 360);
				minVal.floatValue = Mathf.Max((int)tempfloat, -360);
				maxVal.floatValue = Mathf.Min((int)tempfloat2, 360);

			} else
			{
				EditorGUI.LabelField(new Rect(margin + 24, currentLine, colwidths, LINEHEIGHT), new GUIContent("Range"), lefttextstyle);
			}

			currentLine += 25;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return base.GetPropertyHeight(property, label) * rows;  // assuming original is one row
		}
	}
}

