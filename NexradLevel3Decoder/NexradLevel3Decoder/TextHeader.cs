using static Azrellie.Meteorology.NexradNet.Level3.Util;

namespace Azrellie.Meteorology.NexradNet.Level3;

public record TextHeader
{
	public string FileType { get; init; }
	public string SiteId { get; init; }
	public DateTime? TimeStamp { get; init; }
	public string DataType { get; init; }
	public string RadarStationId { get; init; }

	public TextHeader(BinaryReader reader, int textHeaderLength, bool zlib)
	{
		if (!zlib)
		{
			int bytesToSkip = textHeaderLength % 30;
			reader.BaseStream.Seek(bytesToSkip, SeekOrigin.Current);
		}

		FileType = readString(reader, 6);

		// invalid format. abort
		if (!FileType.Contains("SDUS") && !zlib)
			throw new("File format is not valid for parsing. Did you check the text header length?");

		if (zlib)
			reader.BaseStream.Seek(21, SeekOrigin.Current);
		else
			reader.BaseStream.Seek(1, SeekOrigin.Current);

		SiteId = readString(reader, 4);
		reader.BaseStream.Seek(1, SeekOrigin.Current);
		string timeStamp = readString(reader, 6);
		DateTime utcNow = DateTime.UtcNow;
		int day = int.Parse(timeStamp[..2]);
		int hour = int.Parse(timeStamp.Substring(2, 2));
		int min = int.Parse(timeStamp.Substring(4, 2));
		if (zlib) // exclude time stamp in text header for non compressed products
			TimeStamp = new(utcNow.Year, utcNow.Month, day, hour, min, 0);
		reader.BaseStream.Seek(3, SeekOrigin.Current);
		DataType = readString(reader, 3);
		RadarStationId = readString(reader, 3);
		reader.BaseStream.Seek(3, SeekOrigin.Current);
	}
}