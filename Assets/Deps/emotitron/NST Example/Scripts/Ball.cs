using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Ball : NetworkBehaviour {

	Rigidbody rb;
	private void Awake()
	{
		// TEST
		//Application.targetFrameRate = 100;
		rb = GetComponent<Rigidbody>();
	}

	
	private void OnCollisionEnter(Collision collision)
	{
		rb.velocity = rb.velocity.normalized * 15;
		rb.AddTorque(new Vector3(Random.value, Random.value, Random.value) * 20);
	}
}
