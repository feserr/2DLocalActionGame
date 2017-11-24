using UnityEngine;
using UnityEngine.Networking;

[NetworkSettings(channel=2)] public class CubePlayer : NetworkBehaviour {
	public int inputBufferSize = 5;
	public int interpolationDelay = 10;

	[SyncVar(hook="OnServerStateChanged")] public CubeState serverState;

	ICubeStateHandler stateHandler;
	CubePlayerServer server;

	void Awake () {
		AwakeOnServer ();
	}

	[Server] void AwakeOnServer () {
		server = gameObject.AddComponent<CubePlayerServer> ();
	}

	void Start () {
		if (!isLocalPlayer) {
			stateHandler = (ICubeStateHandler)gameObject.AddComponent<CubePlayerObserved> ();
			return;
		}
		stateHandler = (ICubeStateHandler)gameObject.AddComponent<CubePlayerPredicted> ();
		gameObject.AddComponent<CubePlayerInput> ();
	}

	[Command(channel=0)] public void CmdMove (Vector2[] inputs) {
		server.Move (inputs);
	}

	public void SyncState (CubeState stateToUse) {
		transform.position = stateToUse.position;
	}

	void OnServerStateChanged (CubeState newState) {
		serverState = newState;
		if (stateHandler == null) return;
		stateHandler.OnStateChanged (serverState);
	}
}
