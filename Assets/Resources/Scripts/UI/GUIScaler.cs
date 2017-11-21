using UnityEngine;
using System.Collections;

public class GUIScaler : MonoBehaviour
{
    private float _nativeWidth;
    private float _nativeHeight;
    private Vector3 _auxVector;

    void Start()
    {
        _nativeWidth = 480;
        _nativeHeight = 320;
        _auxVector = new Vector3(0.0f, 0.0f, 0.0f);
    }

    void OnGUI()
    {
        //set up scaling
        float rx = Screen.width / _nativeWidth;
        float ry = Screen.height / _nativeHeight;
        _auxVector.Set(rx, ry, 1);

        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
        _auxVector);
    }
}