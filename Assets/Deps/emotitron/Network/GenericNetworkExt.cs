
using UnityEngine;
using UnityEngine.Networking;

public static class GenericNetworkExt
{
	private static NetworkWriter tempwriter = new NetworkWriter();

	public static void SendPayloadArrayToAllClients(this NetworkWriter writer, short msgType, int channel = Channels.DefaultUnreliable)
	{
		foreach (NetworkConnection nc in NetworkServer.connections)
		{
			if (nc == null)
				continue;

			tempwriter.StartMessage(msgType);
			for (int i = 4; i < writer.Position; i++)
			{
				tempwriter.Write(writer.AsArray()[i]);
			}
			tempwriter.FinishMessage();

			nc.SendWriter(tempwriter, channel);
		}
	}

	/// <summary>
	/// Write a byte array to the UNET writer without it adding a 16bit tally. Of course
	/// this only is use if the size is fixed - since the reader won't know how many bytes to read on its own.
	/// </summary>
	/// <param name="bytesToWrite">num of bytes in the array to actually write. -1 (default) will write all.</param>
	/// <returns></returns>
	public static void WriteUncountedByteArray(this NetworkWriter writer, byte[] bytes, int bytesToWrite = -1)
	{
		int numOfBytes = (bytesToWrite == -1) ? bytes.Length : bytesToWrite;
		for (int i = 0; i < numOfBytes; i++)
		{
			writer.Write(bytes[i]);
		}
	}

	// Alternative to reading in a Byte array with the UNET reader - which allocates a new Array to do it. This SHOULD produce less garbage.

	/// <summary>
	/// Alternative to HLAPI NetworkReader ReadBytes(), which creates a new byte[] every time. This reuses a byte[] and reads the bytes in
	/// one at a time, hopefully eliminating GC.
	/// </summary>
	/// <param name="reader"></param>
	/// <param name="count"></param>
	/// <returns>Returns the same byte array as the on in the arguments.</returns>
	public static byte[] ReadBytesNonAlloc(this NetworkReader reader, byte[] targetbytearray, int count = -1)
	{
		if (count == -1)
			count = targetbytearray.Length;

		for (int i = 0; i < count; i++)
		{
			targetbytearray[i] = reader.ReadByte();
		}
		return targetbytearray;
	}
	// if no reusablearray was provided, use this functions own.
	/// <summary>
	/// If you don't provide a target byte[] array, then the 16 capacity one built into this class will be used. Be sure to make use of the contents
	/// immediately though, as it may get reusued again.
	/// </summary>
	/// <param name="reader"></param>
	/// <param name="count"></param>
	/// <returns>Returns the reusable byte array.</returns>
	public static byte[] ReadBytesNonAlloc(this NetworkReader reader, int count)
	{
		return ReadBytesNonAlloc(reader, reusableByteArray, count);
	}

	public static byte[] reusableByteArray = new byte[64]; // long enough to hold a ulong buffer

}


