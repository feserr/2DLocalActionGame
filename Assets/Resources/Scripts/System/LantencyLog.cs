using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LantencyLog : MonoBehaviour {
    void Update()
    {
    }

    void OnGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Space(10f);
            GUILayout.BeginVertical();
            GUILayout.Label("Player ping values");
            int i = 0;

		    while (i < Network.connections.Length)
		    {
                GUILayout.Space(10f);
                GUILayout.Label("Player " + Network.connections[i] + " - " +
                    Network.GetAveragePing(Network.connections[i]) + " ms");

                i++;
            }

            GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }
}
