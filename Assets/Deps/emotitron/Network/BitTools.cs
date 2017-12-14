
namespace emotitron.BitToolsUtils
{
	public static class BitTools
	{

		// byte
		public static void SetBitInMask(this int bit, ref byte mask, bool onoff)
		{
			mask = ((byte)((onoff) ?
				(mask | (byte)(1 << bit)) :
				(mask & (byte)(~(1 << bit)))));
		}

		//public static void SetBitInMask(ref byte mask, int bit, bool onoff)
		//{
		//	mask = ((byte)((onoff) ?
		//		(mask | (byte)(1 << bit)) :
		//		(mask & (byte)(~(1 << bit)))));
		//}

		//ushort
		public static void SetBitInMask(this int bit, ref ushort mask, bool onoff)
		{
			mask = (ushort)((onoff) ?
				(mask | (ushort)(1 << bit)) :
				(mask & (ushort)(~(1 << bit))));
		}

		//public static void SetBitInMask(ref ushort mask, int bit, bool onoff)
		//{
		//	mask = (ushort)((onoff) ?
		//		(mask | (ushort)(1 << bit)) :
		//		(mask & (ushort)(~(1 << bit))));
		//}

		public static void SetBitInMask(this int bit, ref int mask, bool onoff)
		{
			mask = (int)((onoff) ?
				(mask | (int)(1 << bit)) :
				(mask & (int)(~(1 << bit))));
		}
		// uint
		public static void SetBitInMask(this int bit, ref uint mask, bool onoff)
		{
			mask = (uint)((onoff) ?
				(mask | (uint)(1 << bit)) :
				(mask & (uint)(~(1 << bit))));
		}

		//public static void SetBitInMask(ref uint mask, int bit, bool onoff)
		//{
		//	mask = (uint)((onoff) ?
		//		(mask | (uint)(1 << bit)) :
		//		(mask & (uint)(~(1 << bit))));
		//}

		//ulong
		public static void SetBitInMask(this int bit, ref ulong mask, bool onoff)
		{
			mask = (ulong)((onoff) ?
				(mask | (ulong)((ulong)1 << bit)) :
				(mask & (ulong)(~((ulong)1 << bit))));
		}

		//public static void SetBitInMask(ref ulong mask, int bit, bool onoff)
		//{
		//	mask = (ulong)((onoff) ?
		//		(mask | (ulong)((ulong)1 << bit)) :
		//		(mask & (ulong)(~((ulong)1 << bit))));
		//}



		public static bool GetBitInMask(this byte mask, int bit)
		{
			return ((mask & (byte)(1 << bit)) != 0);
		}
		public static bool GetBitInMask(this ushort mask, int bit)
		{
			return ((mask & (ushort)(1 << bit)) != 0);
		}
		public static bool GetBitInMask(this int mask, int bit)
		{
			return ((mask & (int)(1 << bit)) != 0);
		}

		public static bool GetBitInMask(this uint mask, int bit)
		{
			return ((mask & (uint)((uint)1 << bit)) != 0);
		}

		public static bool GetBitInMask(this ulong mask, int bit)
		{
			return ((mask & (ulong)((ulong)1 << bit)) != 0);
		}



		public static bool CompareBit(this int bit, byte a, byte b)
		{
			byte mask = (byte)(1 << bit);
			return ((a & mask) == (b & mask));
		}
		public static bool CompareBit(this int bit, ushort a, ushort b)
		{
			ushort mask = (ushort)(1 << bit);
			return ((a & mask) == (b & mask));
		}
		public static bool CompareBit(this int bit, uint a, uint b)
		{
			uint mask = (uint)1 << bit;
			return ((a & mask) == (b & mask));
		}



		public static int CountTrueBits(this byte mask)
		{
			int count = 0;
			for (int i = 0; i < 8; i++)
			{
				if (mask.GetBitInMask(i))
					i++;
			}
			return count;
		}

		public static int CountTrueBits(this ushort mask)
		{
			int count = 0;
			for (int i = 0; i < 16; i++)
			{
				if (mask.GetBitInMask(i))
					count++;
			}
			return count;
		}
		public static int CountTrueBits(this uint mask)
		{
			int count = 0;
			for (int i = 0; i < 32; i++)
			{
				if (mask.GetBitInMask(i))
					count++;
			}
			return count;
		}

		public static int CountTrueBits(this ulong mask)
		{
			int count = 0;
			for (int i = 0; i < 64; i++)
			{
				if (mask.GetBitInMask(i))
					count++;
			}
			return count;
		}

		public static string PrintBitMask(this ushort mask)
		{
			string str = "";
			for (int i = 15; i >= 0; i--)
			{
				str += (GetBitInMask(mask, i)) ? 1 : 0;

				if (i % 4 == 0)
					str += " ";
			}
			return str;
		}

		public static string PrintBitMask(this uint mask, int highliteNum = -1)
		{
			string str = "";
			for (int i = 31; i >= 0; i--)
			{
				if (i == highliteNum)
					str += "<b>";

				str += (GetBitInMask(mask, i)) ? 1 : 0;

				if (i == highliteNum)
					str += "</b>";

				if (i % 4 == 0)
					str += " ";
			}
			return str;
		}
		public static string PrintBitMask(this ulong mask, int highliteNum = -1)
		{
			string str = "";
			for (int i = 63; i >= 0; i--)
			{
				if (i == highliteNum)
					str += "<b>";

				str += (GetBitInMask(mask, i)) ? 1 : 0;

				if (i == highliteNum)
					str += "</b>";

				if (i % 4 == 0)
					str += " ";
			}
			return str;
		}

		public static int GetTrueBitOfLayerMask(int layermask)
		{
			for (int i = 0; i < 32; i++)
			{
				if (layermask.GetBitInMask(i))
					return i;
			}
			return 0;
		}

		// Half-Byte functions


		//public static byte CompressTwoHalfBytesIntoOne(this byte first, byte second, int bitsInFirst = 4)
		//{
		//	return (byte)(first | (second << bitsInFirst));
		//}


		//private static byte[] tempTwoByte = new byte[2];
		//public static byte[] DecompressTwoVarsFromByte(this byte dualByte, int bitsInFirst = 4)
		//{
		//	// Clear the high order bits in case they aren't zero
		//	tempTwoByte[0] = (byte)(dualByte << (8 - bitsInFirst));
		//	tempTwoByte[0] = (byte)(tempTwoByte[0] >> (8 - bitsInFirst));
		//	tempTwoByte[1] = (byte)(dualByte >> bitsInFirst);
		//	return tempTwoByte;
		//}


		//public static void DecompressTwoVarsFromByte(this byte dualByte, out byte first, out byte second, int bitsInFirst = 4)
		//{
		//	// Clear the high order bits in case they aren't zero
		//	first = (byte)(dualByte << (8 - bitsInFirst));
		//	first = (byte)(first >> (8 - bitsInFirst));
		//	second = (byte)(dualByte >> bitsInFirst);
		//}

		//public static byte OverwriteSomeBits(this byte host, byte parasite, int bits)
		//{
		//	host = (byte)((host >> bits) << bits);
		//	return (byte)(host | parasite);
		//}

		//public static ushort OverwriteSomeBits(this ushort host, ushort parasite, int bits)
		//{
		//	host = (ushort)((host >> bits) << bits);
		//	return (ushort)(host | parasite);
		//}

		//public static int OverwriteSomeBits(this int host, int parasite, int bits)
		//{
		//	host = ((host >> bits) << bits);
		//	return (host | parasite);
		//}
		//public static uint OverwriteSomeBits(this uint host, uint parasite, int bits)
		//{
		//	host = ((host >> bits) << bits);
		//	return (host | parasite);
		//}

		//public static ulong OverwriteSomeBits(this ulong host, ulong parasite, int bits)
		//{
		//	host = ((host >> bits) << bits);
		//	return (host | parasite);
		//}

		//private static byte[] tempBytes = new byte[8];
		/// <summary>
		/// A recycled array with a length of 8 will be provided for you to assist in GC reduction.
		/// </summary>
		//public static byte[] BufferToByteArray(this ulong buffer, int numOfBytes)
		//{
		//	for (int offset = 0; offset < numOfBytes; offset++)
		//	{
		//		tempBytes[offset] = (byte)(buffer >> (offset * 8));
		//	}
		//	return tempBytes;
		//}
		/// <summary>
		/// Supply a reusable byte[], or don't. One will be provided for you if one isn't provided.
		/// </summary>
		//public static byte[] BufferToByteArray(this ulong buffer, byte[] bytearray, int numOfBytes)
		//{
		//	for (int offset = 0; offset < numOfBytes; offset++)
		//	{
		//		bytearray[offset] = (byte)(buffer >> (offset * 8));
		//	}
		//	return bytearray;
		//}

		//public static ulong ByteArrayToBuffer(this byte[] byteArray, int numOfBytes)
		//{
		//	ulong buffer = 0;
		//	for (int offset = 0; offset < numOfBytes; offset++)
		//	{
		//		buffer = buffer | (((ulong)byteArray[offset]) << (offset * 8));
		//	}
		//	return buffer;
		//}
	}
}
