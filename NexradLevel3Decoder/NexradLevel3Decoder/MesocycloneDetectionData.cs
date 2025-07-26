namespace Azrellie.Meteorology.NexradNet.Level3;

public record MesocycloneDetectionData
{
	/// <summary>
	/// Circulation ID. Ranges from 0-999.
	/// </summary>
	public short circulationID { get; init; } = 0;
	/// <summary>
	/// Closest SCIT identified storm cell ID. Value that ranges A0 to Z0, A1 to Z1, then A2 to Z9
	/// </summary>
	public string associatedSCITStormID { get; init; } = string.Empty;
	/// <summary>
	/// Strength rank. Ranges from 1-25.
	/// </summary>
	/// <remarks>
	/// If the strength rank was computed by the low top or shallow method, an L or S will also be displayed.
	/// </remarks>
	public string strengthRank { get; init; } = string.Empty;
	/// <summary>
	/// The azimuth of the mesocyclone in degrees (0-360)
	/// </summary>
	public short azimuth { get; init; } = 0;
	/// <summary>
	/// The range of the mesocyclone from the radar in nautical miles.
	/// </summary>
	public byte range { get; init; } = 0;
	/// <summary>
	/// The low level rotational velocity of the mesocyclone (maxes out at 148 mph/129 kts).
	/// </summary>
	public byte lowLevelRotationalVelocity { get; init; } = 0;
	/// <summary>
	/// The gate to gate shear of the rotational velocities within the mesocyclone.
	/// </summary>
	public byte lowLevelGateToGateVelocityDifference { get; init; } = 0;
	/// <summary>
	/// Base height of the mesocyclone in kft.
	/// </summary>
	/// <remarks>
	/// If the base is on the lowest elevation scan or below 1 km, then the height is preceded by a "&lt;" in the display.
	/// </remarks>
	public string baseHeight { get; init; } = string.Empty;
	/// <summary>
	/// The mesocyclone depth in kft.
	/// </summary>
	/// <remarks>
	/// If the base is on the lowest elevation scan or below 1 km, then the depth is preceded by a ">" in the display.
	/// </remarks>
	public string depth { get; init; } = string.Empty;
	/// <summary>
	/// Based on the average depth of the 10 SCIT identified storm cells having the highest cell based VIL.
	/// </summary>
	public byte stormRelativeDepthPercentage { get; init; } = 0;
	/// <summary>
	/// The height in kft (thousands of feet) where the highest rotational velocity is occurring in the mesocyclone.
	/// </summary>
	public byte heightOfMaxRotationalVelocity { get; init; } = 0;
	/// <summary>
	/// The max rotational velocity of the mesocyclone in kts.
	/// </summary>
	public byte maxRotationalVelocity { get; init; } = 0;
	/// <summary>
	/// Whether or not this mesocyclone has a TVS (tornado vortex signature).
	/// </summary>
	public bool tornadoVortexSignature { get; init; } = false;
	/// <summary>
	/// The motion of the mesocyclone. A value of -32768 indicates this value was not available to the specific mesoyclone.
	/// </summary>
	public short motionDegrees { get; init; } = 0;
	/// <summary>
	/// The velocity of motion of the mesocyclone. A value of -32768 indicates this value was not available to the specific mesoyclone.
	/// </summary>
	public short motionVelocity { get; init; } = 0;
	/// <summary>
	/// The strength index of the mesocyclone.
	/// </summary>
	public int mesocycloneStrengthIndex { get; init; } = 0;
	public override string ToString() => $"ID: {circulationID} - Strength Rank: {strengthRank} - Low Level Rot Velocity: {lowLevelRotationalVelocity} - Low Level Velocity Diff: {lowLevelGateToGateVelocityDifference} - Base Height: {baseHeight} - Max Rot Velocity: {maxRotationalVelocity} - TVS: {tornadoVortexSignature} - Motion (deg/kts): {motionDegrees}/{motionVelocity}";
}

/// <summary>
/// Data collection for mesocyclone detection.
/// </summary>
public class MesocycloneDetectionDataCollection
{
	/// <summary>
	/// The radar ID.
	/// </summary>
	public short radarID { get; init; }
	/// <summary>
	/// The date and time of the data in UTC.
	/// </summary>
	public DateTime date { get; init; }
	/// <summary>
	/// Average direction of mesocyclones in degrees.
	/// </summary>
	public short averageDirectionDegrees { get; init; }
	/// <summary>
	/// Average speed of mesocyclones in knots.
	/// </summary>
	public short averageSpeedKts { get; init; }
	public List<MesocycloneDetectionData> data { get; init; } = [];
}