using UnityEngine;
using UnityEngine.Networking;

public class SpawnObject : NetworkBehaviour {

	public GameObject prefab;

	public override void OnStartServer()
	{
		GameObject go = Instantiate(prefab, null);
		go.transform.position = transform.position;
		NetworkServer.Spawn(go);
	}
}
