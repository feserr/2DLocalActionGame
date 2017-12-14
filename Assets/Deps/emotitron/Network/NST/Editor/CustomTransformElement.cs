using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace emotitron.Network.NST
{
	[CustomPropertyDrawer(typeof(TransformElement))]
	[CanEditMultipleObjects]

	public class TransformElementDrawer : PropertyDrawer
	{
		public static Color red = new Color(.7f, .6f, .6f);
		public static Color green = new Color(.6f, .7f, .6f);
		public static Color blue = new Color(.6f, .6f, .7f);
		public static Color purple = new Color(.3f, .2f, .3f);
		public static Color orange = new Color(.3f, .25f, .2f);
		//protected static Color darkred = red * .5f;
		//protected static Color darkgreen = green * .5f;
		//protected static Color darkblue = blue * .5f;

		protected const int LINEHEIGHT = 16;
		protected float rows = 16;

		protected float margin;
		protected float realwidth;
		protected float colwidths;
		protected float currentLine;
		protected int savedIndentLevel;
		protected bool isPos;

		protected SerializedProperty elementType;
		protected SerializedProperty isRoot;
		protected SerializedProperty keyRate;
		protected SerializedProperty sendCull;
		protected SerializedProperty gameobject;
		protected SerializedProperty extrapolation;

		public static GUIStyle lefttextstyle = new GUIStyle
		{
			alignment = TextAnchor.UpperLeft,
			richText = true
		};

		public override void OnGUI(Rect r, SerializedProperty property, GUIContent label)
		{
			elementType = property.FindPropertyRelative("elementType");
			isRoot = property.FindPropertyRelative("isRoot");
			keyRate = property.FindPropertyRelative("keyRate");
			sendCull = property.FindPropertyRelative("sendCulling");
			gameobject = property.FindPropertyRelative("gameobject");
			extrapolation = property.FindPropertyRelative("extrapolation");

			isPos = (ElementType)elementType.intValue == ElementType.Position;

			margin = 4;
			realwidth = r.width + 16 - 4;
			colwidths = realwidth / 4f;

			colwidths = Mathf.Max(colwidths, 65); // limit the smallest size so things like sliders aren't shrunk too small to draw.

			//r = EditorGUI.PrefixLabel(r, GUIUtility.GetControlID(FocusType.Passive), label);

			currentLine = r.y + margin * 2;

			Color headerblockcolor = (isPos ? orange : purple);

			EditorGUI.DrawRect(new Rect(margin, r.y + 2, realwidth, r.height - margin), Color.black);// new Color(.33f, .33f, .33f));
			EditorGUI.DrawRect(new Rect(margin + 1, r.y + 2 + 1, realwidth - 2, r.height - margin - 2), new Color(.66f, .66f, .66f));
			EditorGUI.DrawRect(new Rect(margin + 1, r.y + 2 + 1, realwidth - 2, LINEHEIGHT * 2 + 18), headerblockcolor);

			savedIndentLevel = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 1; //(isRoot.boolValue) ? 1 : 2;

			string typeLable = (isPos) ? "Position" : "Rotation";
			string headerLabel = "<color=white><b>" + ((isRoot.boolValue) ? "Root " + typeLable : typeLable + " Element:</b>") + "</color>";
			EditorGUI.LabelField(new Rect(margin, currentLine, colwidths * 4, LINEHEIGHT), new GUIContent(headerLabel), lefttextstyle);

			//if (GUI.Button(new Rect(r.width - 30, currentLine, 30, LINEHEIGHT), new GUIContent("X")))
			//	Debug.Log(property);

			currentLine += LINEHEIGHT + 4;

			EditorGUI.PropertyField(new Rect(0, currentLine, 136, LINEHEIGHT), sendCull, GUIContent.none);
			EditorGUI.PropertyField(new Rect(margin + 120, currentLine, realwidth - 120 - margin, LINEHEIGHT), gameobject, GUIContent.none);

			currentLine += LINEHEIGHT + 12;

			EditorGUI.PropertyField(new Rect(0, currentLine, realwidth, LINEHEIGHT), extrapolation, new GUIContent("Extrapolation:"));
			currentLine += LINEHEIGHT + 4;


			if (((SendCulling)sendCull.intValue).UsesKeyframe())
			{
				EditorGUI.PropertyField(new Rect(0, currentLine, realwidth, LINEHEIGHT), keyRate, new GUIContent("Key Every:"));
				currentLine += LINEHEIGHT + 4;
			}
		}
	}
}