using System.Collections.Generic;
using UnityEngine;

public class CubePlayerPredicted : MonoBehaviour, ICubeStateHandler {
	Queue<Vector2> pendingMoves;
	CubePlayer player;
	CubeState predictedState;

	void Awake () {
		pendingMoves = new Queue<Vector2> ();
		player = GetComponent<CubePlayer> ();
		UpdatePredictedState ();
	}

	public void AddInput (Vector2 input) {
		pendingMoves.Enqueue (input);
		UpdatePredictedState ();
	}

	public void OnStateChanged (CubeState newState) {
		while (pendingMoves.Count > (predictedState.moveNum - player.serverState.moveNum)) {
			pendingMoves.Dequeue ();
		}
		UpdatePredictedState ();
	}

	void UpdatePredictedState () {
		predictedState = player.serverState;
		foreach (Vector2 input in pendingMoves) {
			predictedState = CubeState.Move (predictedState, input, 0);
		}
		player.SyncState (predictedState);
	}
}
