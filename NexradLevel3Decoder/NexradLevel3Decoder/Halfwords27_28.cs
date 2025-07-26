using static Azrellie.Meteorology.NexradNet.Level3.Util;

namespace Azrellie.Meteorology.NexradNet.Level3;

/// <summary>
/// This refers to the type of data that may be present at halfwords 27 and 28 based on radar product. If no data is present, productDescriptionData will have no keys/values.
/// </summary>
public record Halfwords27_28
{
	public Dictionary<string, object> productDescriptionData { get; init; } = [];

	public Halfwords27_28(BinaryReader reader, Level3 self)
	{
		if (self.ProductDescription == null)
		{
			self.INTERNAL_debugLog($"[PRODUCT DESCRIPTION] Checking halfwords 27 and 28 for product specific data...", ConsoleColor.Yellow);
			self.INTERNAL_debugLog($"[PRODUCT DESCRIPTION] {self.Header.MessageCode} product detected. Extracting data...", ConsoleColor.Yellow);
		}
		if (self.Header.MessageCode == Enums.MessageCode.StormTotalAccumulation || self.Header.MessageCode == Enums.MessageCode.DigitalStormTotalDifferenceAccumulation)
		{
			DateTime accumulationStartDate = DateTime.UnixEpoch.AddDays(readShort(reader)).AddMinutes(readShort(reader));
			productDescriptionData.Add("accumulationStartDate", accumulationStartDate);
		}
		else if (self.Header.MessageCode == Enums.MessageCode.MesocycloneDetection)
		{
			short minReflectivity = readShort(reader);
			short overlapDisplayFilter = readShort(reader);
			productDescriptionData.Add("minReflectivity", minReflectivity);
			productDescriptionData.Add("overlapDisplayFilter", overlapDisplayFilter);
		}
		else // product doesnt match anything in this if block, just set the data to what it originally was
			reader.BaseStream.Seek(4, SeekOrigin.Current);
	}
}