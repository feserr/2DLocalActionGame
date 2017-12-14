using UnityEngine;
using emotitron.BitToolsUtils;
using System.Runtime.InteropServices;

namespace emotitron.Network.NST
{
	[StructLayout(LayoutKind.Explicit)]
	public struct CompressedElement
	{
		[FieldOffset(0)]
		public uint x;
		[FieldOffset(4)]
		public uint y;
		[FieldOffset(8)]
		public uint z;

		[FieldOffset(0)]
		public ulong quat;

		public readonly static CompressedElement zero;

		public CompressedElement(uint _x, uint _y, uint _z, int _bits = 0, int _bitsSent = 0)
		{
			this = default(CompressedElement);
			x = _x;
			y = _y;
			z = _z;
		}

		public CompressedElement(ulong _quat)
		{
			this = default(CompressedElement);
			quat = _quat;
		}

		static CompressedElement()
		{
			zero = new CompressedElement() { x = 0, y = 0, z = 0 };
		}

		// Indexer
		public uint this[int index]
		{
			get
			{
				return (index == 0) ? x : (index == 1) ? y : z;
			}
			set
			{
				if (index == 0) x = value;
				else if (index == 1) y = value;
				else if (index == 2) z = value;
				//includesXYZ[index] = true;
			}
		}

		public static implicit operator ulong(CompressedElement val)
		{
			return val.quat;
		}
		public static implicit operator CompressedElement(ulong val)
		{
			return new CompressedElement(val);
		}

		/// <summary>
		/// Basic compare of the X, Y, Z, and W values. True if they all match.
		/// </summary>
		public static bool Compare(CompressedElement a, CompressedElement b)
		{
			return (a.x == b.x && a.y == b.y && a.z == b.z);
		}

		public static void Copy(CompressedElement source, CompressedElement target)
		{
			target.x = source.x;
			target.y = source.y;
			target.z = source.z;
		}

		/// <summary>
		/// Get the bit count of the highest bit that is different between two compressed positions. This is the min number of bits that must be sent.
		/// </summary>
		/// <returns></returns>
		public static int HighestDifferentBit(uint a, uint b)
		{
			int highestDiffBit = 0;

			for (int i = 0; i < 32; i++)
				if (i.CompareBit(a, b) == false)
					highestDiffBit = i;

			return highestDiffBit;
		}

		public static CompressedElement operator +(CompressedElement a, CompressedElement b)
		{
			return new CompressedElement((uint)((long)a.x + b.x), (uint)((long)a.y + b.y), (uint)((long)a.z + b.z));
		}

		public static CompressedElement operator -(CompressedElement a, CompressedElement b)
		{
			return new CompressedElement((uint)((long)a.x - b.x), (uint)((long)a.y - b.y), (uint)((long)a.z - b.z));
		}
		public static CompressedElement operator *(CompressedElement a, float b)
		{
			return new CompressedElement((uint)((long)a.x * b), (uint)((long)a.y * b), (uint)((long)a.z * b));
		}

		public static CompressedElement Extrapolate (CompressedElement curr, CompressedElement prev, float amount = 1)
		{
			return curr + 
				((amount == 1) ? (curr - prev) : 
				(amount == 0) ? zero :
				(curr - prev) * amount);
		}

		public override string ToString()
		{
			return x + " " + y + " " + z;
		}
	}
}
