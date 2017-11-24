using System.Collections.Generic;
using UnityEngine;

public class CubePlayerInput : MonoBehaviour {
	List<Vector2> inputBuffer;
	CubePlayer player;
	CubePlayerPredicted predicted;

	void Awake () {
		inputBuffer = new List<Vector2> ();
		player = GetComponent<CubePlayer> ();
		predicted = GetComponent<CubePlayerPredicted> ();
	}

	void FixedUpdate () {
		Vector2 input = Input.GetAxis ("Horizontal") * Vector2.right + Input.GetAxis ("Vertical") * Vector2.up;
		if ((inputBuffer.Count == 0) && (input == Vector2.zero)) return;
		predicted.AddInput (input);
		inputBuffer.Add (input);
		if (inputBuffer.Count < player.inputBufferSize) return;
		player.CmdMove (inputBuffer.ToArray ());
		inputBuffer.Clear ();
	}
}
