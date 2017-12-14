//Copyright 2017, Davin Carten, All rights reserved

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using emotitron.BitToolsUtils;
using SlimMath;
using emotitron.Network.Compression;

namespace emotitron.Network.NST
{
	/// <summary>
	/// Compress vector3 to the scale of the map.
	/// </summary>
	public static class NSTCompressVector
	{
		// constructor
		static NSTCompressVector()
		{
			EstablishMinBitsPerAxis();
		}

		public static AxisRange xrange = new AxisRange(0);
		public static AxisRange yrange = new AxisRange(1);
		public static AxisRange zrange = new AxisRange(2);
		// indexer for the above
		public static AxisRange AxisRanges(int i)
		{
			return ((i == 0) ? xrange : (i == 1) ? yrange : zrange);
		}


		// Max values for x number of bits - Theoretically I could do more than 32 bits here if other channels are less than 32.
		// But why really, 32 bits for an axis is EXTREME as it is.
		private static uint[] maxValue = new uint[32]
		{ 0, 1, 3, 7, 15, 31, 63, 127, 255,
			511, 1023, 2047, 4095, 8191, 16383, 32767, 65535,
			131071, 262143, 524287, 1048575, 2097151, 4194303, 8388607, 16777215,
			33554431, 67108863, 134217727, 268435455, 536870911, 1073741823, 2147483647 //, 4294967295
			//8589934591, 17179869183, 34359738367, 68719476735, 137438953471, 274877906943, 549755813887, 1099511627775,
			//2199023255551, 4398046511103, 8796093022207, 17592186044415, 35184372088831, 70368744177663, 140737488355327, 281474976710655
		};
		
		// This section is called on new maps, determines the minimum bit level per channel to meet the min res set in NSTSettings
		//private static int[] bits = new int[3];
		//private static int[] lowerBitcount = new int[3];
		//private static float[] encoder = new float[3];
		//private static float[] decoder = new float[3];

		private static void EstablishMinBitsPerAxis()
		{
			if ((!NSTMapBounds.isInitialized || NSTMapBounds.ActiveBoundsCount == 0) && Time.time > 1)
				Debug.LogError("<b>Scene is missing map bounds</b>, defaulting to a map size of " + NSTMapBounds.combinedBounds + ". Be sure to add NSTMapBounds components to your map to define its bounds. Disregard this message if app is shutting down." );

			for (int axis = 0; axis < 3; axis++)
			{
				AxisRanges(axis).ChangeRange(NSTMapBounds.combinedBounds.min[axis], NSTMapBounds.combinedBounds.max[axis], NSTSettings.single.minPosResolution);

				//bits[i] = GetMinBitsForResolution(NSTSettings.single.minPosResolution, NSTMapBounds.combinedBounds.size[i]);
				//lowerBitcount[i] = bits[i] / 3 * 2;
				//encoder[i] = maxValue[bits[i]] / NSTMapBounds.combinedBounds.size[i];
				//decoder[i] = NSTMapBounds.combinedBounds.size[i] / maxValue[bits[i]];
			}

			DebugX.LogWarning("Change in Map Bounds (Due to an NSTBounds being added or removed from the scene). Be sure this map change is happening to all networked clients or things will break badly. \n" +
				"Position keyframes will be x:" + AxisRanges(0).bits + " bits, y:" + AxisRanges(1).bits + "bits, and z:" + AxisRanges(2).bits + 
				" bits at the current minimum resolutons settings (in NST Settings).");
		}

		// Determine the number of bits needed to achieve the minres set in NSTSettings
		private static int GetMinBitsForResolution(float minRes, float mapSize)
		{
			// find the first available bit depth that will give enough resolution at this map size
			for (int i = 0; i < maxValue.Length; i++)
			{
				if (maxValue[i] / mapSize >= minRes)
					return i;
			}

			//TODO: add float support for when this fails?
			DebugX.LogWarning("Cannot give requested position resolution of " + minRes + ", your world must be VERY large and your desired resolution VERY small and exceeds the ability of NST.");

			return maxValue.Length - 1;
		}

		// Called by Map Manager when a new map is loaded
		/// <summary>
		/// Called when new map is loaded (by the NSTMapBounds component). Sets the v3 multipliers for the map bounds. 
		/// These multipliers do some of the conversion from v3 float to unsigned in advance, rather than every network update.
		/// </summary>
		public static void UpdateForNewBounds()
		{
			// indicate whether a map bounds exists - if not raw floats will be used.
			EstablishMinBitsPerAxis();
		}

		public static void WriteCompPosToBitstream(this CompressedElement compressedpos, ref UdpKit.UdpBitStream bitstream, XYZBool _includeXYZ, bool lowerBitsOnly = false)
		{
			for (int axis = 0; axis < 3; axis++)
			{
				//TODO: use lowerbits from inside ranges
				if (_includeXYZ[axis]) bitstream.WriteUInt(compressedpos[axis], lowerBitsOnly ? AxisRanges(axis).lowerBits : AxisRanges(axis).bits);
			}
		}

		public static void WriteCompressedAxisToBitstream(this uint val, int axis, ref UdpKit.UdpBitStream bitstream, bool lowerBitsOnly = false)
		{
			//TODO: use lowerbits from inside ranges
			bitstream.WriteUInt(val, lowerBitsOnly ? AxisRanges(axis).lowerBits : AxisRanges(axis).bits);
		}

		public static uint WriteAxisToBitstream(this float val, int axis, ref UdpKit.UdpBitStream bitstream, bool lowerBitsOnly = false)
		{
			//TODO: use lowerbits from inside ranges
			uint compressedAxis = val.CompressAxis(axis);
			bitstream.WriteUInt(val.CompressAxis(axis), lowerBitsOnly ? AxisRanges(axis).lowerBits : AxisRanges(axis).bits);
			return compressedAxis;
		}

		public static uint CompressAxis(this float val, int axis)
		{
			//TODO: return value frame ranges
			return AxisRanges(axis).Encode(val);
		}

		public static CompressedElement CompressPos(this Vector3 pos)
		{
			//TODO: clamp these?
			//TODO: use ranges values
			return new CompressedElement(
				AxisRanges(0).Encode(pos.x),
				AxisRanges(1).Encode(pos.y),
				AxisRanges(2).Encode(pos.z));
		}

		public static int ReadCompressedAxisFromBitstream(ref UdpKit.UdpBitStream bitstream, int axis, bool lowerBitsOnly = false)
		{
			return (bitstream.ReadInt(lowerBitsOnly ? AxisRanges(axis).lowerBits : AxisRanges(axis).bits));
		}

		public static float ReadAxisFromBitstream(ref UdpKit.UdpBitStream bitstream, int axis, bool lowerBitsOnly = false)
		{
			uint compressedAxis = bitstream.ReadUInt(lowerBitsOnly ? AxisRanges(axis).lowerBits : AxisRanges(axis).bits);

			return compressedAxis.DecompressAxis(axis);
		}

		private static float DecompressAxis(this uint val, int axis)
		{
			return AxisRanges(axis).Decode(val);
		}

		public static CompressedElement ReadCompressedPosFromBitstream(ref UdpKit.UdpBitStream bitstream, XYZBool _includeXYZ, bool lowerBitsOnly = false)
		{
			return new CompressedElement(
				(_includeXYZ[0]) ? (bitstream.ReadUInt(lowerBitsOnly ? AxisRanges(0).lowerBits : AxisRanges(0).bits)) : 0,
				(_includeXYZ[1]) ? (bitstream.ReadUInt(lowerBitsOnly ? AxisRanges(1).lowerBits : AxisRanges(1).bits)) : 0,
				(_includeXYZ[2]) ? (bitstream.ReadUInt(lowerBitsOnly ? AxisRanges(2).lowerBits : AxisRanges(2).bits)) : 0);
		}

		private static Vector3 Decompress(uint x, uint y, uint z)
		{
			return new Vector3
				(
					AxisRanges(0).Decode(x),
					AxisRanges(1).Decode(y),
					AxisRanges(2).Decode(z)
				);
		}
		public static Vector3 Decompress(this CompressedElement compos)
		{
			return new Vector3
				(
					AxisRanges(0).Decode(compos.x),
					AxisRanges(1).Decode(compos.y),
					AxisRanges(2).Decode(compos.z)
				);
		}

		private static bool TestMatchingUpper(uint a, uint b, int lowerbits)
		{
			return (((a >> lowerbits) << lowerbits) == ((b >> lowerbits) << lowerbits));
		}

		//TODO make this accept an lowerbit level (half/twothirds)
		public static bool TestMatchingUpper(CompressedElement prevPos, CompressedElement b)
		{
			return
				(
				TestMatchingUpper(prevPos.x, b.x, AxisRanges(0).lowerBits) &&
				TestMatchingUpper(prevPos.y, b.y, AxisRanges(1).lowerBits) &&
				TestMatchingUpper(prevPos.z, b.z, AxisRanges(2).lowerBits)
				);
		}


		//public static CompressedPos GuessUpperBitsOld(CompressedPos oldcpos, CompressedPos newcpos)
		//{
		//	CompressedPos bestGuess = new CompressedPos();
		//	CompressedPos oldUppers = oldcpos.ZeroLowerBits();

		//	for (int i = 0; i < 3; i++)
		//	{
		//		uint original = newcpos[i] | oldUppers[i];
				
		//		// guess if the upperbits increased or decreased based on whether lower bits were a high or low number
		//		uint prevLowers = oldcpos[i] & maxValue[lowerBitcount[i]];
		//		uint midwayValue = maxValue[lowerBitcount[i] - 1];

		//		// value that will increase or decrease the upperbits by one
		//		int increment = (prevLowers > midwayValue) ? (1 << lowerBitcount[i]) : -(1 << lowerBitcount[i]);
		//		uint offset = newcpos[i] | (oldUppers[i] + increment);

		//		int distorig = Mathf.Abs(oldcpos[i] - original);
		//		int distoffs = Mathf.Abs(oldcpos[i] - offset);

		//		bestGuess[i] = (distorig < distoffs) ? original : offset;
		//		if (distorig > distoffs)  Debug.Log("Used guess ");
		//	}
		//	return bestGuess;
		//}

		/// <summary>
		/// Attempts to guess the most likely upperbits state by seeing if each axis of the new position would be
		/// closer to the old one if the upper bit is incremented by one, two, three etc. Stops trying when it fails to get a better result than the last increment.
		/// </summary>
		/// <param name="oldcpos">Last best position test against.</param>
		/// <returns>Returns a corrected CompressPos</returns>
		public static CompressedElement GuessUpperBits(this CompressedElement newcpos, CompressedElement oldcpos)
		{
			CompressedElement oldUppers = oldcpos.ZeroLowerBits();
			CompressedElement newLowers = newcpos.ZeroUpperBits();
			CompressedElement bestGuess = oldUppers + newLowers;

			for (int i = 0; i < 3; i++)
			{
				// value that will increase or decrease the upperbits by one
				uint increment = ((uint)1 << AxisRanges(i).lowerBits);
				int multiplier = 1;

				// start by just applying the old uppers to the new lowers. This is the distance to beat.
				//uint lastguess = oldUppers[i] | newLowers[i];
				long lastguessdist = System.Math.Abs((long)bestGuess[i] - oldcpos[i]);
				bool lookup = true;
				bool lookdn = true;

				while (multiplier < 10)
				{
					if (lookup)
					{
						uint guessup = bestGuess[i] + increment;
						long updist = guessup - oldcpos[i];

						if (updist < lastguessdist)
						{
							bestGuess[i] = guessup;
							lastguessdist = updist;
							lookdn = false;
							continue;
						}
					}  

					if (lookdn)
					{
						uint guessdn = (uint)((long)bestGuess[i] - increment);
						long dndist = (long)oldcpos[i] - guessdn;

						if (dndist < lastguessdist)
						{
							bestGuess[i] = guessdn;
							lastguessdist = dndist;
							lookup = false;
							continue;
						}
					}

					// No improvements found, we are done looking.
					break;

					//multiplier++;

					//Debug.Log("increment " + ((uint)(increment * multiplier)).PrintBitMask(lowerBitcount[i]));
					//Debug.Log("oldpositn " + oldcpos[i].PrintBitMask(lowerBitcount[i]));
					//Debug.Log("lastguess " + lastguess.PrintBitMask(lowerBitcount[i]));
					//Debug.Log(" -  -  -  -");

				}

				//bestGuess[i] = lastguess;
			}
			return bestGuess;
		}

		public static CompressedElement ZeroLowerBits(this CompressedElement fullpos)
		{
			return new CompressedElement(
				AxisRanges(0).ZeroLowerBits(fullpos.x),
				AxisRanges(1).ZeroLowerBits(fullpos.y),
				AxisRanges(2).ZeroLowerBits(fullpos.z)
				);
		}

		public static CompressedElement ZeroUpperBits(this CompressedElement fullpos)
		{
			return new CompressedElement(
				AxisRanges(0).ZeroUpperBits(fullpos.x),
				AxisRanges(1).ZeroUpperBits(fullpos.y),
				AxisRanges(2).ZeroUpperBits(fullpos.z)
				);
		}

		public static CompressedElement OverwriteLowerBits(CompressedElement upperbits, CompressedElement lowerbits)
		{
			//Debug.Log("upperx " + ((upperbits.x >> lowerBitcount[0]) << lowerBitcount[0]) + " lowerx " + lowerbits.x + " result " + (((upperbits.x >> lowerBitcount[0]) << lowerBitcount[0]) | lowerbits.x));
			return new CompressedElement
			(
				AxisRanges(0).ZeroLowerBits(upperbits[0]) | lowerbits[0],
				AxisRanges(1).ZeroLowerBits(upperbits[1]) | lowerbits[1],
				AxisRanges(2).ZeroLowerBits(upperbits[2]) | lowerbits[2]
			);
		}
	}
}



