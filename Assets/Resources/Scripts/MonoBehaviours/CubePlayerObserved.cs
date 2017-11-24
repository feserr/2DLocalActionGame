using System.Collections.Generic;
using UnityEngine;

public class CubePlayerObserved : MonoBehaviour, ICubeStateHandler {
	int clientTick;
	CubeState interpolated;
	CubePlayer player;
	LinkedList<CubeState> stateBuffer;
	
	void Awake () {
		player = GetComponent<CubePlayer> ();
		stateBuffer = new LinkedList<CubeState> ();
		SetObservedState (player.serverState);
		AddState (player.serverState);
	}

	void FixedUpdate () {
		clientTick++;
		LinkedListNode<CubeState> fromNode = stateBuffer.First;
		LinkedListNode<CubeState> toNode = fromNode.Next;
		while ((toNode != null) && (toNode.Value.timestamp <= clientTick)) {
			fromNode = toNode;
			toNode = fromNode.Next;
			stateBuffer.RemoveFirst ();
		}
		SetObservedState ((toNode != null) ? CubeState.Interpolate (fromNode.Value, toNode.Value, clientTick) : fromNode.Value);
	}

	public void OnStateChanged (CubeState newState) {
		AddState (newState);
	}

	void AddState (CubeState state) {
		stateBuffer.AddLast (state);
		clientTick = state.timestamp - player.interpolationDelay;
		while (stateBuffer.First.Value.timestamp <= clientTick) {
			stateBuffer.RemoveFirst ();
		}
		interpolated.timestamp = Mathf.Max (clientTick, stateBuffer.First.Value.timestamp - player.inputBufferSize);
		stateBuffer.AddFirst (interpolated);
		if (interpolated.timestamp <= clientTick) return;
		interpolated.timestamp = clientTick;
		stateBuffer.AddFirst (interpolated);
	}

	void SetObservedState (CubeState newState) {
		interpolated = newState;
		player.SyncState (interpolated);
	}
}
