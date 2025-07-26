namespace Azrellie.Meteorology.NexradNet.Level3;

public class Mesocyclone
{
	public short IPosition { get; set; }
	public short JPosition { get; set; }
	/// <summary>
	/// Possible values:
	/// <para>1 = Mesocyclone (extrapolated)</para>
	/// <para>3 = Mesocyclone (persistent, new, or increasing)</para>
	/// <para>5 = TVS (Tornado Vortex Signature) (extrapolated)</para>
	/// <para>6 = ETVS (Tornado Vortex Signature) (extrapolated)</para>
	/// <para>7 = TVS (Tornado Vortex Signature) (persistent, new, or increasing)</para>
	/// <para>8 = ETVS (Tornado Vortex Signature) (persistent, new, or increasing)</para>
	/// <para>9 = MDA circulation with strength rank >= 5 and with a base height less or equal to 1 km ARL or with its base on the lowest elevation angle</para>
	/// <para>10 = MDA circulation with strength rank >= 5 and with a base height > 1 ARL and that base is not on the lowest elevation angle</para>
	/// <para>11 = MDA circulation with strength rank less than 5</para>
	/// <para>A value of -999 indicates the cells are beyond the max range for algorithm processing.</para>
	/// </summary>
	public short PointFeatureType { get; set; }
	/// <summary>
	/// For feature types 1-4 and 9-11, radius in km.
	/// <para>A value of -999 indicates the cells are beyond the max range for algorithm processing.</para>
	/// </summary>
	public double PointFeatureAttribute { get; set; }
}