using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace emotitron.Network.Compression
{
	public enum BitCullingLevel { None, DropTopThird, DropTopHalf }
	/// <summary>
	/// Define a range and resolution for an axis, and expose encoding/decoding options based on those values.
	/// </summary>
	[System.Serializable]
	public class AxisRange
	{
		public int axis;

		public float min = -10;
		public float max = 10;
		public float resolution = 100f;
		private float precision;

		public bool useAxis = true;

		private float encoder;
		private float decoder;

		[HideInInspector] public int bits;
		[HideInInspector] public int lowerBits;
		[HideInInspector] public int lowerTwoThirdsBits;
		[HideInInspector] public int lowerHalfBits;

		public int maxvalue;

		// Default Constructor
		public AxisRange(int _axis)
		{
			axis = _axis;
			min = -10;
			max = 10;
			resolution = 100f;
			CalculateEncoder();
		}

		// Constructor
		public AxisRange(int _axis, float _min, float _max, float _resolution)
		{
			axis = _axis;
			min = _min;
			max = _max;
			resolution = _resolution;
			CalculateEncoder();
		}

		public void ChangeRange(float _min, float _max, float _resolution)
		{
			min = _min;
			max = _max;
			resolution = _resolution;
			CalculateEncoder();
		}

		private int BitsAtCullingLevel(BitCullingLevel level)
		{
			return
				(level == BitCullingLevel.DropTopThird) ? lowerTwoThirdsBits :
				(level == BitCullingLevel.DropTopHalf) ? lowerBits :
				bits;
		}


		public void CalculateEncoder()
		{
			precision = 1 / resolution;
			bits = GetBitsForRangeAndRez(min, max, precision);
			lowerBits = bits / 3 * 2;

			lowerTwoThirdsBits = bits / 3 * 2;
			lowerHalfBits = bits / 3 * 2;

			encoder = GetPremultiplier(bits, min, max, precision);
			decoder = GetUnPremultiplier(bits, min, max, precision);
			maxvalue = (int)maxValue[bits];
		}

		public uint Encode(float val)
		{
			return (uint)Mathf.Clamp(((val - min) * encoder), 0, maxvalue);
		}

		public float Decode(uint val)
		{
			return val * decoder + min;
		}

		/// <summary>
		/// Determine the fewest bits required to communicate the largest value expected.
		/// </summary>
		public static int GetBitsForRangeAndRez(float min, float max, float rez)
		{
			if (rez == 0f)
			{
				Debug.Log("Resolution of 0 on object will result in a division by zero.");
				return 0;
			}

			return GetBitsForMaxValue((uint)Mathf.Ceil((max - min) / rez));
		}

		public static float GetPremultiplier(int bits, float min, float max, float rez)
		{
			return maxValue[bits] / (max - min);
		}

		public static float GetUnPremultiplier(int bits, float min, float max, float rez)
		{
			return (max - min) / maxValue[bits];
		}

		public static int GetBitsForMaxValue(uint maxvalue)
		{
			for (int i = 0; i < maxValue.Length; i++)
			{
				if (maxvalue < maxValue[i])
					return i;
			}
			return maxValue.Length - 1;
		}

		public uint ZeroLowerBits(uint val, BitCullingLevel level = BitCullingLevel.DropTopThird)
		{
			int shift = BitsAtCullingLevel(level);
			return (val >> shift) << shift;
		}

		public uint ZeroUpperBits(uint val, BitCullingLevel level = BitCullingLevel.DropTopThird)
		{
			return val & maxValue[BitsAtCullingLevel(level)];
		}

		public static uint[] maxValue = new uint[32]
		{
			0, 1, 3, 7, 15, 31, 63, 127, 255,
			511, 1023, 2047, 4095, 8191, 16383, 32767, 65535,
			131071, 262143, 524287, 1048575, 2097151, 4194303, 8388607, 16777215,
			33554431, 67108863, 134217727, 268435455, 536870911, 1073741823, 2147483647
			//, 4294967295, 8589934591, 17179869183, 34359738367, 68719476735, 137438953471, 274877906943, 549755813887, 1099511627775,
			//2199023255551, 4398046511103, 8796093022207, 17592186044415, 35184372088831, 70368744177663, 140737488355327, 281474976710655
		};

		public override string ToString()
		{
			return " min: " + min + " max: " + max + " res: " + resolution;
		}
	}
}