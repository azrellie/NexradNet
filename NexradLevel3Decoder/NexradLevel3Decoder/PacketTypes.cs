using static Azrellie.Meteorology.NexradNet.Level3.Util;
using static Azrellie.Meteorology.NexradNet.Level3.Enums;
using System.Text;
using System.Numerics;

namespace Azrellie.Meteorology.NexradNet.Level3;

public class PacketTypes
{
	static readonly Dictionary<byte, string> hydrometeorClassMap = new()
	{
		[0] = "No Data",
		[10] = "Biological",
		[20] = "Ground Clutter",
		[30] = "Ice Crystals",
		[40] = "Dry Snow",
		[50] = "Wet Snow",
		[60] = "Light/Moderate Rain",
		[70] = "Heavy Rain",
		[80] = "Big Drops",
		[90] = "Graupel",
		[100] = "Hail, possibly with rain",
		[110] = "Large Hail",
		[120] = "Giant Hail",
		[130] = "?",
		[140] = "Unknown",
		[150] = "Range Folded",
	};
	static readonly Dictionary<byte, string> hybridHydrometeorClassMap = new()
	{
		[0] = "?",
		[10] = "?",
		[20] = "No Data",
		[30] = "Range Folded",
		[40] = "Biological",
		[50] = "Ground Clutter",
		[60] = "Ice Crystals",
		[70] = "Graupel",
		[80] = "Wet Snow",
		[90] = "Dry Snow",
		[100] = "Light and moderate rain",
		[110] = "Heavy Rain",
		[120] = "Big Drops",
		[130] = "Hail and rain mixed",
		[140] = "Unknown",
		[150] = "Large Hail",
		[160] = "Giant Hail",
	};
	static readonly Dictionary<byte, string> rainRateClassMap = new()
	{
		[0] = "No Precipitation",
		[10] = "Unfilled",
		[20] = "Convective R(Z,ZDR)",
		[30] = "Tropical R(Z,ZDR)",
		[40] = "Specific Attenuation",
		[50] = "R(KDP) 25 coeff.",
		[60] = "R(KDP) 44 coeff.",
		[70] = "R(Z)",
		[80] = "R(Z) * 0.6",
		[90] = "R(Z) * 0.8",
		[100] = "R(Z) * multiplier",
	};

	public static byte[] runLengthEncoding(byte b)
	{
		int run = b >> 4;
		int value = b & 0x0F;
		if (run == 0) return [];
		Span<byte> buffer = stackalloc byte[16];
		buffer[..run].Fill((byte)value);
		return [..buffer[..run]];
	}
	
	// raster data packet
	static SymbologyPacket47623 packet47623(BinaryReader reader, ProductDescription productDescription)
	{
		ushort packetCode = readUShort(reader);
		reader.BaseStream.Seek(4, SeekOrigin.Current); // skip 2 packet codes 8000 and 00C0
		short iCoordinateStart = readShort(reader);
		short jCoordinateStart = readShort(reader);
		short xScaleInt = readShort(reader);
		reader.BaseStream.Seek(2, SeekOrigin.Current);
		short yScaleInt = readShort(reader);
		reader.BaseStream.Seek(2, SeekOrigin.Current);
		short numberOfRows = readShort(reader);
		reader.BaseStream.Seek(2, SeekOrigin.Current); // skip packing descriptor

		List<byte[]> rasterData = [];
		for (int i = 0; i < numberOfRows; i++)
		{
			short bytesInThisRow = readShort(reader);
			List<byte> bins = [];
			// multiple sequences of pixel data is stored in each byte reducing file size
			// this adds an array of bytes from a single byte into the bins array using run length encoding
			for (int j = 0; j < bytesInThisRow; j++)
				bins.AddRange(runLengthEncoding(readByte(reader)));
			rasterData.Add([..bins]);
		}

		SymbologyPacket47623 packet = new()
		{
			PacketCode = packetCode,
			ICoordinateStart = iCoordinateStart,
			JCoordinateStart = jCoordinateStart,
			XScaleInt = xScaleInt,
			YScaleInt = yScaleInt,
			NumberOfRows = numberOfRows,
			RasterData = [..rasterData]
		};
		return packet;
	}

	// low res radial data packet
	static SymbologyPacket44831 packet44831(BinaryReader reader, ProductDescription productDescription)
	{
		ushort packetCode = readUShort(reader);
		short indexOfFirstRangeBin = readShort(reader);
		short numberOfRangeBins = readShort(reader);
		short iCenterOfSweep = readShort(reader);
		short jCenterOfSweep = readShort(reader);
		float rangeScaleFactor = readShort(reader) * 0.001f;
		short numberOfRadials = readShort(reader);

		List<Radial> radials = [];
		for (int i = 0; i < numberOfRadials; i++)
		{
			if (reader.BaseStream.Position >= reader.BaseStream.Length) break;
			int rleLength = readShort(reader) * 2;
			long remainingBytes = reader.BaseStream.Length - reader.BaseStream.Position;

			if (remainingBytes <= 2)
				break;
			if (rleLength > remainingBytes)
				rleLength = (int)remainingBytes;

			Radial16 radial = new()
			{
				StartAngle = readShort(reader) * 0.1f,
				AngleDelta = readShort(reader) * 0.1f
			};
			byte[] bins = new byte[numberOfRangeBins];
			int binPos = 0;
			for (int j = 0; j < rleLength; j++)
			{
				if (reader.BaseStream.Position >= reader.BaseStream.Length) break;
				byte[] data = runLengthEncoding(readByte(reader));
				for (int k = 0; k < data.Length && binPos < bins.Length; k++)
					bins[binPos++] = data[k];
			}

			Radial16 radialCopy = radial with
			{
				Bins = bins
			};
			radials.Add(radialCopy);
		}

		SymbologyPacket44831 packet = new()
		{
			PacketCode = packetCode,
			IndexOfFirstRangeBin = indexOfFirstRangeBin,
			NumberOfRangeBins = numberOfRangeBins,
			ICenterOfSweep = iCenterOfSweep,
			JCenterOfSweep = jCenterOfSweep,
			RangeScaleFactor = rangeScaleFactor,
			NumberOfRadials = numberOfRadials,
			Radials = [..radials]
		};

		return packet;
	}

	// generic data packet
	static SymbologyPacket28 packet28(BinaryReader reader, ProductDescription productDescription)
	{
		ushort packetCode = readUShort(reader);
		reader.BaseStream.Seek(2, SeekOrigin.Current); // reserved data
		int lengthOfSerializedData = readInt(reader);
		int nameLength = readInt(reader);
		string productName = Encoding.ASCII.GetString(read(reader, nameLength));

		if (int.IsOddInteger(nameLength))
			reader.BaseStream.Seek(1, SeekOrigin.Current); // skip null terminator because we already have a known length of the string
		if (int.IsEvenInteger(nameLength) && nameLength % 4 > 0)
			reader.BaseStream.Seek(2, SeekOrigin.Current); // skip 2 null terminators because we already have a known length of the string

		int descLength = readInt(reader);
		string productDesc = Encoding.ASCII.GetString(read(reader, descLength)); // not the same as the ProductDescription class

		if (int.IsOddInteger(descLength))
			reader.BaseStream.Seek(1, SeekOrigin.Current); // skip null terminator because we already have a known length of the string
		if (int.IsEvenInteger(descLength) && descLength % 4 > 0)
			reader.BaseStream.Seek(2, SeekOrigin.Current); // skip 2 null terminators because we already have a known length of the string
		MessageCode productCode = (MessageCode)readInt(reader);
		int productType = readInt(reader);
		DateTime generationTime = DateTime.UnixEpoch.AddSeconds(readUInt(reader));
		int radarNameLength = readInt(reader);
		string radarName = Encoding.ASCII.GetString(read(reader, radarNameLength));
		float radarLatitude = readFloat(reader);
		float radarLongitude = readFloat(reader);
		float radarHeight = readFloat(reader);
		DateTime volumeScanStartTime = DateTime.UnixEpoch.AddSeconds(readUInt(reader));
		DateTime elevationScanStartTime = DateTime.UnixEpoch.AddSeconds(readUInt(reader)); // only used if packet.Type == 2
		float elevationAngle = readFloat(reader); // if its 0, then elevation angle does not apply to this product (only used if packet.Type == 2)
		int volumeScanNumber = readInt(reader);
		OperationalMode2 operationalMode = (OperationalMode2)readInt(reader);
		VolumeCoveragePattern volumeCoveragePattern = (VolumeCoveragePattern)readShort(reader); // apparently 0 as in not used in some circumstances?
		short elevationNumber = readShort(reader); // only used if packet.Type == 2
		reader.BaseStream.Seek(6, SeekOrigin.Current); // skip spare data for future use
		int numberOfParameters = readInt(reader);
		if (numberOfParameters == 0)
			reader.BaseStream.Seek(10, SeekOrigin.Current);
		int numberOfComponents = readInt(reader);

		List<Component> components = [];
		for (int a = 0; a < numberOfComponents; a++)
		{
			reader.BaseStream.Seek(4, SeekOrigin.Current);
			int componentType = readInt(reader);
			if (componentType == 1)
			{
				int radialDescLength = readInt(reader);
				string desc = Encoding.ASCII.GetString(read(reader, radialDescLength));
				reader.BaseStream.Seek(2, SeekOrigin.Current); // skip null terminator
				float binSize = readFloat(reader);
				float rangeToFirstBin = readFloat(reader);
				reader.BaseStream.Seek(4, SeekOrigin.Current);
				int numberOfRadials = readInt(reader);
				reader.BaseStream.Seek(4, SeekOrigin.Current);
				List<RadialData> radialDataList = [];
				for (int radialIndex = 0; radialIndex < numberOfRadials; radialIndex++)
				{
					float azimuth = readFloat(reader);
					float elevation = readFloat(reader);
					float width = readFloat(reader);
					int numberOfBins = readInt(reader);
					reader.BaseStream.Seek(4, SeekOrigin.Current);

					List<byte> typeList = [];
					for (int i = 0; i < 100; i++)
					{
						char b = (char)readByte(reader);
						if (b != ';')
							typeList.Add((byte)b);
						else
							break;
					}
					string type = Encoding.ASCII.GetString([..typeList]).Trim();
					typeList.Clear();

					for (int i = 0; i < 100; i++)
					{
						char b = (char)readByte(reader);
						if (b != '\0')
							typeList.Add((byte)b);
						else
							break;
					}
					string unit = Encoding.ASCII.GetString([..typeList]).Trim();

					reader.BaseStream.Seek(6, SeekOrigin.Current);

					float[] bins = new float[numberOfBins];
					float[] binsRaw = new float[numberOfBins];
					for (int binIndex = 0; binIndex < numberOfBins; binIndex++)
					{
						int data = readInt(reader);
						binsRaw[binIndex] = data;
						float offset = (float)productDescription.ProductData2.plot["offset"];
						float scale = (float)productDescription.ProductData2.plot["scale"];
						float processedData = (data - offset) / scale;
						bins[binIndex] = processedData;
					}

					RadialData radialData = new()
					{
						Azimuth = azimuth,
						Elevation = elevation,
						Width = width,
						NumberOfBins = numberOfBins,
						Type = type,
						Unit = unit,
						BinsRaw = [..binsRaw],
						Bins = [..bins]
					};
					radialDataList.Add(radialData);
				}
				RadialComponent radial = new()
				{
					ComponentType = componentType,
					Description = desc,
					BinSize = binSize,
					RangeToFirstBin = rangeToFirstBin,
					NumberOfRadials = numberOfRadials,
					RadialData = [..radialDataList]
				};
				components.Add(radial);
			}
			else if (componentType == 4)
			{
				TextComponent text = new()
				{
					ComponentType = componentType,
					NumberOfComponentParameters = readInt(reader),
					ComponentParameterList = readInt(reader)
				};
				int msgTypeLength = readInt(reader);
				string messageType = Encoding.ASCII.GetString(read(reader, msgTypeLength));
				int attributesLength = readInt(reader);
				string attributes = Encoding.ASCII.GetString(read(reader, attributesLength));

				// extra null terminators may be present at the end of the attributes string for an unknown reason
				// so we check the 4 bytes before hand and see if they are all 0x00. if so, extra null terminators are present
				byte[] bytes = readBytes(reader, 4);
				if (bytes[0] == '\0' && bytes[1] == '\0' && bytes[2] == '\0' && bytes[3] == '\0')
					reader.BaseStream.Position -= 2; // go back 2 bytes instead of 4 because we already skipped extra null terminators checking if any extras existed
				else
					reader.BaseStream.Position -= 4; // no extra null terminators, go back 4 bytes

				int textLength = readInt(reader);
				string textData = Encoding.ASCII.GetString(read(reader, textLength));
				for (int q = 0; q < 32; q++)
				{
					if (reader.BaseStream.Position >= reader.BaseStream.Length) // reached end of stream. terminate
						break;
					byte b = readByte(reader);
					if (b == 0x04)
					{
						reader.BaseStream.Position -= 8; // go back 8 bytes because we found where the next component type byte is at
						break;
					}
				}
				text.MessageType = messageType;
				text.AttributeData = attributes;
				text.Text = textData;
				components.Add(text);
			}
		}
		SymbologyPacket28 packet = new()
		{
			PacketCode = packetCode,
			LengthOfSerializedData = lengthOfSerializedData,
			ProductName = productName,
			ProductDescription = productDesc,
			ProductCode = productCode,
			Type = productType,
			GenerationTime = generationTime,
			RadarName = radarName,
			RadarLatitude = radarLatitude,
			RadarLongitude = radarLongitude,
			RadarHeight = radarHeight,
			VolumeScanStartTime = volumeScanStartTime,
			ElevationScanStartTime = elevationScanStartTime,
			ElevationAngle = elevationAngle,
			VolumeScanNumber = volumeScanNumber,
			OperationalMode = operationalMode,
			VolumeCoveragePattern = volumeCoveragePattern,
			ElevationNumber = elevationNumber,
			NumberOfParameters = numberOfParameters,
			NumberOfComponents = numberOfComponents,
			Components = [..components]
		};
		return packet;
	}

	// special graphic symbol packet with subpackets
	static SymbologyPacket23 packet23(BinaryReader reader, ProductDescription productDescription)
	{
		ushort packetCode = readUShort(reader);
		short lengthOfBlock = readShort(reader);
		List<SymbologyPacket> packets = [];
		for (int i = 0; i < lengthOfBlock / 2; i += 6)
		{
			ushort subPacketCode = readUShort(reader);
			reader.BaseStream.Position -= 2;
			if (subPacketCode == 24 || subPacketCode == 25) // dont want packet 24 or 25 in packet 23s packet collection
				break;
			SymbologyPacket subPacket = Packets[subPacketCode].Invoke(reader, productDescription);
			packets.Add(subPacket);
		}
		SymbologyPacket23 packet = new()
		{
			PacketCode = packetCode,
			LengthOfBlock = lengthOfBlock,
			Packets = [..packets]
		};
		return packet;
	}

	// mesocyclone packet
	static SymbologyPacket20 packet20(BinaryReader reader, ProductDescription productDescription)
	{
		ushort packetCode = readUShort(reader);
		short lengthOfBlock = readShort(reader);
		List<Mesocyclone> mesocyclones = [];
		for (int i = 0; (i < lengthOfBlock) && (i + 8 <= lengthOfBlock); i += 8)
		{
			Mesocyclone mesocyclone = new()
			{
				IPosition = readShort(reader),
				JPosition = readShort(reader),
				PointFeatureType = readShort(reader),
				PointFeatureAttribute = readShort(reader) / 4
			};
			mesocyclones.Add(mesocyclone);
		}
		SymbologyPacket20 packet = new()
		{
			PacketCode = packetCode,
			LengthOfBlock = lengthOfBlock,
			Mesocyclones = [..mesocyclones]
		};
		return packet;
	}

	// mesocyclone packet
	static SymbologyPacket19 packet19(BinaryReader reader, ProductDescription productDescription)
	{
		ushort packetCode = readUShort(reader);
		short lengthOfBlock = readShort(reader);
		List<Mesocyclone> mesocyclones = [];
		int i;
		for (i = 0; (i < lengthOfBlock) && (i + 8 <= lengthOfBlock); i += 8)
		{
			Mesocyclone mesocyclone = new();
			mesocyclone.IPosition = readShort(reader);
			mesocyclone.JPosition = readShort(reader);
			mesocyclone.PointFeatureType = readShort(reader);
			mesocyclone.PointFeatureAttribute = readShort(reader);
			mesocyclones.Add(mesocyclone);
		}
		if (i < lengthOfBlock)
			reader.BaseStream.Seek(lengthOfBlock - i, SeekOrigin.Current);
		SymbologyPacket19 packet = new()
		{
			PacketCode = packetCode,
			LengthOfBlock = lengthOfBlock,
			Mesocyclones = [..mesocyclones]
		};
		return packet;
	}

	// high res radial data packet
	static SymbologyPacket16 packet16(BinaryReader reader, ProductDescription productDescription)
	{
		ushort packetCode = readUShort(reader);
		short indexOfFirstRangeBin = readShort(reader);
		short numberOfRangeBins = readShort(reader);
		short iCenterOfSweep = readShort(reader);
		short jCenterOfSweep = readShort(reader);
		float rangeScaleFactor = readShort(reader) * 0.001f;
		short numberOfRadials = readShort(reader);
		float scale = float.MinValue;
		float offset = float.MinValue;
		float dataIncrement = float.MinValue;
		float minDataValue = float.MinValue;
		Dictionary<int, float> scaled = [];


		if (productDescription.ProductData2.plot.TryGetValue("maxDataValue", out object? _))
		{
			scale = (float)productDescription.ProductData2.plot["scale"];
			offset = (float)productDescription.ProductData2.plot["offset"];
		}

		if (productDescription.ProductData2.plot.TryGetValue("dataLevels", out object? dataLevels))
		{
			dataIncrement = (float)productDescription.ProductData2.plot["dataIncrement"];
			minDataValue = (int)productDescription.ProductData2.plot["minDataValue"];
			for (int n = 0; n <= (int)dataLevels; n++)
				scaled[n] = minDataValue + (n * dataIncrement);
		}

		MessageCode code = productDescription.ProductCode;

		float linearScale = float.MinValue; // halfword 31
		float linearOffset = float.MinValue; // halfword 32
		short digitalLogStart = short.MinValue; // halfword 33
		float logScale = float.MinValue; // halfword 34
		float logOffset = float.MinValue; // halfword 35
		if (code == MessageCode.DigitalVerticallyIntegratedLiquid)
		{
			productDescription.ProductData2.productDescriptionData.TryGetValue("linearScale", out object? linearScaleValue);
			productDescription.ProductData2.productDescriptionData.TryGetValue("linearOffset", out object? linearOffsetValue);
			productDescription.ProductData2.productDescriptionData.TryGetValue("digitalLogStart", out object? digitalLogStartValue);
			productDescription.ProductData2.productDescriptionData.TryGetValue("logScale", out object? logScaleValue);
			productDescription.ProductData2.productDescriptionData.TryGetValue("logOffset", out object? logOffsetValue);
			linearScale = (float)linearScaleValue;
			linearOffset = (float)linearOffsetValue;
			digitalLogStart = (short)digitalLogStartValue;
			logScale = (float)logScaleValue;
			logOffset = (float)logOffsetValue;
		}

		List<Radial> radials = [];
		int i = 0;
		for (; i < numberOfRadials; i++)
		{
			short bytesInRadial = readShort(reader);
			float startAngle = readShort(reader) * 0.1f;
			float angleDelta = readShort(reader) * 0.1f;

			bool alphanuemricProduct = code is MessageCode.HydrometeorClassification or MessageCode.HybridHydrometeorClassification or MessageCode.RainRateClassification or MessageCode.DigitalHydrometeorClassification;
			byte[] binsRaw = readBytes(reader, numberOfRangeBins);
			float[] bins = [..binsRaw.Select(x => (float)x)];
			float[]? processedBins = alphanuemricProduct ? null : new float[numberOfRangeBins];
			string[]? alphanumericBins = alphanuemricProduct ? new string[numberOfRangeBins] : null;

			Vector<float> offsetVec = new(offset);
			Vector<float> scaleVec = new(scale);
			Vector<float> minDataValueVec = new(minDataValue);
			Vector<float> dataIncVec = new(dataIncrement);
			int simdLength = Vector<float>.Count;
			int j = 0;
			// digital vil is a special case, since the offset and scale is different if the digital vil value is over a threshold (starting since build 9)
			if (!alphanuemricProduct)
				if (code is MessageCode.DigitalVerticallyIntegratedLiquid)
					for (; j < numberOfRangeBins; j++)
					{
						byte value = binsRaw[j];
						processedBins[j] = value < digitalLogStart ? (value - linearOffset) / linearScale : MathF.Exp((value - logOffset) / logScale); // add raw values
					}
				else
					for (; j < numberOfRangeBins - simdLength; j += simdLength)
					{
						// product uses scaled values instead of an offset/scale value
						if (dataIncrement != float.MinValue && minDataValue != float.MinValue)
						{
							Vector<float> value = new(bins, j);
							value = minDataValueVec + (value * dataIncVec);
							value.CopyTo(processedBins, j);
						}
						else
						{
							Vector<float> value = new(bins, j);
							value = (value - offsetVec) / scaleVec;
							value.CopyTo(processedBins, j);
						}
					}

			j = 0;
			if (alphanuemricProduct)
				for (; j < numberOfRangeBins; j++)
				{
					byte value = binsRaw[j];
					if (code is MessageCode.HydrometeorClassification or MessageCode.DigitalHydrometeorClassification)
						alphanumericBins[j] = hydrometeorClassMap[value];
					else if (code is MessageCode.HybridHydrometeorClassification)
						alphanumericBins[j] = hybridHydrometeorClassMap[value];
					else if (code is MessageCode.RainRateClassification)
						alphanumericBins[j] = rainRateClassMap[value];
				}

			if (!alphanuemricProduct)
				for (j = numberOfRadials - (numberOfRadials % Vector<float>.Count); j < numberOfRadials; j++)
					processedBins[i] = minDataValue + (bins[i] * dataIncrement);

			Radial255 radial = new()
			{
				StartAngle = startAngle,
				AngleDelta = angleDelta,
				Bins = processedBins ?? [],
				BinsRaw = binsRaw,
				BinsAlphanumeric = alphanumericBins ?? []
			};

			radials.Add(radial);
			if (bytesInRadial != numberOfRangeBins)
				reader.BaseStream.Seek(bytesInRadial - numberOfRangeBins, SeekOrigin.Current);
		}
		SymbologyPacket16 packet = new()
		{
			PacketCode = packetCode,
			IndexOfFirstRangeBin = indexOfFirstRangeBin,
			NumberOfRangeBins = numberOfRangeBins,
			ICenterOfSweep = iCenterOfSweep,
			JCenterOfSweep = jCenterOfSweep,
			RangeScaleFactor = rangeScaleFactor,
			NumberOfRadials = numberOfRadials,
			Radials = [..radials]
		};
		return packet;
	}

	// storm tracking information packet
	static SymbologyPacket15 packet15(BinaryReader reader, ProductDescription productDescription)
	{
		ushort packetCode = readUShort(reader);
		short lengthOfBlock = readShort(reader);
		long endByte = reader.BaseStream.Position + lengthOfBlock;
		List<(short p1, short p2, string text)> data = [];
		while (reader.BaseStream.Position < endByte)
			data.Add((readShort(reader), readShort(reader), Encoding.ASCII.GetString(read(reader, 2))));
		SymbologyPacket15 packet = new()
		{
			PacketCode = packetCode,
			LengthOfBlock = lengthOfBlock,
			Data = [..data]
		};
		return packet;
	}

	// tornado vortex signature packet
	static SymbologyPacket12 packet12(BinaryReader reader, ProductDescription productDescription)
	{
		ushort packetCode = readUShort(reader);
		short lengthOfBlock = readShort(reader);
		List<short[]> coordinates = [];
		int i = 0;
		for (; i < lengthOfBlock && i + 4 <= lengthOfBlock; i += 4)
		{
			short[] shorts = [readShort(reader), readShort(reader)];
			coordinates.Add(shorts);
		}
		reader.BaseStream.Seek(lengthOfBlock - i, SeekOrigin.Current);
		List<string> ids = [];
		if (lengthOfBlock - i == 2)
		{
			reader.BaseStream.Position -= 2;
			ids.Add(Encoding.ASCII.GetString(read(reader, 2)));
		}

		List<TornadoVortexSignature> tvsList = [];
		foreach (short[] coord in coordinates)
			foreach (string id in ids)
			{
				TornadoVortexSignature tvs = new()
				{
					IPosition = coord[0],
					JPosition = coord[1],
					StormID = id
				};
				tvsList.Add(tvs);
			}
		SymbologyPacket12 packet = new()
		{
			PacketCode = packetCode,
			LengthOfBlock = lengthOfBlock,
			TornadoVortexSignatures = [..tvsList]
		};
		return packet;
	}

	// vector packet
	static SymbologyPacket10 packet10(BinaryReader reader, ProductDescription productDescription)
	{
		ushort packetCode = readUShort(reader);
		short lengthOfBlock = readShort(reader);
		short colorLevel = readShort(reader);
		List<(Vector2 start, Vector2 end)> vectors = [];
		long endByte = reader.BaseStream.Position + lengthOfBlock - 2;
		while (reader.BaseStream.Position < endByte)
		{
			(Vector2 start, Vector2 end) = (new(), new());
			start.X = readShort(reader); // i position
			start.Y = readShort(reader); // j position
			end.X = readShort(reader); // i position
			end.Y = readShort(reader); // j position
			vectors.Add((start, end));
		}
		SymbologyPacket10 packet = new()
		{
			PacketCode = packetCode,
			LengthOfBlock = lengthOfBlock,
			ColorLevel = colorLevel,
			Vectors = [..vectors]
		};
		return packet;
	}

	// text and special symbols packet
	static SymbologyPacket8 packet8(BinaryReader reader, ProductDescription productDescription)
	{
		ushort packetCode = readUShort(reader);
		short lengthOfBlock = readShort(reader);
		short colorLevel = readShort(reader);
		short iStartingPoint = readShort(reader);
		short jStartingPoint = readShort(reader);
		string text = Encoding.ASCII.GetString(read(reader, lengthOfBlock - 6));
		SymbologyPacket8 packet = new()
		{
			PacketCode = packetCode,
			LengthOfBlock = lengthOfBlock,
			ColorLevel = colorLevel,
			IStartingPoint = iStartingPoint,
			JStartingPoint = jStartingPoint,
			Text = text
		};
		return packet;
	}

	// unlinked vector packet
	static SymbologyPacket7 packet7(BinaryReader reader, ProductDescription productDescription)
	{
		ushort packetCode = readUShort(reader);
		short lengthOfBlock = readShort(reader);
		short beginIVector1 = readShort(reader);
		short beginJVector1 = readShort(reader);
		short endIVector1 = readShort(reader);
		short endJVector1 = readShort(reader);
		short beginIVector2 = readShort(reader);
		short beginJVector2 = readShort(reader);
		short endIVector2 = readShort(reader);
		short endJVector2 = readShort(reader);
		SymbologyPacket7 packet = new()
		{
			PacketCode = packetCode,
			LengthOfBlock = lengthOfBlock,
			BeginIVector1 = beginIVector1,
			BeginJVector1 = beginJVector1,
			EndIVector1 = endIVector1,
			EndJVector1 = endJVector1,
			BeginIVector2 = beginIVector2,
			BeginJVector2 = beginJVector2,
			EndIVector2 = endIVector2,
			EndJVector2 = endJVector2
		};
		return packet;
	}

	// linked vector packet
	static SymbologyPacket6 packet6(BinaryReader reader, ProductDescription productDescription)
	{
		ushort packetCode = readUShort(reader);
		short lengthOfBlock = readShort(reader);
		short iStartingPoint = readShort(reader);
		short jStartingPoint = readShort(reader);
		List<Vector2> vectors = [];
		short endByte = (short)(reader.BaseStream.Position + lengthOfBlock - 4);
		while (endByte > reader.BaseStream.Position)
			vectors.Add(new(readShort(reader), readShort(reader)));
		SymbologyPacket6 packet = new()
		{
			PacketCode = packetCode,
			LengthOfBlock = lengthOfBlock,
			IStartingPoint = iStartingPoint,
			JStartingPoint = jStartingPoint,
			Vectors = [..vectors]
		};
		return packet;
	}

	// wind barb packet
	static SymbologyPacket4 packet4(BinaryReader reader, ProductDescription productDescription)
	{
		ushort packetCode = readUShort(reader);
		short lengthOfBlock = readShort(reader);
		short value = readShort(reader); // range of 1 to 5 per noaa documentation on page 112 on build 23
		short x = readShort(reader);
		short y = readShort(reader);
		short windDirection = readShort(reader);
		short windSpeed = readShort(reader);
		SymbologyPacket4 packet = new()
		{
			PacketCode = packetCode,
			LengthOfBlock = lengthOfBlock,
			Value = value,
			X = x,
			Y = y,
			WindDirection = windDirection,
			WindSpeed = windSpeed
		};
		return packet;
	}

	// text and special symbol packet
	static SymbologyPacket2 packet2(BinaryReader reader, ProductDescription productDescription)
	{
		ushort packetCode = readUShort(reader);
		short lengthOfBlock = readShort(reader);
		short iStartingPoint = readShort(reader);
		short jStartingPoint = readShort(reader);
		string specialSymbol = Encoding.ASCII.GetString(read(reader, 2));
		SymbologyPacket2 packet = new()
		{
			PacketCode = packetCode,
			LengthOfBlock = lengthOfBlock,
			IStartingPoint = iStartingPoint,
			JStartingPoint = jStartingPoint,
			SpecialSymbol = specialSymbol
		};
		return packet;
	}

	// text and special symbol packet
	static SymbologyPacket1 packet1(BinaryReader reader, ProductDescription productDescription)
	{
		ushort packetCode = readUShort(reader);
		short lengthOfBlock = (short)(readShort(reader) - 4); // not in documentation, but this includes the packet code and length of the next block, so we remove 4 bytes from the total
		short iStartingPoint = readShort(reader);
		short jStartingPoint = readShort(reader);
		string text = readString(reader, lengthOfBlock);
		SymbologyPacket1 packet = new()
		{
			PacketCode = packetCode,
			LengthOfBlock = lengthOfBlock,
			IStartingPoint = iStartingPoint,
			JStartingPoint = jStartingPoint,
			Text = text
		};
		return packet;
	}

	public static Dictionary<ushort, Func<BinaryReader, ProductDescription, SymbologyPacket>> Packets = new()
	{
		{ 47623, packet47623 },
		{ 44831, packet44831 },
		{ 28, packet28 },
		{ 23, packet23 },
		{ 20, packet20 },
		{ 19, packet19 },
		{ 16, packet16 },
		{ 15, packet15 },
		{ 12, packet12 },
		{ 10, packet10 },
		{ 6, packet6 },
		{ 8, packet8 },
		{ 7, packet7 },
		{ 4, packet4 },
		{ 2, packet2 },
		{ 1, packet1 }
	};
}
