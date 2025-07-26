using static Azrellie.Meteorology.NexradNet.Level3.Enums;
using static Azrellie.Meteorology.NexradNet.Level3.Util;

namespace Azrellie.Meteorology.NexradNet.Level3;

public record Header
{
	public MessageCode MessageCode { get; init; }
	public DateTime DateOfMessage { get; init; }
	public int LengthOfMessage { get; init; }
	public short SourceID { get; init; }
	public short DestinationID { get; init; }
	public short NumberBlocks { get; init; }
	public int WholeHeaderSize { get; init; }

	// parse the header whenever a new instance of the Level3 class is created
	public Header(BinaryReader reader)
	{
		MessageCode = (MessageCode)readShort(reader);
		short days = readShort(reader);
		int time = readInt(reader);
		DateOfMessage = DateTime.UnixEpoch.AddDays(days - 1).AddSeconds(time);
		LengthOfMessage = readInt(reader);
		SourceID = readShort(reader);
		DestinationID = readShort(reader);
		NumberBlocks = readShort(reader);
		WholeHeaderSize = (int)reader.BaseStream.Position;
	}
}