using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using emotitron.Network.NST;

public class TeleportButtons : MonoBehaviour {

	[SerializeField] private GameObject buttonPrefab;
	public static TeleportButtons single;
	// Use this for initialization
	void Awake ()
	{
		single = this;	
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public GameObject AddTeleportButton(NetworkSyncTransform nst)
	{
		GameObject buttonGO = Instantiate(buttonPrefab, transform);
		Button button = buttonGO.GetComponent<Button>();
		Text text = buttonGO.transform.GetChild(0).GetComponent<Text>();
		text.text = "Server Teleport Player " + nst.NstId + " to center";

		button.onClick.AddListener(delegate { nst.Teleport(NSTMapBounds.combinedBounds.center); });
		return buttonGO;
	}

}
