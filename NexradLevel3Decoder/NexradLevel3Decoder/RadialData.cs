namespace Azrellie.Meteorology.NexradNet.Level3;

public readonly record struct RadialData(
	float Azimuth,
	float Elevation,
	float Width,
	float NumberOfBins,
	string Type,
	string Unit,
	float[] Bins,
	float[] BinsRaw
);