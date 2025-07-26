using System.IO.Compression;
using System.Text;

namespace Azrellie.Meteorology.NexradNet.Level3;

internal class Util
{
	/// <summary>
	/// Reads a short from a binary reader in big endian.
	/// </summary>
	/// <param name="reader"></param>
	public static short readShort(BinaryReader reader)
	{
		byte[] bytes = reader.ReadBytes(2);
		Array.Reverse(bytes);
		return BitConverter.ToInt16(bytes);
	}

	/// <summary>
	/// Reads an unsigned short from a binary reader in big endian.
	/// </summary>
	/// <param name="reader"></param>
	public static ushort readUShort(BinaryReader reader)
	{
		byte[] bytes = reader.ReadBytes(2);
		Array.Reverse(bytes);
		return BitConverter.ToUInt16(bytes);
	}

	/// <summary>
	/// Reads an int from a binary reader in big endian.
	/// </summary>
	/// <param name="reader"></param>
	public static int readInt(BinaryReader reader)
	{
		byte[] bytes = reader.ReadBytes(4);
		Array.Reverse(bytes);
		return BitConverter.ToInt32(bytes);
	}

	public static float readFloat(BinaryReader reader)
	{
		byte[] bytes = reader.ReadBytes(4);
		Array.Reverse(bytes);
		return BitConverter.ToSingle(bytes);
	}

	public static string readString(BinaryReader reader, int length) => Encoding.ASCII.GetString(reader.ReadBytes(length));

	/// <summary>
	/// Reads an unsigned int from a binary reader in big endian.
	/// </summary>
	/// <param name="reader"></param>
	public static uint readUInt(BinaryReader reader)
	{
		byte[] bytes = reader.ReadBytes(4);
		Array.Reverse(bytes);
		return BitConverter.ToUInt32(bytes);
	}

	/// <summary>
	/// Reads a specific amount of bytes from a binary reader.
	/// </summary>
	/// <param name="reader"></param>
	public static byte[] read(BinaryReader reader, int bytesToRead) => reader.ReadBytes(bytesToRead);

	/// <summary>
	/// Reads a specific amount of bytes from a binary reader.
	/// </summary>
	/// <param name="reader"></param>
	public static byte[] read(BinaryReader reader, long bytesToRead) => reader.ReadBytes((int)bytesToRead);

	/// <summary>
	/// Reads a byte from a binary reader.
	/// </summary>
	/// <param name="reader"></param>
	public static byte readByte(BinaryReader reader) => reader.ReadByte();

	/// <summary>
	/// Reads a certain number of bytes from a binary reader.
	/// </summary>
	/// <param name="reader"></param>
	public static byte[] readBytes(BinaryReader reader, int bytes) => reader.ReadBytes(bytes);

	public static bool usesZlibCompression(BinaryReader reader, int headerSkip)
	{
		reader.BaseStream.Seek(headerSkip, SeekOrigin.Begin);
		byte b = readByte(reader);
		byte b2 = readByte(reader);
		reader.BaseStream.Position = 0;
		return b == 0x78 && b2 == 0xDA;
	}

	public static bool hasMultipleZlibChunks(BinaryReader reader)
	{
		long originalPos = reader.BaseStream.Position;

		byte[] data = new byte[reader.BaseStream.Length];
		reader.BaseStream.Position = 0;
		reader.Read(data, 0, (int)reader.BaseStream.Length);

		int chunksFound = 0;
		for (int i = 0; i < data.Length - 1; i++)
			if (data[i] == 0x78 && data[i + 1] == 0xDA)
				chunksFound++;

		reader.BaseStream.Position = originalPos;
		return chunksFound > 1;
	}

	public static MemoryStream decompressZlibChunks(BinaryReader reader, Level3 self)
	{
		self.INTERNAL_debugLog("Processing zlib chunks...", ConsoleColor.Yellow);
		List<int> headerPositions = [];

		byte[] data = new byte[reader.BaseStream.Length];
		reader.BaseStream.Position = 0;
		reader.Read(data, 0, (int)reader.BaseStream.Length);

		for (int i = 0; i < data.Length - 1; i++)
			if (data[i] == 0x78 && data[i + 1] == 0xDA)
				headerPositions.Add(i);

		self.INTERNAL_debugLog($"Found {headerPositions.Count} zlib chunks.", ConsoleColor.Yellow);
		MemoryStream decompressedData = new();

		for (int i = 0; i < headerPositions.Count; i++)
		{
			int start = headerPositions[i] + 2;
			int end = (i + 1 < headerPositions.Count) ? headerPositions[i + 1] : data.Length;
			int sizeOfChunk = end - start;

			reader.BaseStream.Position = start;
			byte[] compressedChunk = new byte[sizeOfChunk];
			Array.Copy(data, start, compressedChunk, 0, compressedChunk.Length);

			self.INTERNAL_debugLog($"Processing chunk {i + 1}: Offset {start}, Size {compressedChunk.Length} bytes", ConsoleColor.Yellow);

			bool decompressSuccessful = false;
			try
			{
				using MemoryStream compressedStream = new(compressedChunk);
				using DeflateStream inflater = new(compressedStream, CompressionMode.Decompress);
				using MemoryStream chunkOutput = new();
				inflater.CopyTo(chunkOutput);

				byte[] decompressedChunk = chunkOutput.ToArray();
				decompressedData.Write(decompressedChunk, 0, decompressedChunk.Length);

				decompressSuccessful = true;
				self.INTERNAL_debugLog($"Chunk {i + 1} decompressed successfully. Size: {decompressedChunk.Length} bytes", ConsoleColor.Yellow);
			}
			catch (Exception ex)
			{
				self.INTERNAL_debugLog($"Error processing zlib chunk {i + 1}. Details:", ConsoleColor.Red);
				self.INTERNAL_debugLog(ex.Message, ConsoleColor.Red);
			}
			if (!decompressSuccessful)
				break;
		}
		
		return decompressedData;
	}
}