namespace Azrellie.Meteorology.NexradNet.Level3;

public record RadialComponent : Component
{
	public string Description { get; init; }
	public float BinSize { get; init; }
	public float RangeToFirstBin { get; init; }
	public int NumberOfComponentParameters { get; init; }
	public int ComponentParameterList { get; init; }
	public int NumberOfRadials { get; init; }
	public RadialData[] RadialData { get; init; }
}