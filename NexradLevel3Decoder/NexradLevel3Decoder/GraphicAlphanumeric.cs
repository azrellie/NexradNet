using static Azrellie.Meteorology.NexradNet.Level3.Util;
using static Azrellie.Meteorology.NexradNet.Level3.PacketTypes;

namespace Azrellie.Meteorology.NexradNet.Level3;

public record GraphicAlphanumeric
{
	public short BlockID { get; init; }
	public int LengthOfBlock { get; init; }
	public short NumberOfPages { get; init; }
	public short LengthOfPage { get; init; }
	public List<SymbologyPacket> ProductPackets { get; }

	public GraphicAlphanumeric(BinaryReader reader, ProductDescription productDescription, Level3 self)
	{
		if (readShort(reader) != -1)
			throw new("Invalid block divider"); // throw the scary error of terror if the block divider is invalid

		BlockID = readShort(reader);
		LengthOfBlock = readInt(reader);
		NumberOfPages = readShort(reader);
		ProductPackets = [];

		for (int i = 0; i < NumberOfPages; i++)
		{
			reader.BaseStream.Seek(2, SeekOrigin.Current); // skip page number because its not being used
			short pageLength = readShort(reader);
			int endingByte = (int)(reader.BaseStream.Position + pageLength);

			while (reader.BaseStream.Position < endingByte)
			{
				ushort packetCode = readUShort(reader);
				self.INTERNAL_debugLog($"[GRAPHIC] Decoding graphic alphanumeric packet {packetCode}...", ConsoleColor.Yellow);
				reader.BaseStream.Position -= 2;
				SymbologyPacket packet = Packets[packetCode].Invoke(reader, productDescription);
				ProductPackets.Add(packet);
			}
		}
	}
}