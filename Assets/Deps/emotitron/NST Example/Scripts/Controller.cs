using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using emotitron.Network.NST;

/// <summary>
/// A VERY bad example of a character controller. Stricty thrown together for demo purposes.
/// </summary>
public class Controller : NetworkBehaviour
{
	private Rigidbody rb;
	private Weapon weapon;
	public GameObject turretGO;

	private const float rotationForce = 30f;
	private const float thrustForce = 300f;

	private void Awake()
	{
		rb = GetComponent<Rigidbody>();
		weapon = GetComponent<Weapon>();
	}
	// Use this for initialization
	void Start ()
	{
		//if (hasAuthority)
		//	rb.isKinematic = false;
		//else
		//	rb.isKinematic = true;
	}

	// Update is called once per frame
	// Update is called once per frame
	void Update()
	{

		// Only accept input for the local player
		if (!hasAuthority)
			return;

		if (Input.GetKeyDown("t"))
			NetworkSyncTransform.lclNST.Teleport(NSTMapBounds.combinedBounds.center);

		if (Input.GetKeyDown("f"))
			weapon.PlayerFire(Weapon.WeaponType.Bullet);

		if (Input.GetKeyDown("space"))
			weapon.PlayerFire(Weapon.WeaponType.Hitscan);

		if (Input.GetKeyDown("3"))
			weapon.PlayerFire(Weapon.WeaponType.Mine);

		if (Input.GetKey("a"))
			rb.AddRelativeForce(new Vector3(-thrustForce * Time.deltaTime, 0, 0));

		if (Input.GetKey("d"))
			rb.AddRelativeForce(new Vector3(thrustForce * Time.deltaTime, 0, 0));

		if (Input.GetKey("w"))
			rb.AddRelativeForce(new Vector3(0, 0, thrustForce * Time.deltaTime));

		if (Input.GetKey("s"))
			rb.AddRelativeForce(new Vector3(0, 0, -thrustForce * Time.deltaTime));

		if (Input.GetKey("q"))
			rb.AddRelativeTorque(new Vector3(0, -rotationForce * Time.deltaTime, 0));

		if (Input.GetKey("e"))
			rb.AddRelativeTorque(new Vector3(0, rotationForce * Time.deltaTime, 0));

		if (Input.GetKey("f"))
			rb.AddRelativeTorque(new Vector3(0, 0, -rotationForce * Time.deltaTime));

		if (Input.GetKey("g"))
			rb.AddRelativeTorque(new Vector3(0, 0, rotationForce * Time.deltaTime));

		if (Input.GetKey("r"))
			rb.AddRelativeTorque(new Vector3(rotationForce * Time.deltaTime, 0, 0));

		if (Input.GetKey("c"))
			rb.AddRelativeTorque(new Vector3(-rotationForce * Time.deltaTime, 0, 0));

		if (Input.GetKey(","))
			turretGO.transform.localRotation = Quaternion.Euler(
				turretGO.transform.localRotation.eulerAngles.x,
				turretGO.transform.localRotation.eulerAngles.y - 30 * Time.deltaTime,
				turretGO.transform.localRotation.eulerAngles.z);

		if (Input.GetKey("."))
			turretGO.transform.localRotation = Quaternion.Euler(
				turretGO.transform.localRotation.eulerAngles.x,
				turretGO.transform.localRotation.eulerAngles.y + 30 * Time.deltaTime,
				turretGO.transform.localRotation.eulerAngles.z);

		if (Input.GetKey("k"))
			turretGO.transform.localRotation = Quaternion.Euler(
				turretGO.transform.localRotation.eulerAngles.x - 30 * Time.deltaTime,
				turretGO.transform.localRotation.eulerAngles.y,
				turretGO.transform.localRotation.eulerAngles.z);

		if (Input.GetKey("l"))
			turretGO.transform.localRotation = Quaternion.Euler(
				turretGO.transform.localRotation.eulerAngles.x + 30 * Time.deltaTime,
				turretGO.transform.localRotation.eulerAngles.y,
				turretGO.transform.localRotation.eulerAngles.z);

		if (Input.GetKey("m"))
			turretGO.transform.localPosition = new Vector3(
				turretGO.transform.localPosition.x - (1 * Time.deltaTime),
				turretGO.transform.localPosition.y,
				turretGO.transform.localPosition.z);

		if (Input.GetKey("/"))
			turretGO.transform.localPosition = new Vector3(
				turretGO.transform.localPosition.x + (1 * Time.deltaTime),
				turretGO.transform.localPosition.y,
				turretGO.transform.localPosition.z);

		// Some touch inputs for testing on mobile.
		for (var i = 0; i < Input.touchCount; ++i)
		{
			Touch touch = Input.GetTouch(i);
			if (touch.phase == TouchPhase.Stationary || touch.phase == TouchPhase.Began)
			{

				if (touch.position.x < (Screen.width * .33f))
				{
					rb.AddRelativeTorque(- transform.up * 30 * Time.deltaTime);
				}
				else if (touch.position.x > (Screen.width *.667f))
				{
					rb.AddRelativeTorque(transform.up * 30 * Time.deltaTime);
				}

				if (touch.position.y < (Screen.height * .33f))
				{
					rb.AddForce(-transform.forward * thrustForce * Time.deltaTime);
				}
				else if (touch.position.y > (Screen.height * .66f))
				{
					rb.AddForce(transform.forward * thrustForce * Time.deltaTime);
				}
			}
			else if (touch.phase == TouchPhase.Ended)
			{
				weapon.PlayerFire(Weapon.WeaponType.Hitscan);
			}
		}
	}
}
