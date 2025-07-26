using System.Numerics;
using static Azrellie.Meteorology.NexradNet.Level3.Enums;

namespace Azrellie.Meteorology.NexradNet.Level3;

public record SymbologyPacket
{
    public ushort PacketCode { get; init; }
}

public record SymbologyPacket1 : SymbologyPacket
{
	public short LengthOfBlock { get; init; }
	public short IStartingPoint { get; init; }
	public short JStartingPoint { get; init; }
	public string Text { get; init; }
}

public record SymbologyPacket2 : SymbologyPacket
{
	public short LengthOfBlock { get; init; }
	public short IStartingPoint { get; init; }
	public short JStartingPoint { get; init; }
	/// <summary>
	/// Special symbol of this packet.
	/// </summary>
	/// <remarks>
	/// The special symbol characters in use are: !, ", #, $, % to report past storm cell position, current storm cell position, forecast storm cell position, past MDA position, and forecast MDA position.
	/// <para>Symbol definitions are:</para>
	/// <para>! = report past storm cell position</para>
	/// <para>" = current storm cell position</para>
	/// <para># = forecast storm cell position</para>
	/// <para>$ = past MDA position</para>
	/// <para>% = forecast MDA position</para>
	/// </remarks>
	public string SpecialSymbol { get; init; }
}

public record SymbologyPacket4 : SymbologyPacket
{
	public short LengthOfBlock { get; init; }
	public short Value { get; init; }
	public short X { get; init; }
	public short Y { get; init; }
	public short WindDirection { get; init; } // degrees
	public short WindSpeed { get; init; } // knots
}

public record SymbologyPacket6 : SymbologyPacket
{
	public short LengthOfBlock { get; init; }
	public short IStartingPoint { get; init; }
	public short JStartingPoint { get; init; }
	public List<Vector2> Vectors { get; init; } = [];
}

public record SymbologyPacket7 : SymbologyPacket
{
	public short LengthOfBlock { get; init; }
	public short BeginIVector1 { get; init; }
	public short BeginJVector1 { get; init; }
	public short EndIVector1 { get; init; }
	public short EndJVector1 { get; init; }
	public short BeginIVector2 { get; init; }
	public short BeginJVector2 { get; init; }
	public short EndIVector2 { get; init; }
	public short EndJVector2 { get; init; }
}

public record SymbologyPacket8 : SymbologyPacket
{
	public short LengthOfBlock { get; init; }
	public short ColorLevel { get; init; }
	public short IStartingPoint { get; init; }
	public short JStartingPoint { get; init; }
	public string Text { get; init; }
}

public record SymbologyPacket10 : SymbologyPacket
{
	public short LengthOfBlock { get; init; }
	public short ColorLevel { get; init; }
	public (Vector2 start, Vector2 end)[] Vectors { get; init; }
}

public record SymbologyPacket12 : SymbologyPacket
{
	public short LengthOfBlock { get; init; }
	public TornadoVortexSignature[] TornadoVortexSignatures { get; init; }
}

public record SymbologyPacket15 : SymbologyPacket
{
	public short LengthOfBlock { get; init; }
	/// <summary>
	/// The starting point of each storm cell in storm tracking information product.
	/// </summary>
	public (short IStartingPoint, short JStartingPoint, string SpecialSymbol)[] Data { get; init; }
}

public record SymbologyPacket16 : SymbologyPacket
{
	public short IndexOfFirstRangeBin { get; init; }
	public short NumberOfRangeBins { get; init; }
	public short ICenterOfSweep { get; init; }
	public short JCenterOfSweep { get; init; }
	public float RangeScaleFactor { get; init; }
	public short NumberOfRadials { get; init; }
	public List<Radial> Radials { get; init; }
}

public record SymbologyPacket19 : SymbologyPacket
{
	public short LengthOfBlock { get; init; }
	public List<Mesocyclone> Mesocyclones { get; init; }
}

public record SymbologyPacket20 : SymbologyPacket
{
	public short LengthOfBlock { get; init; }
	public List<Mesocyclone> Mesocyclones { get; init; }
}

public record SymbologyPacket23 : SymbologyPacket
{
	public short LengthOfBlock { get; init; }
	public List<SymbologyPacket> Packets { get; init; }
}

public record SymbologyPacket28 : SymbologyPacket
{
	public int LengthOfSerializedData { get; init; }
	public string ProductName { get; init; }
	public string ProductDescription { get; init; }
	public MessageCode ProductCode { get; init; }
	public int Type { get; init; }
	public DateTime GenerationTime { get; init; }
	public string RadarName { get; init; }
	public float RadarLatitude { get; init; }
	public float RadarLongitude { get; init; }
	public float RadarHeight { get; init; }
	public DateTime VolumeScanStartTime { get; init; }
	public DateTime ElevationScanStartTime { get; init; }
	public float ElevationAngle { get; init; }
	public int VolumeScanNumber { get; init; }
	public OperationalMode2 OperationalMode { get; init; }
	public VolumeCoveragePattern VolumeCoveragePattern { get; init; }
	public short ElevationNumber { get; init; }
	public int NumberOfParameters { get; init; }
	public int NumberOfComponents { get; init; }
	public List<Component> Components { get; init; } = [];
}

public record SymbologyPacket44831 : SymbologyPacket
{
	public short IndexOfFirstRangeBin { get; init; }
	public short NumberOfRangeBins { get; init; }
	public short ICenterOfSweep { get; init; }
	public short JCenterOfSweep { get; init; }
	public float RangeScaleFactor { get; init; }
	public short NumberOfRadials { get; init; }
	public short NumberOfRleHalfwordsInRadial { get; init; }
	public Radial[] Radials { get; init; }
}

public record SymbologyPacket47623 : SymbologyPacket
{
	public short ICoordinateStart { get; init; }
	public short JCoordinateStart { get; init; }
	public short XScaleInt { get; init; }
	public short YScaleInt { get; init; }
	public short NumberOfRows { get; init; }
	public byte[][] RasterData { get; init; }
}