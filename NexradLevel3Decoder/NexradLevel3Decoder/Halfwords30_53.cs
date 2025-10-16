using static Azrellie.Meteorology.NexradNet.Level3.Util;

namespace Azrellie.Meteorology.NexradNet.Level3;

// for data about each product between halfwords 30-53, refer to table V on the wsr88d format documentation https://www.roc.noaa.gov/public-documents/icds/2620001AC.pdf
// halfword 51 is present on all products, indicating whether the data is compressed or not using bzip2. if it is compressed, halfwords 52 and 53 will indicate the size of the uncompressed data, if not, they will be empty halfwords and can be skipped
// to get the size of the uncompressed data, take halfword 52, bit shift it 16 times to the left, and add halfword 53 to the bit shifted halfword 52
// more information can be found about older/legacy products at https://www.roc.noaa.gov/public-documents/icds/2620001P.pdf

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

	public Halfwords30_53(BinaryReader reader, Level3 self)
	{
		if (self.ProductDescription == null)
		{
			self.INTERNAL_debugLog($"[PRODUCT DESCRIPTION] Checking halfwords 30 to 53 for product specific data...", ConsoleColor.Yellow);
			self.INTERNAL_debugLog($"[PRODUCT DESCRIPTION] {self.Header.MessageCode} product detected. Extracting data...", ConsoleColor.Yellow);
		}
		var code = self.Header.MessageCode;
		if (code == Enums.MessageCode.StormTotalAccumulation)
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
			plot.Add("rangeNmi", 124);
			plot.Add("rangeKm", 229.648f);
			reader.BaseStream.Seek(16, SeekOrigin.Current);
			productDescriptionData.Add("maxAccumulation", readShort(reader) * 0.1f); // inches
			DateTime accumEndDate = DateTime.UnixEpoch.AddDays(readShort(reader)).AddSeconds(readShort(reader));
			productDescriptionData.Add("accumulationEndDate", accumEndDate);
			productDescriptionData.Add("meanFieldBias", readShort(reader) * 0.01f);
			productDescriptionData.Add("compressionMethod", readShort(reader));
			productDescriptionData.Add("uncompressedSize", (readUShort(reader) << 16) + readUShort(reader)); // bytes
		}
		else if (code == Enums.MessageCode.VerticallyIntegratedLiquid)
		{
			productDescriptionData.Add("avsetTerminationElevationAngle", readShort(reader) * 0.1f); // degrees, -1 to +45
			reader.BaseStream.Seek(32, SeekOrigin.Current); // skip 32 bytes, as they dont mean anything in this product
			productDescriptionData.Add("maxVIL", readShort(reader)); // kg/m^2, 0 to 200
			reader.BaseStream.Seek(4, SeekOrigin.Current);
			productDescriptionData.Add("compressionMethod", readShort(reader));
			reader.BaseStream.Seek(6, SeekOrigin.Current);
			plot.Add("rangeNmi", 124);
			plot.Add("rangeKm", 229.648f);
		}
		else if (code == Enums.MessageCode.DigitalVerticallyIntegratedLiquid)
		{
			productDescriptionData.Add("avsetTerminationAngle", readShort(reader) * 0.1f); // degrees

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
			plot.Add("rangeNmi", 248);
			plot.Add("rangeKm", 459.296f);
		}
		else if (code == Enums.MessageCode.SuperResolutionDigitalBaseReflectivity ||
					code == Enums.MessageCode.DigitalBaseReflectivity ||
					code == Enums.MessageCode.BaseReflectivityLongRange ||
					code == Enums.MessageCode.BaseReflectivityShortRange)
		{
			productDescriptionData.Add("elevationAngle", readShort(reader) * 0.1f); // degrees, -1 to +45
			plot.Add("minDataValue", readShort(reader) / 10); // plot data per noaa documentation on page 36 build 23
			plot.Add("dataIncrement", readShort(reader) / 10f); // this must be divided by 10 since its encoded as a short and not a float
			plot.Add("dataLevels", readShort(reader));
			reader.BaseStream.Seek(26, SeekOrigin.Current);
			productDescriptionData.Add("maxReflectivity", readShort(reader)); // if this is -33, then data is unavailable
			reader.BaseStream.Seek(6, SeekOrigin.Current);
			productDescriptionData.Add("compressionMethod", readShort(reader));
			productDescriptionData.Add("uncompressedSize", (readUShort(reader) << 16) + readUShort(reader));
			if (code == Enums.MessageCode.SuperResolutionDigitalBaseReflectivity || code == Enums.MessageCode.DigitalBaseReflectivity)
			{
				plot.Add("rangeNmi", 248);
				plot.Add("rangeKm", 459.296f);
			}
			else if (code == Enums.MessageCode.BaseReflectivityLongRange)
			{
				plot.Add("rangeNmi", 225);
				plot.Add("rangeKm", 416.7f);
			}
			else if (code == Enums.MessageCode.BaseReflectivityShortRange)
			{
				plot.Add("rangeNmi", 48);
				plot.Add("rangeKm", 88.896f);
			}
		}
		else if (code == Enums.MessageCode.LegacyBaseReflectivity1 ||
					code == Enums.MessageCode.LegacyBaseReflectivity2 ||
					code == Enums.MessageCode.LegacyBaseReflectivity3 ||
					code == Enums.MessageCode.LegacyBaseReflectivity4 ||
					code == Enums.MessageCode.LegacyBaseReflectivityLongRange ||
					code == Enums.MessageCode.LegacyBaseReflectivity6)
		{
			productDescriptionData.Add("elevationAngle", readShort(reader) * 0.1f); // degrees, -1 to +45
			plot.Add("minDataValue", readShort(reader) / 10); // plot data per noaa documentation on page 36 build 23
			plot.Add("dataIncrement", (float)readShort(reader));
			plot.Add("dataLevels", readShort(reader));
			reader.BaseStream.Seek(26, SeekOrigin.Current);
			productDescriptionData.Add("maxReflectivity", readShort(reader)); // if this is -33, then data is unavailable
			reader.BaseStream.Seek(10, SeekOrigin.Current);
			if (code == Enums.MessageCode.LegacyBaseReflectivity1 || code == Enums.MessageCode.LegacyBaseReflectivity4)
			{
				plot.Add("rangeNmi", 124);
				plot.Add("rangeKm", 229.648f);
			}
			else if(code == Enums.MessageCode.LegacyBaseReflectivity2 || code == Enums.MessageCode.LegacyBaseReflectivity3 || code == Enums.MessageCode.LegacyBaseReflectivityLongRange || code == Enums.MessageCode.LegacyBaseReflectivity6)
			{
				plot.Add("rangeNmi", 248);
				plot.Add("rangeKm", 459.296f);
			}
		}
		else if (code == Enums.MessageCode.LegacyBaseVelocity1 ||
					code == Enums.MessageCode.LegacyBaseVelocity2 ||
					code == Enums.MessageCode.LegacyBaseVelocity3 ||
					code == Enums.MessageCode.LegacyBaseVelocity4 ||
					code == Enums.MessageCode.LegacyBaseVelocity5 ||
					code == Enums.MessageCode.LegacyBaseVelocity6 ||
					code == Enums.MessageCode.SuperResolutionDigitalBaseVelocity)
		{
			productDescriptionData.Add("elevationAngle", readShort(reader) * 0.1f);
			plot.Add("minDataValue", readShort(reader) / 10); // plot data per noaa documentation on page 36 build 23
			plot.Add("dataIncrement", readShort(reader) / 10f);
			plot.Add("dataLevels", readShort(reader));
			plot.Add("rangeNmi", 162);
			plot.Add("rangeKm", 300.024f);
			reader.BaseStream.Seek(26, SeekOrigin.Current);
			productDescriptionData.Add("maxNegativeVelocity", readShort(reader));
			productDescriptionData.Add("maxPositiveVelocity", readShort(reader));
			productDescriptionData.Add("motionSourceFlag", readShort(reader));
			reader.BaseStream.Seek(2, SeekOrigin.Current);
			productDescriptionData.Add("compressionMethod", readShort(reader));
			productDescriptionData.Add("uncompressedSize", (readUShort(reader) << 16) + readUShort(reader));
			if (code == Enums.MessageCode.LegacyBaseVelocity1 || code == Enums.MessageCode.LegacyBaseVelocity4)
			{
				plot.Add("rangeNmi", 32);
				plot.Add("rangeKm", 59.264f);
			}
			else if (code == Enums.MessageCode.LegacyBaseVelocity2 || code == Enums.MessageCode.LegacyBaseVelocity5)
			{
				plot.Add("rangeNmi", 62);
				plot.Add("rangeKm", 114.824f);
			}
			else if (code == Enums.MessageCode.LegacyBaseVelocity3 || code == Enums.MessageCode.LegacyBaseVelocity6)
			{
				plot.Add("rangeNmi", 124);
				plot.Add("rangeKm", 229.648);
			}
		}
		else if (code == Enums.MessageCode.BaseVelocityDataArray)
		{
			productDescriptionData.Add("elevationAngle", readShort(reader) * 0.1f);
			plot.Add("minDataValue", readShort(reader) / 10); // plot data per noaa documentation on page 36 build 23
			plot.Add("dataIncrement", readShort(reader) / 10f);
			plot.Add("dataLevels", readShort(reader));
			reader.BaseStream.Seek(26, SeekOrigin.Current);
			productDescriptionData.Add("maxNegativeVelocity", readShort(reader));
			productDescriptionData.Add("maxPositiveVelocity", readShort(reader));
			reader.BaseStream.Seek(4, SeekOrigin.Current);
			productDescriptionData.Add("compressionMethod", readShort(reader));
			productDescriptionData.Add("uncompressedSize", (readUShort(reader) << 16) + readUShort(reader));
			plot.Add("rangeNmi", 124);
			plot.Add("rangeKm", 229.648f);
		}
		else if (code == Enums.MessageCode.EchoTops)
		{
			productDescriptionData.Add("avsetTerminationAngle", readShort(reader) * 0.1f); // degrees
			reader.BaseStream.Seek(32, SeekOrigin.Current);
			productDescriptionData.Add("maxEcho", readShort(reader)); // kfeet
			reader.BaseStream.Seek(12, SeekOrigin.Current);
			plot.Add("rangeNmi", 124);
			plot.Add("rangeKm", 229.648f);
		}
		else if (code == Enums.MessageCode.EnhancedEchoTops)
		{
			productDescriptionData.Add("avsetTerminationAngle", readShort(reader) * 0.1f); // degrees
			reader.BaseStream.Seek(32, SeekOrigin.Current);
			productDescriptionData.Add("maxEcho", readShort(reader)); // kfeet
			productDescriptionData.Add("artifactEditedRadialsInVolumeCount", readShort(reader));
			productDescriptionData.Add("echoTopsReflectivityFactorThreshold", readShort(reader));
			productDescriptionData.Add("numberOfSpuriousPointsRemoved", readShort(reader));
			productDescriptionData.Add("compressionMethod", readShort(reader));
			productDescriptionData.Add("uncompressedSize", (readUShort(reader) << 16) + readUShort(reader));
			plot.Add("rangeNmi", 186);
			plot.Add("rangeKm", 344.472f);
		}
		else if (code == Enums.MessageCode.StormRelativeVelocity1 || code == Enums.MessageCode.StormRelativeVelocity2)
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
			if (code == Enums.MessageCode.StormRelativeVelocity1)
			{
				plot.Add("rangeNmi", 27);
				plot.Add("rangeKm", 50.004f);
			}
			else if (code == Enums.MessageCode.StormRelativeVelocity2)
			{
				plot.Add("rangeNmi", 124);
				plot.Add("rangeKm", 229.648f);
			}
		}
		else if (code == Enums.MessageCode.HydrometeorClassification)
		{
			productDescriptionData.Add("elevationAngle", readShort(reader) * 0.1f);
			reader.BaseStream.Seek(32, SeekOrigin.Current);
			productDescriptionData.Add("modeFilterSize", readShort(reader));
			reader.BaseStream.Seek(6, SeekOrigin.Current);
			productDescriptionData.Add("compressionMethod", readShort(reader));
			productDescriptionData.Add("uncompressedSize", (readUShort(reader) << 16) + readUShort(reader));
			plot.Add("rangeNmi", 124);
			plot.Add("rangeKm", 229.648f);
		}
		else if (code == Enums.MessageCode.HybridHydrometeorClassification)
		{
			reader.BaseStream.Seek(36, SeekOrigin.Current);
			productDescriptionData.Add("modeFilterSize", readShort(reader));
			productDescriptionData.Add("hybridRatePercentBinsFilled", readShort(reader) * 0.01);
			productDescriptionData.Add("highestElevationUsed", readShort(reader) * 0.1);
			productDescriptionData.Add("compressionMethod", readShort(reader));
			productDescriptionData.Add("uncompressedSize", (readUShort(reader) << 16) + readUShort(reader));
			plot.Add("rangeNmi", 124);
			plot.Add("rangeKm", 229.648f);
		}
		else if (code == Enums.MessageCode.TornadoVortexSignature)
		{
			reader.BaseStream.Seek(32, SeekOrigin.Current);
			productDescriptionData.Add("numberOfTVS", readShort(reader));
			productDescriptionData.Add("numberOfETVS", readShort(reader));
			reader.BaseStream.Seek(6, SeekOrigin.Current);
			productDescriptionData.Add("compressionMethod", readShort(reader));
			productDescriptionData.Add("uncompressedSize", (readUShort(reader) << 16) + readUShort(reader));
		}
		else if (code == Enums.MessageCode.MesocycloneDetection)
		{
			productDescriptionData.Add("minimumDisplayFilterStrengthRank", readShort(reader));
			reader.BaseStream.Seek(46, SeekOrigin.Current);
		}
		else if (
			code == Enums.MessageCode.CompositeReflectivity1 ||
			code == Enums.MessageCode.CompositeReflectivity2 ||
			code == Enums.MessageCode.CompositeReflectivity3 ||
			code == Enums.MessageCode.CompositeReflectivity4 ||
			code == Enums.MessageCode.HighLayerCompositeReflectivity ||
			code == Enums.MessageCode.LayerCompositeReflectivity ||
			code == Enums.MessageCode.LowLayerCompositeReflectivity ||
			code == Enums.MessageCode.MidlayerCompositeReflectivity ||
			code == Enums.MessageCode.UserSelectableLayerCompositeReflectivity)
		{
			productDescriptionData.Add("avsetTerminationElevationAngle", readShort(reader) * 0.1);
			reader.BaseStream.Seek(32, SeekOrigin.Current);
			productDescriptionData.Add("maxReflectivity", readShort(reader));
			reader.BaseStream.Seek(3, SeekOrigin.Current);
			productDescriptionData.Add("calibrationConstant", readInt(reader));
			reader.BaseStream.Seek(5, SeekOrigin.Current);
			if (code == Enums.MessageCode.CompositeReflectivity1 ||
				code == Enums.MessageCode.CompositeReflectivity3 ||
				code == Enums.MessageCode.HighLayerCompositeReflectivity ||
				code == Enums.MessageCode.LayerCompositeReflectivity ||
				code == Enums.MessageCode.LowLayerCompositeReflectivity ||
				code == Enums.MessageCode.MidlayerCompositeReflectivity ||
				code == Enums.MessageCode.UserSelectableLayerCompositeReflectivity)
			{
				plot.Add("rangeNmi", 124);
				plot.Add("rangeKm", 229.648f);
			}
			else if (code == Enums.MessageCode.CompositeReflectivity2 || code == Enums.MessageCode.CompositeReflectivity4)
			{
				plot.Add("rangeNmi", 247);
				plot.Add("rangeKm", 459.296f);
			}
		}
		else if (code == Enums.MessageCode.DigitalCorrelationCoefficient)
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
			plot.Add("rangeNmi", 162);
			plot.Add("rangeKm", 300.024f);
		}
		else if (code == Enums.MessageCode.DigitalDifferentialReflectivity)
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
			plot.Add("rangeNmi", 162);
			plot.Add("rangeKm", 300.024f);
		}
		else if (code == Enums.MessageCode.DigitalAccumulationArray)
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
			plot.Add("rangeNmi", 124);
			plot.Add("rangeKm", 229.648f);
		}
		else if (code == Enums.MessageCode.DigitalPrecipitationArray)
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
			plot.Add("rangeNmi", 124);
			plot.Add("rangeKm", 229.648f);
		}
		else if (code == Enums.MessageCode.DigitalHybridScanReflectivity || code == Enums.MessageCode.HybridScanReflectivity)
		{
			reader.BaseStream.Seek(32, SeekOrigin.Current);
			productDescriptionData.Add("maxReflectivity", readShort(reader));
			DateTime dateOfScan = DateTime.UnixEpoch.AddDays(readShort(reader));
			productDescriptionData.Add("dateOfScan", dateOfScan);
			reader.BaseStream.Seek(4, SeekOrigin.Current);
			productDescriptionData.Add("averageTimeOfHybridScan", readShort(reader));
			productDescriptionData.Add("compressionMethod", readShort(reader));
			productDescriptionData.Add("uncompressedSize", (readUShort(reader) << 16) + readUShort(reader)); // bytes
			plot.Add("rangeNmi", 124);
			plot.Add("rangeKm", 229.648f);
		}
		else if (code == Enums.MessageCode.RadarStatusLog)
		{
			reader.BaseStream.Seek(42, SeekOrigin.Current);
			productDescriptionData.Add("compressionMethod", readShort(reader));
			productDescriptionData.Add("uncompressedSize", (readUShort(reader) << 16) + readUShort(reader)); // bytes
		}
		else if (code == Enums.MessageCode.DigitalInstantaneousPrecipitationRate)
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
			plot.Add("rangeNmi", 124);
			plot.Add("rangeKm", 229.648f);
		}
		else if (code == Enums.MessageCode.DigitalSpecificDifferentialPhase)
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
			plot.Add("rangeNmi", 162);
			plot.Add("rangeKm", 300.024f);
		}
		else if (code == Enums.MessageCode.OneHourAccumulation)
		{
			productDescriptionData.Add("nullProductFlag", readShort(reader));
			reader.BaseStream.Seek(32, SeekOrigin.Current);
			productDescriptionData.Add("maxAccumulation", readShort(reader) * 0.1); // inches
			DateTime accumEndDate = DateTime.UnixEpoch.AddDays(readShort(reader)).AddSeconds(readShort(reader));
			productDescriptionData.Add("accumulationEndDate", accumEndDate);
			productDescriptionData.Add("accumulationEndTime", accumEndDate.AddSeconds(readShort(reader)));
			productDescriptionData.Add("meanFieldBias", readShort(reader) * 0.01);
			reader.BaseStream.Seek(4, SeekOrigin.Current);
			plot.Add("rangeNmi", 124);
			plot.Add("rangeKm", 229.648f);
		}
		else if (code == Enums.MessageCode.DigitalUserSelectableTotalAccumulation)
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
			plot.Add("rangeNmi", 124);
			plot.Add("rangeKm", 229.648f);
		}
		else if (code == Enums.MessageCode.DigitalOneHourDifferenceAccumulation)
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
			plot.Add("rangeNmi", 124);
			plot.Add("rangeKm", 229.648f);
		}
		else if (code == Enums.MessageCode.DigitalStormTotalDifferenceAccumulation || code == Enums.MessageCode.DigitalStormTotalAccumulation)
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
			plot.Add("rangeNmi", 124);
			plot.Add("rangeKm", 229.648f);
		}
		else if (code == Enums.MessageCode.VelocityAzimuthDisplayWindProfile)
		{
			reader.BaseStream.Seek(34, SeekOrigin.Current);
			productDescriptionData.Add("maxWindSpeed", readShort(reader)); // knots
			productDescriptionData.Add("directionOfMaxSpeed", readShort(reader)); // degrees
			productDescriptionData.Add("altitudeOfMaxSpeed", readShort(reader) * 0.01); // feet * 0.01 ranging 0 to 70
			reader.BaseStream.Seek(8, SeekOrigin.Current);
		}
		else if (code == Enums.MessageCode.LegacyBaseSpectrumWidth1 || code == Enums.MessageCode.LegacyBaseSpectrumWidth2 || code == Enums.MessageCode.LegacyBaseSpectrumWidth3)
		{
			productDescriptionData.Add("elevationAngle", readShort(reader) * 0.1f);
			reader.BaseStream.Seek(32, SeekOrigin.Current);
			productDescriptionData.Add("maxSpectrumWidth", readShort(reader)); // knots
			reader.BaseStream.Seek(6, SeekOrigin.Current);
			productDescriptionData.Add("deltaTime", readShort(reader));
			reader.BaseStream.Seek(4, SeekOrigin.Current);
			if (code == Enums.MessageCode.LegacyBaseSpectrumWidth1)
			{
				plot.Add("rangeNmi", 32);
				plot.Add("rangeKm", 59.264f);
			}
			else if (code == Enums.MessageCode.LegacyBaseSpectrumWidth2)
			{
				plot.Add("rangeNmi", 62);
				plot.Add("rangeKm", 114.824f);
			}
			else if (code == Enums.MessageCode.LegacyBaseSpectrumWidth3)
			{
				plot.Add("rangeNmi", 124);
				plot.Add("rangeKm", 229.648f);
			}
		}
		else
		{
			self.INTERNAL_debugLog("No data found in halfwords 30-53. This product may not be supported. Advancing...", ConsoleColor.Yellow);
			reader.BaseStream.Seek(48, SeekOrigin.Current); // skip 48 bytes, since the specified product has no data between halfwords 30-53
		}
	}
}
