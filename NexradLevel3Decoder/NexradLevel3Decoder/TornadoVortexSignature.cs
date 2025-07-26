namespace Azrellie.Meteorology.NexradNet.Level3;

public record TornadoVortexSignature
{
	public short IPosition { get; init; }
	public short JPosition { get; init; }
	public string StormID { get; init; }
}