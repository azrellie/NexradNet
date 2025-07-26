using static Azrellie.Meteorology.NexradNet.Level3.Enums;
using static Azrellie.Meteorology.NexradNet.Level3.Util;

namespace Azrellie.Meteorology.NexradNet.Level3;

public record ProductDescription
{
	public double RadarLatitude { get; init; }
	public double RadarLongitude { get; init; }
	public short RadarElevation { get; init; }
	public MessageCode ProductCode { get; init; }
	public OperationalMode OperationalMode { get; init; }
	public VolumeCoveragePattern VolumeCoveragePattern { get; init; }
	public short SequenceNumber { get; init; }
	public short VolumeScanNumber { get; init; }
	public DateTime VolumeScanDate { get; init; }
	public DateTime GenerationDateOfProduct { get; init; }
	public Halfwords27_28 ProductData1 { get; init; }
	public short ElevationNumber { get; init; }
	public Halfwords30_53 ProductData2 { get; init; }
	public byte Version { get; init; }
	public byte SpotBlank { get; init; }
	public int SymbologyOffset { get; init; }
	public int GraphicOffset { get; init; }
	public int TabularOffset { get; init; }

	public ProductDescription(BinaryReader reader, Level3 self)
	{
		if (readShort(reader) != -1)
			throw new("Invalid block divider"); // throw the scary error of terror if the block divider is invalid

		RadarLatitude = readInt(reader) / 1000.0;
		RadarLongitude = readInt(reader) / 1000.0;
		RadarElevation = readShort(reader);
		ProductCode = (MessageCode)readShort(reader);
		OperationalMode = (OperationalMode)readShort(reader);
		VolumeCoveragePattern = (VolumeCoveragePattern)readShort(reader);
		SequenceNumber = readShort(reader);
		VolumeScanNumber = readShort(reader);
		VolumeScanDate = DateTime.UnixEpoch.AddDays(readShort(reader) - 1).AddSeconds(readInt(reader));
		GenerationDateOfProduct = DateTime.UnixEpoch.AddDays(readShort(reader) - 1).AddSeconds(readInt(reader));
		ProductData1 = new(reader, self);
		ElevationNumber = readShort(reader);
		ProductData2 = new(reader, self);
		Version = readByte(reader);
		SpotBlank = readByte(reader);
		if (self.Header.MessageCode == MessageCode.LegacyBaseReflectivity1 ||
			self.Header.MessageCode == MessageCode.LegacyBaseReflectivity2 ||
			self.Header.MessageCode == MessageCode.LegacyBaseReflectivity3 ||
			self.Header.MessageCode == MessageCode.LegacyBaseReflectivity4 ||
			self.Header.MessageCode == MessageCode.LegacyBaseReflectivityLongRange ||
			self.Header.MessageCode == MessageCode.LegacyBaseReflectivity6)
			reader.BaseStream.Seek(2, SeekOrigin.Current);
		SymbologyOffset = readInt(reader);
		GraphicOffset = readInt(reader);
		TabularOffset = readInt(reader);
	}
}