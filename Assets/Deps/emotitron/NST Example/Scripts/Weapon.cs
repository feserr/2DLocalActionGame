using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using emotitron.Network;
using emotitron.Network.NST;
using emotitron.SmartVars;
using emotitron.BitToolsUtils;

/// <summary>
/// A very basic weapon example. Note that this uses the NetworkSyncTransform's 'SendCustomEvent' call. You can add your own custom data to this and it will be sent along
/// in the next NST message. It will be receieved on all clients and generates the OnNstCustomMessageEvent event - which will contain a copy of that custom data.
/// </summary>
public class Weapon : NetworkBehaviour
{
	public enum WeaponType { Bullet, Mine, Hitscan }

	private NetworkSyncTransform nst;
	public GameObject bulletPrefab;
	public GameObject minePrefab;
	public GameObject turret;
	private LineRenderer linerend;

	// Sample custom message struct, you can create your own to send to the NST
	private struct PlayerFireCustomMsg
	{
		public byte weaponId;
		public Color color;
		public uint hitmask;
	}

	void Awake ()
	{
		// This message is fired off when a client NST sends a custom message. The position and rotation information is exactly what is sent
		// The lossy position and rotation data can be used to make sure that your actions locally apply the same rounding errors 
		// that the server and other clients will be using.
		NetworkSyncTransform.OnCustomMsgSndEvent -= OnCustomMsgSnd;
		NetworkSyncTransform.OnCustomMsgSndEvent += OnCustomMsgSnd;

		// these are called as soon as the NST message arrives. Note that the rotations will only be correct if you have the NST set to update
		// rotations on events. If it is set to 'changes only' these rotations values will be zero.
		NetworkSyncTransform.OnCustomMsgRcvEvent -= OnCustomMsgRcv;
		NetworkSyncTransform.OnCustomMsgRcvEvent += OnCustomMsgRcv;

		// these are called when the NST message comes off the buffer and is applied.  Note that the rotations will only be correct 
		// if you have the NST set to update rotations on events. If it is set to 'changes only' these rotations values will be zero.
		NetworkSyncTransform.OnCustomMsgBeginInterpolationEvent -= OnCustomMsgApply;
		NetworkSyncTransform.OnCustomMsgBeginInterpolationEvent += OnCustomMsgApply;

		linerend = GetComponent<LineRenderer>();
	}

	/// <summary>
	/// Player with local authority fires by calling this. This tells the NST to create a custom message and attach your data to it.
	/// </summary>
	/// <param name="wid"></param>
	public void PlayerFire(WeaponType w)
	{
		int hitId = 0;
		uint hitmask = 0;
		if (w == WeaponType.Hitscan)
		{
			hitmask = CastRay(transform.position, transform.rotation, out hitId);
		}

		PlayerFireCustomMsg customMsg = new PlayerFireCustomMsg
		{
			weaponId = (byte)w,
			color = new Color(Random.value, 
			Random.value, Random.value), hitmask = hitmask
		};

		NetworkSyncTransform.SendCustomEventSimple(customMsg);

	}

	/// <summary>
	/// OnCustomMsgSndEvent fires on the originating client when a custom event is sent. The position and rotation information will contain the same
	/// lossy rounding errors/ranges that are being sent to the network. Useful for ensuring that your local events use the exact same pos/rot data
	/// the server and clients will be using (such as projectile vectors).
	/// </summary>
	/// <param name="rootPos">Lossy position after compression - exactly what is sent.</param>
	/// <param name="rotations">Lossy rotation after compression - exactly what is sent.</param>
	private static void OnCustomMsgSnd(NetworkConnection ownerConn, byte[] bytearray, NetworkSyncTransform nst, Vector3 rootPos, List<GenericX> positions, List<GenericX> rotations)
	{
		Weapon wpn = nst.GetComponent<Weapon>();

		PlayerFireCustomMsg weaponFireMsg = bytearray.DeserializeToStruct<PlayerFireCustomMsg>();

		if (weaponFireMsg.weaponId == (byte)WeaponType.Bullet)
			wpn.FireBullet(rootPos, rotations[0], weaponFireMsg);

		else if (weaponFireMsg.weaponId == (byte)WeaponType.Mine)
			wpn.FireMine(wpn.turret.transform.position, rotations[1], weaponFireMsg);
		
	}

	/// <summary>
	/// When a custom message is received, the OnCustomMsgRcvEvent is fired. Note that the rotations will only be correct if you have the NST set to update
	/// rotations on events. If it is set to 'changes only' these rotations values will be zero.
	/// </summary>
	private static void OnCustomMsgRcv(NetworkConnection ownerConn, byte[] bytearray, NetworkSyncTransform shooterNst, Vector3 pos, List<GenericX> positions, List<GenericX> rotations)
	{
		//For this example we fired locally already when the custom message was sent (with OnCustomMsgSend). Firing again here on the local player would cause repeat fire events.
		//Note however that code for the local player can be added here to sync the projectile with the server, by adding a projectileID to your custom events.
		
		Weapon wpn = shooterNst.GetComponent<Weapon>();

		PlayerFireCustomMsg weaponFireMsg = bytearray.DeserializeToStruct<PlayerFireCustomMsg>();

		if ((WeaponType)weaponFireMsg.weaponId == WeaponType.Bullet)
		{
			if (!shooterNst.isLocalPlayer)
				wpn.FireBullet(pos, rotations[0], weaponFireMsg);
		}

		// Hitscan arrive on server/other clients

		else if ((WeaponType)weaponFireMsg.weaponId == WeaponType.Hitscan)
		{
			//DebugText.Log(weaponFireMsg.hitmask.PrintBitMask() + " RCV " + (WeaponType)weaponFireMsg.weaponId);
			uint confirmedHitmask = 0;

			// Draw the graphic if this isn't the local player
			if (!shooterNst.isLocalPlayer)
				wpn.DrawRay(pos, rotations[0]);
			
			// Server needs to test if this was a hit.
			if (NetworkServer.active)
			{
				for (int i = 0; i < 32; i++)
				{
					if (weaponFireMsg.hitmask.GetBitInMask(i) == false)
						continue;

					NetworkSyncTransform hitNst = NetworkSyncTransform.GetNstFromId((uint)i);
					bool hit = hitNst.TestHitscanAgainstRewind(ownerConn, new Ray(pos, (Quaternion)rotations[0] * Vector3.forward));

					if (hit)
						((int)hitNst.NstId).SetBitInMask(ref confirmedHitmask, true);

				}
				DebugText.Log("Rewind Confirmation Mask : \n" + confirmedHitmask.PrintBitMask(), true);
			}
		}
		
	}
	/// <summary>
	/// When a custom message is taken from the buffer and applied for interpolation, the OnCustomMsgRcvEvent is fired. Note that the rotations 
	/// will only be correct if you have the NST set to update rotations on events. If it is set to 'changes only' these rotations values will be zero.
	/// </summary>
	private static void OnCustomMsgApply(NetworkConnection ownerConn, byte[] bytearray, NetworkSyncTransform nst, Vector3 pos, List<GenericX> positions, List<GenericX> rotations)
	{
		Weapon wpn = nst.GetComponent<Weapon>();

		PlayerFireCustomMsg weaponFireMsg = bytearray.DeserializeToStruct<PlayerFireCustomMsg>();

		if (nst == NetworkSyncTransform.lclNST)
			return;

		if (weaponFireMsg.weaponId == (byte)WeaponType.Mine)
			wpn.FireMine(wpn.turret.transform.position, rotations[1], weaponFireMsg);
	}

	// it is advisable to create a reusable byte array of the size you need to avoid GC by creating 'new' every time.
	//private static byte[] reusablebyte = new byte[1];

	/// <summary>
	/// This is the weapon fire code created for this example. It just creates a basic non-synced projectile.
	/// </summary>
	private void FireBullet(Vector3 pos, Quaternion rot, PlayerFireCustomMsg msg)
	{
		GameObject bullet = Instantiate(bulletPrefab);
		Rigidbody rb = bullet.GetComponent<Rigidbody>();
		bullet.transform.position = pos;
		bullet.transform.rotation = rot;
		bullet.GetComponentInChildren<MeshRenderer>().material.color = msg.color;

		Destroy(bullet, 1f);

		rb.velocity = bullet.transform.forward * 10;
	}

	/// <summary>
	/// This is the secondary weapon fire code created for this example. It just creates a basic non-synced projectile.
	/// </summary>
	private void FireMine(Vector3 pos, Quaternion rot, PlayerFireCustomMsg msg)
	{
		Debug.DrawRay(pos, rot * Vector3.forward * 10, Color.red, .5f, true);

		GameObject bullet = Instantiate(minePrefab);
		Rigidbody rb = bullet.GetComponent<Rigidbody>();
		bullet.transform.position = pos;
		bullet.transform.rotation = turret.transform.parent.rotation * rot;
		bullet.GetComponentInChildren<MeshRenderer>().material.color = msg.color;

		Destroy(bullet, 2f);

		rb.velocity = bullet.transform.forward * 5;
	}
	
	RaycastHit[] hits = new RaycastHit[32];

	private uint CastRay(Vector3 pos, Quaternion rot, out int hitId)
	{
		hitId = -1;
		uint hitmask = 0;

		DrawRay(pos, rot);
		
		//RaycastHit raycasthit;
		int hitcount = Physics.RaycastNonAlloc(new Ray(pos, rot * (Vector3.forward)), hits);
		for (int i = 0; i < hitcount; i++)
		{
			NetworkSyncTransform hitNST = hits[i].collider.transform.root.GetComponent<NetworkSyncTransform>();

			if (hitNST == null)
				continue;

			((int)(hitNST.NstId)).SetBitInMask(ref hitmask, true);
		}

		//Debug.Log(hitmask.PrintBitMask());
		DebugText.Log(hitmask.PrintBitMask() + " INIT", true);
		return hitmask;
	}
	private void DrawRay(Vector3 pos, Quaternion rot)
	{
		linerend.enabled = true;
		linerend.SetPosition(0, pos);
		linerend.SetPosition(1, pos + rot * (Vector3.forward * 20f));
		Invoke("HideRay", .25f);
	}

	private void HideRay()
	{
		linerend.enabled = false;
	}
}
