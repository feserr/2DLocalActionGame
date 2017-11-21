using UnityEngine;
using System.Collections;

public class FPSDisplay : MonoBehaviour
{
    private float _deltaTime = 0.0f;
    private Rect _auxRect;
    private Color _auxColor;
    GUIStyle _style;

    void Update()
    {
        _deltaTime += (Time.deltaTime - _deltaTime) * 0.1f;
        _auxRect = new Rect(0.0f, 0.0f, 0.0f, 0.0f);
        _auxColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        _style = new GUIStyle();
    }

    void OnGUI()
    {
        int w = Screen.width, h = Screen.height;

        _auxRect.Set(0, 0, w, h * 2 / 100);
        _style.alignment = TextAnchor.UpperRight;
        _style.fontSize = h * 2 / 30;
        _style.normal.textColor = _auxColor;
        float msec = _deltaTime * 1000.0f;
        float fps = 1.0f / _deltaTime;
        string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
        GUI.Label(_auxRect, text, _style);
    }
}