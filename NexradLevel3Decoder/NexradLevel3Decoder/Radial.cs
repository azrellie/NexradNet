namespace Azrellie.Meteorology.NexradNet.Level3;

public record Radial
{
    public float StartAngle { get; init; }
    public float AngleDelta { get; init; }
}

public record Radial16 : Radial
{
	public byte[] Bins { get; init; }
}

public record Radial255 : Radial
{
	public float[] Bins { get; init; }
	public byte[] BinsRaw { get; init; }
	public string[] BinsAlphanumeric { get; init; }
}