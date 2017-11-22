using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LantencyLog : MonoBehaviour {
    void Update()
    {
    }

    void OnGUI()
    {
        GUILayout.Label("Player ping values");
        int i = 0;

		while (i < Network.connections.Length)
		{
            GUILayout.Label("Player " + Network.connections[i] + " - " +
				Network.GetAveragePing(Network.connections[i]) + " ms");
            i++;
        }
    }
}
