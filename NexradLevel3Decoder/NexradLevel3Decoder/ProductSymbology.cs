using static Azrellie.Meteorology.NexradNet.Level3.Util;
using static Azrellie.Meteorology.NexradNet.Level3.PacketTypes;

namespace Azrellie.Meteorology.NexradNet.Level3;

public record ProductSymbology
{
	public short BlockID { get; init; }
	public int LengthOfBlock { get; init; }
	public short NumberOfLayers { get; init; }
	public List<SymbologyPacket> SymbologyPackets { get; } = [];

	public ProductSymbology(BinaryReader reader, ProductDescription productDescription, Level3 self)
	{
		self.INTERNAL_debugLog($"Begin decoding product symbology...", ConsoleColor.Yellow);
		if (readShort(reader) != -1)
			throw new("Invalid block divider"); // throw the scary error of terror if the block divider is invalid

		long benchmarkStart = DateTime.UtcNow.Ticks;
		long totalMemory = GC.GetTotalMemory(true);

		BlockID = readShort(reader);
		LengthOfBlock = readInt(reader);
		NumberOfLayers = readShort(reader);

		for (int i = 0; i < NumberOfLayers; i++)
		{
			long startPos = reader.BaseStream.Position;

			short layerDivider = readShort(reader);
			int lengthOfDataLayer = readInt(reader);

			if (layerDivider != -1)
				throw new("Invalid layer divider");

			try
			{
				List<SymbologyPacket> packets = [];
				while (startPos + lengthOfDataLayer > reader.BaseStream.Position)
				{
					if (reader.BaseStream.Position >= reader.BaseStream.Length) break;
					ushort packetCode = readUShort(reader);
					if (packetCode == 24 || packetCode == 25)
					{
						// skip packets 24 and 25 as they are identical to packet 23
						// also skip 2 bytes to clear the packet length data
						reader.BaseStream.Seek(2, SeekOrigin.Current);
						continue;
					}
					reader.BaseStream.Position -= 2;
					self.INTERNAL_debugLog($"[SYMBOLOGY] Decoding product symbology packet {packetCode}...", ConsoleColor.Yellow);
					SymbologyPacket packet = Packets[packetCode].Invoke(reader, productDescription);
					SymbologyPackets.Add(packet);
				}
			}
			catch (Exception ex)
			{
				self.INTERNAL_debugLog($"There was an error in parsing a packet.\nDetails:", ConsoleColor.Red);
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
				Console.ForegroundColor = ConsoleColor.Gray;
				reader.BaseStream.Seek(startPos + lengthOfDataLayer, SeekOrigin.Current);
			}
		}

		long benchmarkEnd = DateTime.UtcNow.Ticks;
		long memNow = GC.GetTotalMemory(true);
		var memoryUsage = memNow - totalMemory;
		var processingTime = (benchmarkEnd - benchmarkStart) / TimeSpan.TicksPerMillisecond;
		self.INTERNAL_debugLog($"Product symbology took {processingTime} ms to process using {(memNow - totalMemory) / 1000.0:F2} kb or {(memNow - totalMemory) / (1000.0 * 1000.0):F2} mb of memory.", ConsoleColor.Yellow);
	}
}