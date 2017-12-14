using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mover : MonoBehaviour
{
	public float range;
	public float rate;

	//void Awake () {
		
	//}

	private void Start()
	{
		if (!UnityEngine.Networking.NetworkServer.active)
		{
			Destroy(this);
		}
	}
	void Update ()
	{
		gameObject.transform.localPosition = new Vector3(0, 0, Mathf.Sin(Time.time * rate) * range);


	}
}
