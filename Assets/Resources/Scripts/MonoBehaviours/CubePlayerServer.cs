using System.Collections.Generic;
using UnityEngine;

public class CubePlayerServer : MonoBehaviour {
	Queue<Vector2> inputBuffer;
	int movesMade;
	CubePlayer player;
	int serverTick;

	void Awake () {
		inputBuffer = new Queue<Vector2> ();
		player = GetComponent<CubePlayer> ();
		player.serverState = CubeState.CreateStartingState ();
	}

	void FixedUpdate () {
		serverTick++;
		if (movesMade > 0) {
			movesMade--;
		}
		if (movesMade == 0) {
			CubeState serverState = player.serverState;
			while ((movesMade < player.inputBufferSize) && (inputBuffer.Count > 0)) {
				serverState = CubeState.Move (serverState, inputBuffer.Dequeue (), serverTick);
				movesMade++;
			}
			if (movesMade > 0) {
				player.serverState = serverState;
			}
		}
	}

	public void Move (Vector2[] inputs) {
		foreach (Vector2 input in inputs) {
			inputBuffer.Enqueue (input);
		}
	}
}
