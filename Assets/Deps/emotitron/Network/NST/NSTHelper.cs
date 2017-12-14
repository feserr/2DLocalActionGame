using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using emotitron.Network.Compression;
using emotitron.BitToolsUtils;

namespace emotitron.Network.NST.Internal
{

	public static class NSTHelper
	{
		/// <summary>
		/// Replicate entire gameobject as only empty gameobjects and colliders.
		/// </summary>
		public static GameObject CreateRewindObject(NetworkSyncTransform nst, GameObject rootgo)
		{
			GameObject rewgo = new GameObject("Rewind " + rootgo.name);
			CloneChildrenAndColliders(nst, rootgo, rewgo);

			rewgo.transform.parent = rootgo.transform.parent;
			rewgo.transform.localPosition = rootgo.transform.localPosition;
			rewgo.transform.localScale = rootgo.transform.localScale;
			rewgo.transform.localRotation = rootgo.transform.localRotation;

			rewgo.SetActive(false);
			return rewgo;
		}

		/// <summary>
		/// Make a barebones copy of an object including only empty gameobjects and colliders.
		/// </summary>
		private static void CloneChildrenAndColliders(NetworkSyncTransform nst, GameObject rootGO, GameObject targetgo)
		{
			// If we are replicating one of the rotation gameobjects, store its clone
			for (int pe = 0; pe < nst.positionElements.Count; pe++)
			{
				if (rootGO.gameObject == nst.positionElements[pe].gameobject)
				{
					nst.positionElements[pe].rewindGO = targetgo;
				}
			}

			// See if this gameobject is used by rotation elements, if so we want to associate it with that element
			for (int re = 0; re < nst.rotationElements.Count; re++)
			{
				if (rootGO.gameObject == nst.rotationElements[re].gameobject)
					nst.rotationElements[re].rewindGO = targetgo;
			}

			// Find all children
			for (int i = 0; i < rootGO.transform.childCount; i++)
			{
				Transform src = rootGO.transform.GetChild(i);
				Transform copy = new GameObject(src.name).transform;

				copy.parent = targetgo.transform;

				copy.localPosition = src.localPosition;
				copy.localScale = src.localScale;
				copy.localRotation = src.localRotation;

				copy.gameObject.SetActive(src.gameObject.activeSelf);

				Collider orig = src.GetComponent<Collider>();

				if (orig != null && nst.rewindHitLayers.value.GetBitInMask(src.gameObject.layer))  //((src.gameObject.layer & nst.rewindHitLayers.value) > 0))
				{
					Collider newcol = copy.gameObject.AddColliderCopy(orig);
					newcol.isTrigger = true;
				}

				CloneChildrenAndColliders(nst, rootGO.transform.GetChild(i).gameObject, copy.gameObject);
			}
		}

		// Save the last new dictionary opening found to avoid retrying to same ones over and over when finding new free keys.
		public static int nstDictLastCheckedPtr; 
		public static int GetFreeNstId()
		{
			if (NSTSettings.single.bitsForNstId < 6)
			{
				for (int i = 0; i < NetworkSyncTransform.NstIds.Length; i++)
				{
					if (NetworkSyncTransform.NstIds[i] == null)
						return i;
				}
			}
			else
			{
				for (int i = 0; i < 64; i++)
				{
					int offseti = (int)((i + nstDictLastCheckedPtr + 1) % NSTSettings.single.MaxNSTObjects);
					if (NetworkSyncTransform.nstIdToNSTLookup[(uint)offseti] == null)
					{
						nstDictLastCheckedPtr = offseti;
						return offseti;
					}
				}
			}

			Debug.LogError("No more available NST ids. Increase the number Max Nst Objects in NST Settings, or your game will be VERY broken.");
			return -1;
		}

		public static void UpdateLclTransformDebug(NetworkSyncTransform nst, CompressedElement compressedpos, CompressedElement lastSent)
		{
			nst.debugXformGO.transform.position = (nst.cullUpperBits) ?
				NSTCompressVector.OverwriteLowerBits(lastSent, compressedpos).Decompress() :
				compressedpos.Decompress();

			nst.debugXformGO.transform.rotation =
					nst.transform.rotation.CompressQuatToBitsBuffer(nst.rotationElements[0].totalBitsForQuat).DecompressBitBufferToQuat(nst.rotationElements[0].totalBitsForQuat);
		}

		public static string PrintBufferMask(this FrameBuffer buffer, int hilitebit = -1)
		{
			string str = "";
			string tagsEnd = "";

			for (int i = buffer.bufferSize - 1; i >= 0; i--)
			{
				tagsEnd = "";
				if (buffer.frames[i].msgType == MsgType.Low_Bits)
				{
					str += "<color=orange>";
					tagsEnd = "</color>" + tagsEnd;
				}
				else if (buffer.frames[i].msgType == MsgType.No_Positn)
				{
					str += "<color=purple>";
					tagsEnd = "</color>" + tagsEnd;
				}
				if (i == buffer.CurrentIndex)
				{
					str += "<b>";
					tagsEnd = "</b>" + tagsEnd;
				}

				str += (BitToolsUtils.BitTools.GetBitInMask(buffer.validFrameMask, i)) ? 1 : 0;

				str += tagsEnd;

				if (i % 4 == 0)
					str += " ";
			}
			return str + " orng=lbits, purple=no_pos";
		}
	}
}

