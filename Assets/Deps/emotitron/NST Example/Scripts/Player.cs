using UnityEngine;
using UnityEngine.Networking;
using emotitron.Network.NST;

/// <summary>
/// You should really have a Player class for your players to manage
/// </summary>
public class Player : NetworkBehaviour
{
	public NetworkSyncTransform nst;
	public Rigidbody rb;
	public Weapon weapon;
	public Camera cam;
	private Camera defaultCam;
	private GameObject myTeleportButton;
	public static Player localPlayer;

	void Awake ()
	{


		rb = GetComponent<Rigidbody>();
		weapon = GetComponent<Weapon>();
		nst = GetComponent<NetworkSyncTransform>();
	}

	public override void OnStartServer()
	{
		myTeleportButton = TeleportButtons.single.AddTeleportButton(nst);
	}

	public override void OnStartLocalPlayer()
	{
		localPlayer = this;
		defaultCam = Camera.main;
		defaultCam.gameObject.SetActive(false);
		cam.gameObject.SetActive(true);
	}

	private void OnDestroy()
	{
		try
		{
			if (hasAuthority)
				defaultCam.gameObject.SetActive(true);
		}
		catch
		{

		}


		Destroy(myTeleportButton);
	}




}
