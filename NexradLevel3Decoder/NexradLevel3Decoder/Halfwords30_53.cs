using static Azrellie.Meteorology.NexradNet.Level3.Util;

namespace Azrellie.Meteorology.NexradNet.Level3;

// for data about each product between halfwords 30-53, refer to table V on the wsr88d format documentation https://www.roc.noaa.gov/public-documents/icds/2620001AC.pdf
// halfword 51 is present on all products, indicating whether the data is compressed or not using bzip2. if it is compressed, halfwords 52 and 53 will indicate the size of the uncompressed data, if not, they will be empty halfwords and can be skipped
// to get the size of the uncompressed data, take halfword 52, bit shift it 16 times to the left, and add halfword 53 to the bit shifted halfword 52

/// <summary>
/// This refers to the type of data that may be present at halfwords 30 and 53 based on radar product. If no data is present, productDescriptionData will have no keys/values.
/// </summary>
public record Halfwords30_53
{
	public Dictionary<string, object> productDescriptionData { get; init; } = [];
	public Dictionary<string, object> plot { get; init; } = [];

	private float decodeHalfword(short halfword)
	{
		ushort sign = (ushort)((halfword >> 15) & 0x1);
		ushort exponent = (ushort)((halfword >> 10) & 0x1F);
		ushort fraction = (ushort)(halfword & 0x3FF);

		float coefficient;
		if (exponent == 0)
			coefficient = MathF.Pow(-1, sign) * 2 * (fraction / MathF.Pow(2, 10));
		else
			coefficient = MathF.Pow(-1, sign) * MathF.Pow(2, exponent - 16) * (1 + (fraction / MathF.Pow(2, 10)));

		return coefficient;
	}

	// add the rest of the plots to the other products
	public Halfwords30_53(BinaryReader reader, Level3 self)
	{
		if (self.ProductDescription == null)
		{
			self.INTERNAL_debugLog($"[PRODUCT DESCRIPTION] Checking halfwords 30 to 53 for product specific data...", ConsoleColor.Yellow);
			self.INTERNAL_debugLog($"[PRODUCT DESCRIPTION] {self.Header.MessageCode} product detected. Extracting data...", ConsoleColor.Yellow);
		}
		if (self.Header.MessageCode == Enums.MessageCode.StormTotalAccumulation)
		{
			// if null product flag is 0, then accumulation is present in the product. non 0 indicates no accumulation in the product
			productDescriptionData.Add("nullProductFlag", readShort(reader));
			plot.Add("scale", readFloat(reader) * 100);
			plot.Add("offset", readFloat(reader));
			plot.Add("dependent35", readShort(reader));
			plot.Add("maxDataValue", readShort(reader));
			plot.Add("minDataValue", 0);
			plot.Add("leadingFlags", readShort(reader));
			plot.Add("trailingFlags", readShort(reader));
			reader.BaseStream.Seek(16, SeekOrigin.Current);
			productDescriptionData.Add("maxAccumulation", readShort(reader) * 0.1); // inches
			DateTime accumEndDate = DateTime.UnixEpoch.AddDays(readShort(reader)).AddSeconds(readShort(reader));
			productDescriptionData.Add("accumulationEndDate", accumEndDate);
			productDescriptionData.Add("meanFieldBias", readShort(reader) * 0.01);
			productDescriptionData.Add("compressionMethod", readShort(reader));
			productDescriptionData.Add("uncompressedSize", (readUShort(reader) << 16) + readUShort(reader)); // bytes
		}
		else if (self.Header.MessageCode == Enums.MessageCode.VerticallyIntegratedLiquid)
		{
			productDescriptionData.Add("avsetTerminationElevationAngle", readShort(reader) * 0.1); // degrees, -1 to +45
			reader.BaseStream.Seek(32, SeekOrigin.Current); // skip 32 bytes, as they dont mean anything in this product
			productDescriptionData.Add("maxVIL", readShort(reader)); // kg/m^2, 0 to 200
			reader.BaseStream.Seek(4, SeekOrigin.Current);
			productDescriptionData.Add("compressionMethod", readShort(reader));
			reader.BaseStream.Seek(6, SeekOrigin.Current);
		}
		else if (self.Header.MessageCode == Enums.MessageCode.DigitalVerticallyIntegratedLiquid)
		{
			productDescriptionData.Add("avsetTerminationAngle", readShort(reader) * 0.1); // degrees

			self.INTERNAL_debugLog($"[PRODUCT DESCRIPTION] HVIL: Building VIL coefficient values...", ConsoleColor.Yellow);
			float linearScale = decodeHalfword(readShort(reader)); // halfword 31
			float linearOffset = decodeHalfword(readShort(reader)); // halfword 32
			short digitalLogStart = readShort(reader); // halfword 33
			float logScale = decodeHalfword(readShort(reader)); // halfword 34
			float logOffset = decodeHalfword(readShort(reader)); // halfword 35
			reader.BaseStream.Seek(22, SeekOrigin.Current);

			short digitalVIL = readShort(reader);
			double physicalVIL;
			if (digitalVIL < digitalLogStart)
				physicalVIL = (digitalVIL - linearOffset) / linearScale;
			else
				physicalVIL = Math.Exp((digitalVIL - logOffset) / logScale);

			productDescriptionData.Add("linearScale", linearScale);
			productDescriptionData.Add("linearOffset", linearOffset);
			productDescriptionData.Add("digitalLogStart", digitalLogStart);
			productDescriptionData.Add("logScale", logScale);
			productDescriptionData.Add("logOffset", logOffset);
			productDescriptionData.Add("maxVIL", physicalVIL);
			productDescriptionData.Add("maxDigitalVIL", digitalVIL);
			productDescriptionData.Add("artifactEditedRadialsInVolumeCount", readShort(reader));
			reader.BaseStream.Seek(4, SeekOrigin.Current);
			productDescriptionData.Add("compressionMethod", readShort(reader));
			productDescriptionData.Add("uncompressedSize", (readUShort(reader) << 16) + readUShort(reader));
		}
		else if (self.Header.MessageCode == Enums.MessageCode.SuperResolutionDigitalBaseReflectivity ||
					self.Header.MessageCode == Enums.MessageCode.DigitalBaseReflectivity ||
					self.Header.MessageCode == Enums.MessageCode.BaseReflectivityLongRange ||
					self.Header.MessageCode == Enums.MessageCode.BaseReflectivityShortRange)
		{
			productDescriptionData.Add("elevationAngle", readShort(reader) * 0.1); // degrees, -1 to +45
			plot.Add("minDataValue", readShort(reader) / 10); // plot data per noaa documentation on page 36 build 23
			plot.Add("dataIncrement", readShort(reader) / 10f); // this must be divided by 10 since its encoded as a short and not a float
			plot.Add("dataLevels", readShort(reader));
			reader.BaseStream.Seek(26, SeekOrigin.Current);
			productDescriptionData.Add("maxReflectivity", readShort(reader)); // if this is -33, then data is unavailable
			reader.BaseStream.Seek(6, SeekOrigin.Current);
			productDescriptionData.Add("compressionMethod", readShort(reader));
			productDescriptionData.Add("uncompressedSize", (readUShort(reader) << 16) + readUShort(reader));
		}
		else if (self.Header.MessageCode == Enums.MessageCode.LegacyBaseReflectivity1 ||
					self.Header.MessageCode == Enums.MessageCode.LegacyBaseReflectivity2 ||
					self.Header.MessageCode == Enums.MessageCode.LegacyBaseReflectivity3 ||
					self.Header.MessageCode == Enums.MessageCode.LegacyBaseReflectivity4 ||
					self.Header.MessageCode == Enums.MessageCode.LegacyBaseReflectivityLongRange ||
					self.Header.MessageCode == Enums.MessageCode.LegacyBaseReflectivity6)
		{
			productDescriptionData.Add("elevationAngle", readShort(reader) * 0.1); // degrees, -1 to +45
			plot.Add("minDataValue", readShort(reader) / 10); // plot data per noaa documentation on page 36 build 23
			plot.Add("dataIncrement", (float)readShort(reader));
			plot.Add("dataLevels", readShort(reader));
			reader.BaseStream.Seek(26, SeekOrigin.Current);
			productDescriptionData.Add("maxReflectivity", readShort(reader)); // if this is -33, then data is unavailable
			reader.BaseStream.Seek(10, SeekOrigin.Current);
		}
		else if (self.Header.MessageCode == Enums.MessageCode.LegacyBaseVelocity1 ||
					self.Header.MessageCode == Enums.MessageCode.LegacyBaseVelocity2 ||
					self.Header.MessageCode == Enums.MessageCode.LegacyBaseVelocity3 ||
					self.Header.MessageCode == Enums.MessageCode.LegacyBaseVelocity4 ||
					self.Header.MessageCode == Enums.MessageCode.LegacyBaseVelocity5 ||
					self.Header.MessageCode == Enums.MessageCode.LegacyBaseVelocity6 ||
					self.Header.MessageCode == Enums.MessageCode.SuperResolutionDigitalBaseVelocity)
		{
			productDescriptionData.Add("elevationAngle", readShort(reader) * 0.1);
			plot.Add("minDataValue", readShort(reader) / 10); // plot data per noaa documentation on page 36 build 23
			plot.Add("dataIncrement", (float)readShort(reader));
			plot.Add("dataLevels", readShort(reader) + 1); // not in documentation, but this is 254 instead of 255 for whatever reason
			reader.BaseStream.Seek(26, SeekOrigin.Current);
			productDescriptionData.Add("maxNegativeVelocity", readShort(reader));
			productDescriptionData.Add("maxPositiveVelocity", readShort(reader));
			productDescriptionData.Add("motionSourceFlag", readShort(reader));
			reader.BaseStream.Seek(2, SeekOrigin.Current);
			productDescriptionData.Add("compressionMethod", readShort(reader));
			productDescriptionData.Add("uncompressedSize", (readUShort(reader) << 16) + readUShort(reader));
		}
		else if (self.Header.MessageCode == Enums.MessageCode.BaseVelocityDataArray)
		{
			productDescriptionData.Add("elevationAngle", readShort(reader) * 0.1);
			plot.Add("minDataValue", readShort(reader) / 10); // plot data per noaa documentation on page 36 build 23
			plot.Add("dataIncrement", (float)readShort(reader));
			plot.Add("dataLevels", readShort(reader));
			reader.BaseStream.Seek(26, SeekOrigin.Current);
			productDescriptionData.Add("maxNegativeVelocity", readShort(reader));
			productDescriptionData.Add("maxPositiveVelocity", readShort(reader));
			reader.BaseStream.Seek(4, SeekOrigin.Current);
			productDescriptionData.Add("compressionMethod", readShort(reader));
			productDescriptionData.Add("uncompressedSize", (readUShort(reader) << 16) + readUShort(reader));
		}
		else if (self.Header.MessageCode == Enums.MessageCode.EchoTops)
		{
			productDescriptionData.Add("avsetTerminationAngle", readShort(reader) * 0.1); // degrees
			reader.BaseStream.Seek(32, SeekOrigin.Current);
			productDescriptionData.Add("maxEcho", readShort(reader)); // kfeet
			reader.BaseStream.Seek(12, SeekOrigin.Current);
		}
		else if (self.Header.MessageCode == Enums.MessageCode.EnhancedEchoTops)
		{
			productDescriptionData.Add("avsetTerminationAngle", readShort(reader) * 0.1); // degrees
			reader.BaseStream.Seek(32, SeekOrigin.Current);
			productDescriptionData.Add("maxEcho", readShort(reader)); // kfeet
			productDescriptionData.Add("artifactEditedRadialsInVolumeCount", readShort(reader));
			productDescriptionData.Add("echoTopsReflectivityFactorThreshold", readShort(reader));
			productDescriptionData.Add("numberOfSpuriousPointsRemoved", readShort(reader));
			productDescriptionData.Add("compressionMethod", readShort(reader));
			productDescriptionData.Add("uncompressedSize", (readUShort(reader) << 16) + readUShort(reader));
		}
		else if (self.Header.MessageCode == Enums.MessageCode.StormRelativeVelocity1 || self.Header.MessageCode == Enums.MessageCode.StormRelativeVelocity2)
		{
			productDescriptionData.Add("elevationAngle", readShort(reader) * 0.1);
			reader.BaseStream.Seek(32, SeekOrigin.Current);
			productDescriptionData.Add("maxNegativeVelocity", readShort(reader));
			productDescriptionData.Add("maxPositiveVelocity", readShort(reader));
			productDescriptionData.Add("motionSourceFlag", readShort(reader));
			reader.BaseStream.Seek(2, SeekOrigin.Current);
			productDescriptionData.Add("averageSpeedOfStorms", readShort(reader) * 0.1); // knots
			productDescriptionData.Add("averageDirectionOfStorms", readShort(reader) * 0.1); // degrees
			reader.BaseStream.Seek(2, SeekOrigin.Current);
		}
		else if (self.Header.MessageCode == Enums.MessageCode.HydrometeorClassification)
		{
			productDescriptionData.Add("elevationAngle", readShort(reader) * 0.1);
			reader.BaseStream.Seek(32, SeekOrigin.Current);
			productDescriptionData.Add("modeFilterSize", readShort(reader));
			reader.BaseStream.Seek(6, SeekOrigin.Current);
			productDescriptionData.Add("compressionMethod", readShort(reader));
			productDescriptionData.Add("uncompressedSize", (readUShort(reader) << 16) + readUShort(reader));
		}
		else if (self.Header.MessageCode == Enums.MessageCode.HybridHydrometeorClassification)
		{
			reader.BaseStream.Seek(36, SeekOrigin.Current);
			productDescriptionData.Add("modeFilterSize", readShort(reader));
			productDescriptionData.Add("hybridRatePercentBinsFilled", readShort(reader) * 0.01);
			productDescriptionData.Add("highestElevationUsed", readShort(reader) * 0.1);
			productDescriptionData.Add("compressionMethod", readShort(reader));
			productDescriptionData.Add("uncompressedSize", (readUShort(reader) << 16) + readUShort(reader));
		}
		else if (self.Header.MessageCode == Enums.MessageCode.TornadoVortexSignature)
		{
			reader.BaseStream.Seek(32, SeekOrigin.Current);
			productDescriptionData.Add("numberOfTVS", readShort(reader));
			productDescriptionData.Add("numberOfETVS", readShort(reader));
			reader.BaseStream.Seek(6, SeekOrigin.Current);
			productDescriptionData.Add("compressionMethod", readShort(reader));
			productDescriptionData.Add("uncompressedSize", (readUShort(reader) << 16) + readUShort(reader));
		}
		else if (self.Header.MessageCode == Enums.MessageCode.MesocycloneDetection)
		{
			productDescriptionData.Add("minimumDisplayFilterStrengthRank", readShort(reader));
			reader.BaseStream.Seek(46, SeekOrigin.Current);
		}
		else if (
			self.Header.MessageCode == Enums.MessageCode.CompositeReflectivity1 ||
			self.Header.MessageCode == Enums.MessageCode.CompositeReflectivity2 ||
			self.Header.MessageCode == Enums.MessageCode.CompositeReflectivity3 ||
			self.Header.MessageCode == Enums.MessageCode.CompositeReflectivity4 ||
			self.Header.MessageCode == Enums.MessageCode.HighLayerCompositeReflectivity ||
			self.Header.MessageCode == Enums.MessageCode.LayerCompositeReflectivity ||
			self.Header.MessageCode == Enums.MessageCode.LowLayerCompositeReflectivity ||
			self.Header.MessageCode == Enums.MessageCode.MidlayerCompositeReflectivity ||
			self.Header.MessageCode == Enums.MessageCode.UserSelectableLayerCompositeReflectivity)
		{
			productDescriptionData.Add("avsetTerminationElevationAngle", readShort(reader) * 0.1);
			reader.BaseStream.Seek(32, SeekOrigin.Current);
			productDescriptionData.Add("maxReflectivity", readShort(reader));
			reader.BaseStream.Seek(3, SeekOrigin.Current);
			productDescriptionData.Add("calibrationConstant", readInt(reader));
			reader.BaseStream.Seek(5, SeekOrigin.Current);
		}
		else if (self.Header.MessageCode == Enums.MessageCode.DigitalCorrelationCoefficient)
		{
			productDescriptionData.Add("elevationAngle", readShort(reader) * 0.1);
			plot.Add("scale", readFloat(reader));
			plot.Add("offset", readFloat(reader));
			plot.Add("dependent35", readShort(reader));
			plot.Add("maxDataValue", readShort(reader));
			plot.Add("leadingFlags", readShort(reader));
			plot.Add("trailingFlags", readShort(reader));
			reader.BaseStream.Seek(16, SeekOrigin.Current);
			productDescriptionData.Add("minCorrelationCoefficient", readShort(reader));
			productDescriptionData.Add("maxCorrelationCoefficient", readShort(reader));
			reader.BaseStream.Seek(4, SeekOrigin.Current);
			productDescriptionData.Add("compressionMethod", readShort(reader));
			productDescriptionData.Add("uncompressedSize", (readUShort(reader) << 16) + readUShort(reader));
		}
		else if (self.Header.MessageCode == Enums.MessageCode.DigitalDifferentialReflectivity)
		{
			productDescriptionData.Add("elevationAngle", readShort(reader) * 0.1);
			plot.Add("scale", readFloat(reader));
			plot.Add("offset", readFloat(reader));
			plot.Add("dependent35", readShort(reader));
			plot.Add("maxDataValue", readShort(reader));
			plot.Add("leadingFlags", readShort(reader));
			plot.Add("trailingFlags", readShort(reader));
			reader.BaseStream.Seek(16, SeekOrigin.Current);
			productDescriptionData.Add("minDifferentialReflectivity", readShort(reader) * 0.1);
			productDescriptionData.Add("maxDifferentialReflectivity", readShort(reader) * 0.1);
			reader.BaseStream.Seek(4, SeekOrigin.Current);
			productDescriptionData.Add("compressionMethod", readShort(reader));
			productDescriptionData.Add("uncompressedSize", (readUShort(reader) << 16) + readUShort(reader));
		}
		else if (self.Header.MessageCode == Enums.MessageCode.DigitalAccumulationArray)
		{
			productDescriptionData.Add("nullProductFlag", readShort(reader));
			plot.Add("scale", readFloat(reader));
			plot.Add("offset", readFloat(reader));
			plot.Add("dependent35", readShort(reader));
			plot.Add("maxDataValue", readShort(reader));
			plot.Add("leadingFlags", readShort(reader));
			plot.Add("trailingFlags", readShort(reader));
			reader.BaseStream.Seek(16, SeekOrigin.Current);
			productDescriptionData.Add("maxAccumulation", readShort(reader) * 0.1); // inches
			DateTime accumEndDate = DateTime.UnixEpoch.AddDays(readShort(reader));
			productDescriptionData.Add("accumulationEndDate", accumEndDate);
			productDescriptionData.Add("accumulationEndTime", accumEndDate.AddSeconds(readShort(reader)));
			productDescriptionData.Add("meanFieldBias", readShort(reader) * 0.01);
			productDescriptionData.Add("compressionMethod", readShort(reader));
			productDescriptionData.Add("uncompressedSize", (readUShort(reader) << 16) + readUShort(reader)); // bytes
		}
		else if (self.Header.MessageCode == Enums.MessageCode.DigitalPrecipitationArray)
		{
			productDescriptionData.Add("nullProductFlag", readShort(reader));
			plot.Add("minDataValue", readShort(reader) / 10);
			plot.Add("dataIncrement", (float)readShort(reader));
			plot.Add("dataLevels", readShort(reader));
			reader.BaseStream.Seek(26, SeekOrigin.Current);
			productDescriptionData.Add("maxAccumulation", readShort(reader) * 0.1); // inches
			DateTime accumEndDate = DateTime.UnixEpoch.AddDays(readShort(reader));
			productDescriptionData.Add("accumulationEndDate", accumEndDate);
			productDescriptionData.Add("accumulationEndTime", accumEndDate.AddSeconds(readShort(reader)));
			productDescriptionData.Add("meanFieldBias", readShort(reader) * 0.01);
			reader.BaseStream.Seek(6, SeekOrigin.Current);
		}
		else if (self.Header.MessageCode == Enums.MessageCode.DigitalHybridScanReflectivity || self.Header.MessageCode == Enums.MessageCode.HybridScanReflectivity)
		{
			reader.BaseStream.Seek(32, SeekOrigin.Current);
			productDescriptionData.Add("maxReflectivity", readShort(reader));
			DateTime dateOfScan = DateTime.UnixEpoch.AddDays(readShort(reader));
			productDescriptionData.Add("dateOfScan", dateOfScan);
			reader.BaseStream.Seek(4, SeekOrigin.Current);
			productDescriptionData.Add("averageTimeOfHybridScan", readShort(reader));
			productDescriptionData.Add("compressionMethod", readShort(reader));
			productDescriptionData.Add("uncompressedSize", (readUShort(reader) << 16) + readUShort(reader)); // bytes
		}
		else if (self.Header.MessageCode == Enums.MessageCode.RadarStatusLog)
		{
			reader.BaseStream.Seek(42, SeekOrigin.Current);
			productDescriptionData.Add("compressionMethod", readShort(reader));
			productDescriptionData.Add("uncompressedSize", (readUShort(reader) << 16) + readUShort(reader)); // bytes
		}
		else if (self.Header.MessageCode == Enums.MessageCode.DigitalInstantaneousPrecipitationRate)
		{
			productDescriptionData.Add("precipitationDetetectedFlag", readShort(reader) * 0.001);
			plot.Add("scale", readFloat(reader));
			plot.Add("offset", readFloat(reader));
			plot.Add("dependent35", readShort(reader));
			plot.Add("maxDataValue", readShort(reader));
			plot.Add("leadingFlags", readShort(reader));
			plot.Add("trailingFlags", readShort(reader));
			reader.BaseStream.Seek(16, SeekOrigin.Current);
			productDescriptionData.Add("maxInstantPrecipRate", readUShort(reader) * 0.001);
			productDescriptionData.Add("hybridRatePercentBinsFilled", readShort(reader) * 0.01);
			productDescriptionData.Add("highestElevationUsed", readUShort(reader) * 0.1);
			productDescriptionData.Add("meanFieldBias", readUShort(reader) * 0.01);
			productDescriptionData.Add("compressionMethod", readShort(reader));
			productDescriptionData.Add("uncompressedSize", (readUShort(reader) << 16) + readUShort(reader)); // bytes
		}
		else if (self.Header.MessageCode == Enums.MessageCode.DigitalSpecificDifferentialPhase)
		{
			productDescriptionData.Add("elevationAngle", readShort(reader) * 0.1);
			plot.Add("scale", readFloat(reader));
			plot.Add("offset", readFloat(reader));
			plot.Add("dependent35", readShort(reader));
			plot.Add("maxDataValue", readShort(reader));
			plot.Add("leadingFlags", readShort(reader));
			plot.Add("trailingFlags", readShort(reader));
			reader.BaseStream.Seek(16, SeekOrigin.Current);
			productDescriptionData.Add("minimumSpecificDifferentialPhase", readShort(reader) * 0.05);
			productDescriptionData.Add("maximumSpecificDifferentialPhase", readShort(reader) * 0.05);
			reader.BaseStream.Seek(4, SeekOrigin.Current);
			productDescriptionData.Add("compressionMethod", readShort(reader));
			productDescriptionData.Add("uncompressedSize", (readUShort(reader) << 16) + readUShort(reader)); // bytes
		}
		else if (self.Header.MessageCode == Enums.MessageCode.OneHourAccumulation)
		{
			productDescriptionData.Add("nullProductFlag", readShort(reader));
			reader.BaseStream.Seek(32, SeekOrigin.Current);
			productDescriptionData.Add("maxAccumulation", readShort(reader) * 0.1); // inches
			DateTime accumEndDate = DateTime.UnixEpoch.AddDays(readShort(reader)).AddSeconds(readShort(reader));
			productDescriptionData.Add("accumulationEndDate", accumEndDate);
			productDescriptionData.Add("accumulationEndTime", accumEndDate.AddSeconds(readShort(reader)));
			productDescriptionData.Add("meanFieldBias", readShort(reader) * 0.01);
			reader.BaseStream.Seek(4, SeekOrigin.Current);
		}
		else if (self.Header.MessageCode == Enums.MessageCode.DigitalUserSelectableTotalAccumulation)
		{
			reader.BaseStream.Seek(2, SeekOrigin.Current);
			plot.Add("scale", readFloat(reader));
			plot.Add("offset", readFloat(reader));
			plot.Add("dependent35", readShort(reader));
			plot.Add("maxDataValue", readShort(reader));
			plot.Add("leadingFlags", readShort(reader));
			plot.Add("trailingFlags", readShort(reader));
			reader.BaseStream.Seek(16, SeekOrigin.Current);
			productDescriptionData.Add("maxAccumulation", readShort(reader) * 0.1);
			DateTime accumEndDate = DateTime.UnixEpoch.AddDays(readShort(reader));
			productDescriptionData.Add("endDate", accumEndDate);
			productDescriptionData.Add("startTime", readShort(reader));
			productDescriptionData.Add("meanFieldBias", readShort(reader));
			productDescriptionData.Add("compressionMethod", readShort(reader));
			productDescriptionData.Add("uncompressedSize", (readUShort(reader) << 16) + readUShort(reader)); // bytes
		}
		else if (self.Header.MessageCode == Enums.MessageCode.DigitalOneHourDifferenceAccumulation)
		{
			reader.BaseStream.Seek(2, SeekOrigin.Current);
			plot.Add("scale", readFloat(reader));
			plot.Add("offset", readFloat(reader));
			plot.Add("dependent35", readShort(reader));
			plot.Add("maxDataValue", readShort(reader));
			plot.Add("leadingFlags", readShort(reader));
			plot.Add("trailingFlags", readShort(reader));
			reader.BaseStream.Seek(16, SeekOrigin.Current);
			productDescriptionData.Add("maxAccumulationDifference", readShort(reader) * 0.1);
			DateTime accumEndDate = DateTime.UnixEpoch.AddDays(readShort(reader));
			productDescriptionData.Add("endDate", accumEndDate);
			productDescriptionData.Add("endTime", readShort(reader));
			productDescriptionData.Add("minAccumulationDifference", readShort(reader) * 0.1);
			productDescriptionData.Add("compressionMethod", readShort(reader));
			productDescriptionData.Add("uncompressedSize", (readUShort(reader) << 16) + readUShort(reader)); // bytes
		}
		else if (self.Header.MessageCode == Enums.MessageCode.DigitalStormTotalDifferenceAccumulation || self.Header.MessageCode == Enums.MessageCode.DigitalStormTotalAccumulation)
		{
			productDescriptionData.Add("nullProductFlag", readShort(reader));
			plot.Add("scale", readFloat(reader));
			plot.Add("offset", readFloat(reader));
			plot.Add("dependent35", readShort(reader));
			plot.Add("maxDataValue", readShort(reader));
			plot.Add("leadingFlags", readShort(reader));
			plot.Add("trailingFlags", readShort(reader));
			reader.BaseStream.Seek(16, SeekOrigin.Current);
			productDescriptionData.Add("maxAccumulationDifference", readShort(reader) * 0.1);
			DateTime accumEndDate = DateTime.UnixEpoch.AddDays(readShort(reader));
			productDescriptionData.Add("endDate", accumEndDate);
			productDescriptionData.Add("endTime", readShort(reader));
			productDescriptionData.Add("minAccumulationDifference", readShort(reader) * 0.1);
			productDescriptionData.Add("compressionMethod", readShort(reader));
			productDescriptionData.Add("uncompressedSize", (readUShort(reader) << 16) + readUShort(reader)); // bytes
		}
		else if (self.Header.MessageCode == Enums.MessageCode.VelocityAzimuthDisplayWindProfile)
		{
			reader.BaseStream.Seek(34, SeekOrigin.Current);
			productDescriptionData.Add("maxWindSpeed", readShort(reader)); // knots
			productDescriptionData.Add("directionOfMaxSpeed", readShort(reader)); // degrees
			productDescriptionData.Add("altitudeOfMaxSpeed", readShort(reader) * 0.01); // feet * 0.01 ranging 0 to 70
			reader.BaseStream.Seek(8, SeekOrigin.Current);
		}
		else
		{
			self.INTERNAL_debugLog("No data found in halfwords 30-53. This product may not be supported. Advancing...", ConsoleColor.Yellow);
			reader.BaseStream.Seek(48, SeekOrigin.Current); // skip 48 bytes, since the specified product has no data between halfwords 30-53
		}
	}
}