using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Startup : MonoBehaviour
{
	void Start ()
	{
		// Make sure screen is big enough on mobile to mess with the network buttons.
		if (Screen.width > 1440)
			Screen.SetResolution(Screen.width / 3, Screen.height / 3, false);
	}

}
