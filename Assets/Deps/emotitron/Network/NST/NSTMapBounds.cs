#if UNITY_EDITOR
#define ENABLE_DEBUGX
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using emotitron.Network.Compression;

namespace emotitron.Network.NST
{

	[AddComponentMenu("Network Sync Transform/NST Map Bounds")]

	public enum FactorBoundsOn { EnableDisable, AwakeDestroy}
	/// <summary>
	/// Put this object on the root of a game map. It needs to encompass all of the areas the player is capable of moving to.
	/// The object must contain a MeshRenderer in order to get the bounds.
	/// Used by the NetworkSyncTransform to scale Vector3 position floats into integers for newtwork compression.
	/// </summary>
	public class NSTMapBounds : MonoBehaviour
	{
		[Tooltip("Awake/Destroy will consider a map element into the world size as long as it exists in the scene (You may need to wake it though). Enable/Disable only factors it in if it is active.")]
		[SerializeField] private FactorBoundsOn factorBoundsOn;

		public static Bounds fallBackBounds = new Bounds(new Vector3(0, 0, 0), new Vector3(2000, 1000, 2000));
		// All bounds accounted for (in case there are more than one active Bounds objects
		public static Bounds combinedBounds = new Bounds(new Vector3(0, 0, 0), new Vector3(2000, 1000, 2000));

		private Bounds myBounds;
		private static List<Bounds> activeBounds = new List<Bounds>();
		public static int ActiveBoundsCount { get { return activeBounds.Count; }  }

		public static bool isInitialized;

		private void Awake()
		{
			// Ensure an NSTSettings exists in case the developer completely missed this step somehow.
			NSTSettings.EnsureSettingsExistsInScene();

			//fallBackBounds = new Bounds(new Vector3(0, 0, 0), new Vector3(1000, 1000, 1000f));

			// Add the root meshrendered and its bounds, if one exists.
			MeshRenderer rootmeshrend = GetComponent<MeshRenderer>();

			// Try to find the starting bounds to which we will add. Need to avoid letting the defulat values of nyBounds get used.
			if(rootmeshrend != null)
				myBounds = rootmeshrend.bounds;

			// Find all of the MeshRenderers in this map - TODO: Make colliders an option as this will likely be more accurate.
			MeshRenderer[] allrenderers = GetComponentsInChildren<MeshRenderer>();

			// if we don't have a starting bounds from the root, get the first found child as the start.
			if (rootmeshrend == null && allrenderers.Length > 0)
				myBounds = allrenderers[0].bounds;

			for (int i = 0; i < allrenderers.Length; i++)
			{
				myBounds.Encapsulate(allrenderers[i].bounds);
			}

			// Add to this bounds set to combinedBounds.
			if (factorBoundsOn == FactorBoundsOn.AwakeDestroy)
			{
				FactorInBounds(true);
			}

		}

		private void Start()
		{
			// only send out the initial bounds update once
			if (isInitialized)
				return;

			isInitialized = true;

			NotifyOtherClassesOfBoundsChange();
		}

		private void OnEnable()
		{
			DebugX.Log("Factoring In MapBound for \"" + name + "\"");

			if (factorBoundsOn == FactorBoundsOn.EnableDisable)
				FactorInBounds(true);
		}

		private void OnDisable()
		{
			DebugX.Log("Factoring Out MapBound for now disabled \"" + name + "\"");

			if (factorBoundsOn == FactorBoundsOn.EnableDisable)
				FactorInBounds(false);
		}

		private void OnDestroy()
		{
			DebugX.Log("Factoring out MapBound for now destroyed \"" + name + "\"");

			if (factorBoundsOn == FactorBoundsOn.AwakeDestroy)
				FactorInBounds(false);
		}

		private void FactorInBounds(bool b)
		{
			if (b)
			{
				if (activeBounds.Count == 0)
					combinedBounds = myBounds;
				else
					combinedBounds.Encapsulate(myBounds);

				activeBounds.Add(myBounds);
			}
			else
			{
				activeBounds.Remove(myBounds);
				RecalculateCombinedBounds();
			}

		}

		/// <summary>
		/// Whenever an instance of NSTMapBounds gets removed, the combinedBounds needs to be rebuilt with this.
		/// </summary>
		private static void RecalculateCombinedBounds()
		{
			if (activeBounds.Count == 0)
			{
				combinedBounds = fallBackBounds;

#if UNITY_EDITOR
				if (NSTSettings.single.logTestingInfo)
					Debug.LogWarning("There are no active NSTMapBounds components in the scene.");
#endif

				NotifyOtherClassesOfBoundsChange();
				return;
			}

			combinedBounds = activeBounds[0];
			for (int i = 1; i < activeBounds.Count; i++)
			{
				combinedBounds.Encapsulate(activeBounds[i]);
			}

			// Notify affected classes of the world size change.
			NotifyOtherClassesOfBoundsChange();
		}

		private static void NotifyOtherClassesOfBoundsChange()
		{
			NSTCompressVector.UpdateForNewBounds();
		}
	}
}

