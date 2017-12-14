using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Rotator : MonoBehaviour
{
	public float speed = 20;
	[HideInInspector] public NetworkIdentity ni;

	public void Awake()
	{
		ni = transform.root.GetComponent<NetworkIdentity>();
	}

	// Update is called once per frame
	void Update ()
	{
		// Only objects with authority should be moving things.
		if (ni.hasAuthority)
			transform.localEulerAngles = new Vector3(0, 0, Time.time * speed % 360);
		else
			Destroy(this);
	}
}
