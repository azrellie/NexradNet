using SevenZipExtractor;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using static Azrellie.Meteorology.NexradNet.Level3.Util;

namespace Azrellie.Meteorology.NexradNet.Level3;

/*
TODO:
add support for storm structure NSS
add support for melting layer ML
add support for digital precipitation array DPA
add support for power removed control
add more product enums
*/
public record Level3
{
	public TextHeader TextHeader { get; init; }
	public string? FreeTextMessage { get; init; }
	public Header? Header { get; init; }
	public ProductDescription? ProductDescription { get; init; }
	public ProductSymbology? ProductSymbology { get; init; }
	public GraphicAlphanumeric? GraphicAlphanumeric { get; init; }
	public TabularAlphanumeric? TabularAlphanumeric { get; init; }
	
	/// <summary>
	/// Memory used to process the product in bytes.
	/// </summary>
	public int MemoryUsage { get; init; }
	/// <summary>
	/// Processing time in milliseconds.
	/// </summary>
	public TimeSpan ProcessingTime { get; init; }
	public int HeaderLength { get; init; }
	public int ProductDescriptionLength { get; init; }

	public long SizeNoTextHeader { get; init; }
	private readonly bool debugLoggingEnabled;

	/// <summary>
	/// Reads the tabular data from a processed radar file into an easier to use format.
	/// </summary>
	/// <param name="data">The tabular data from a processed radar file.</param>
	public MesocycloneDetectionDataCollection parseMesocycloneData(List<List<string>> data)
	{
		List<MesocycloneDetectionData> mesocycloneList = [];
		for (int i = 0; i < data[0].Count; i++)
		{
			string strData = data[0][i];
			if (strData.Length == 0) continue;
			if (i == 0 || (i >= 2 && i <= 5)) continue;
			if (i != 1)
			{
				string circulationId = strData[..6].Trim();
				string azimuth = strData[6..13].Split('/')[0];
				string range = strData[6..13].Split('/')[1];
				string strengthRank = strData[13..17].Trim();
				string stormId = strData[17..20].Trim();
				string lowLevelRotationalVelocity = strData[20..25].Trim();
				string lowLevelGateToGateShear = strData[25..29].Trim();
				string baseHeight = strData[29..35].Trim();
				string depth = strData[35..40].Trim();
				string stormRelativeDepthPercentage = strData[40..48].Trim();
				string heightOfMaxRotationalVelocity = strData[48..53].Trim();
				string maxRotationalVelocity = strData[53..60].Trim();
				string tvs = strData[60..63].Trim();
				string motionDegrees = string.Empty;
				string motionVelocity = string.Empty;
				try
				{
					motionDegrees = strData[63..72].Split('/')[0].Trim();
					motionVelocity = strData[63..72].Split('/')[1].Trim();
				}
				catch
				{
					INTERNAL_debugLog("Motion degrees and velocity not found in mesocyclone. Skipping...", ConsoleColor.Red);
				}
				string mesocycloneStrengthIndex = strData[72..80].Trim();
				MesocycloneDetectionData mdData = new()
				{
					circulationID = short.Parse(circulationId),
					azimuth = short.Parse(azimuth),
					range = byte.Parse(range),
					strengthRank = strengthRank,
					associatedSCITStormID = stormId,
					lowLevelRotationalVelocity = byte.Parse(lowLevelRotationalVelocity),
					lowLevelGateToGateVelocityDifference = byte.Parse(lowLevelGateToGateShear),
					baseHeight = baseHeight,
					depth = depth,
					stormRelativeDepthPercentage = byte.Parse(stormRelativeDepthPercentage),
					maxRotationalVelocity = byte.Parse(maxRotationalVelocity),
					heightOfMaxRotationalVelocity = byte.Parse(heightOfMaxRotationalVelocity),
					tornadoVortexSignature = tvs == "Y",
					motionDegrees = motionDegrees == string.Empty ? short.MinValue : short.Parse(motionDegrees),
					motionVelocity = motionVelocity == string.Empty ? short.MinValue : short.Parse(motionVelocity),
					mesocycloneStrengthIndex = int.Parse(mesocycloneStrengthIndex)
				};
				mesocycloneList.Add(mdData);
			}
		}

		short radarId = short.MinValue;
		DateTime dateTime = DateTime.MinValue;
		short averageDirectionDegrees = short.MinValue;
		short averageSpeedKts = short.MinValue;
		string[] lines = [..data[0].Take(5)];
		for (int i = 0; i < lines.Length; i++)
		{
			string line = lines[i].Trim();
			if (i == 1)
			{
				radarId = short.Parse(line[9..16]);
				string date = line[23..34].Trim();
				string time = line[42..52].Trim();
				dateTime = DateTime.SpecifyKind(DateTime.ParseExact(date + " " + time, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture), DateTimeKind.Utc);
				averageDirectionDegrees = short.Parse(line[66..70]);
				averageSpeedKts = short.Parse(line[71..74]);
			}
		}
		MesocycloneDetectionDataCollection dataCollection = new()
		{
			radarID = radarId,
			date = dateTime,
			averageDirectionDegrees = averageDirectionDegrees,
			averageSpeedKts = averageSpeedKts,
			data = mesocycloneList
		};
		return dataCollection;
	}

	/// <summary>
	/// Reads and processes a level 3 radar file.
	/// </summary>
	/// <remarks>The "processing stages" parameter is structured as "0b111". The default value is 0b111. Read the parameter for more info.</remarks>
	/// <param name="reader">The binary reader.</param>
	/// <param name="processingStages">Parts of the product that should be processed. The 3 bits represent each stage. (left to right) Bit 1 = tabular, bit 2 = graphic, bit 3 = symbology. If any bit is 0, then it will not be processed.</param>
	/// <param name="textHeaderLength">The length of the text header.</param>
	/// <param name="guessFormat">Whether the library should try to guess the format of the file.</param>
	/// <param name="enableDebugging">Whether the library should print to the output for debugging reasons.</param>
	public Level3(ref BinaryReader reader, byte processingStages = 0b111, int textHeaderLength = -1, bool guessFormat = true, bool enableDebugging = true)
	{
		var benchmarkStart = DateTime.UtcNow;
		long totalMemory = GC.GetTotalMemory(true);
		if (textHeaderLength < 0 && !guessFormat)
			throw new("Text header length cannot be uninitialized with guess format set to false.");

		if (guessFormat)
		{
			byte? lastByte = null;
			for (int i = 0; i < 100; i++)
			{
				byte thisByte = reader.ReadByte();
				if (lastByte != null)
				{
					short value = BitConverter.ToInt16([thisByte, lastByte.Value]);
					if (Enum.IsDefined(typeof(Enums.MessageCode), (int)value)) // check for a valid message code
					{
						textHeaderLength = (int)reader.BaseStream.Position - 2;
						break;
					}
					else if (value == 30938) // check for a zlib compression header
					{
						textHeaderLength = (int)reader.BaseStream.Position - 2;
						break;
					}
				}
				lastByte = thisByte;
			}
			reader.BaseStream.Position = 0;
		}

		debugLoggingEnabled = enableDebugging;

		INTERNAL_debugLog("Checking for zlib compression...", ConsoleColor.Yellow);
		bool zlib = usesZlibCompression(reader, textHeaderLength);
		if (zlib)
		{
			INTERNAL_debugLog("Product is zlib compressed. Decompressing...", ConsoleColor.Yellow);
			reader.BaseStream.Seek(textHeaderLength + 2, SeekOrigin.Begin);

			MemoryStream decompressedData = new();

			if (hasMultipleZlibChunks(reader))
				decompressedData = decompressZlibChunks(reader, this);
			else
			{
				MemoryStream compressedData = new();
				reader.BaseStream.CopyTo(compressedData);
				compressedData.Position = 0;

				DeflateStream deflate = new(compressedData, CompressionMode.Decompress);
				deflate.CopyTo(decompressedData);
				decompressedData.Position = 0;
			}

			INTERNAL_debugLog("Loading decompressed data to binary reader...", ConsoleColor.Yellow);
			BinaryReader r = new(decompressedData);
			r.BaseStream.Seek(4, SeekOrigin.Begin); // skip random data here
			reader = r;
		}
		else
			INTERNAL_debugLog("Product was not compressed. Moving to text header.", ConsoleColor.Yellow);

		INTERNAL_debugLog("Processing text header...", ConsoleColor.Yellow);
		TextHeader = new(reader, textHeaderLength, zlib);

		if (TextHeader.DataType == "FTM")
		{
			INTERNAL_debugLog($"Free text message product detected. Reading text...", ConsoleColor.Yellow);
			FreeTextMessage = Encoding.ASCII.GetString(read(reader, reader.BaseStream.Length - reader.BaseStream.Position));
			INTERNAL_debugLog("Finished processing free text message.\nText:\n" + FreeTextMessage, ConsoleColor.Yellow);
			Console.ForegroundColor = ConsoleColor.Gray;
			Console.WriteLine();
			reader.Close();
			reader.Dispose();
			return;
		}

		SizeNoTextHeader = reader.BaseStream.Position;
		INTERNAL_debugLog("Processing header...", ConsoleColor.Yellow);
		long p1 = reader.BaseStream.Position;
		Header = new(reader);
		HeaderLength = (int)(reader.BaseStream.Position - p1);

		// product support validation
		foreach (Enums.MessageCode code in unsupportedProducts)
			if (Header.MessageCode == code)
				{
					INTERNAL_debugLog($"Unsupported product detected.\nProduct {Header.MessageCode} (code={(int)Header.MessageCode}) is not yet supported.", ConsoleColor.Red);
					throw new($"Product {Header.MessageCode} (code={(int)Header.MessageCode}) is not yet supported.");
				}

		INTERNAL_debugLog($"Product \"{Header.MessageCode}\" detected.", ConsoleColor.Yellow);
		INTERNAL_debugLog("Processing product description...", ConsoleColor.Yellow);
		long p2 = reader.BaseStream.Position;
		ProductDescription = new(reader, this);
		ProductDescriptionLength = (int)(reader.BaseStream.Position - p2);

		if (ProductDescription.SymbologyOffset != 0 && (processingStages & (1 << 0)) != 0)
		{
			var symbologyBenchmarkStart = DateTime.UtcNow;
			long symbologyTotalMemory = GC.GetTotalMemory(true);

			INTERNAL_debugLog("Non zero product symbology offset detected. Processing product symbology...", ConsoleColor.Yellow);
			if (zlib)
				reader.BaseStream.Seek((ProductDescription.SymbologyOffset * 2) + SizeNoTextHeader, SeekOrigin.Begin); // skip to symbology
			else
				reader.BaseStream.Seek((ProductDescription.SymbologyOffset * 2) + textHeaderLength, SeekOrigin.Begin); // skip to symbology
			BinaryReader symbologyReader;
			ProductDescription.ProductData2.productDescriptionData.TryGetValue("compressionMethod", out dynamic? value);
			bool isDataCompressed = value != 0 && value != null; // check if compression method is 0 or null (no compression applied)

			byte[] bzipCompressionHeader = read(reader, 4); // check for bzip compression in the product
			if (bzipCompressionHeader[0] == 0x42 && bzipCompressionHeader[1] == 0x5A && bzipCompressionHeader[2] == 0x68 && bzipCompressionHeader[3] >= 0x31 && bzipCompressionHeader[3] <= 0x39)
				isDataCompressed = true;
			reader.BaseStream.Position -= 4;

			if (!isDataCompressed)
			{
				// product data was not compressed, assign symbology reader with current binary reader
				INTERNAL_debugLog("Product symbology not compressed. Advancing...", ConsoleColor.Yellow);
				symbologyReader = reader;
			}
			else
			{
				INTERNAL_debugLog("Product symbology was compressed. Decompressing...", ConsoleColor.Yellow);

				// product data was compressed with bzip2 format, decompress it
				long pos = reader.BaseStream.Position;
				byte[] bytes = read(reader, reader.BaseStream.Length - pos);
				MemoryStream compressedData = new(bytes)
				{
					Position = 0
				};
				MemoryStream decompressedData = new();

				// decompress data
				using ArchiveFile zipReader = new(compressedData);
				foreach (Entry entry in zipReader.Entries)
					entry.Extract(decompressedData);

				// create a new stream and copy the file header first, then the decompressed data
				reader.BaseStream.Position = 0;
				byte[] fileHeader = read(reader, pos);

				MemoryStream combinedStream = new(fileHeader.Length + (int)decompressedData.Length);
				combinedStream.Write(fileHeader, 0, fileHeader.Length);

				decompressedData.Position = 0;
				decompressedData.CopyTo(combinedStream);

				combinedStream.Position = pos;  // ready for reading symbology
				symbologyReader = new(combinedStream); // skip to symbology by starting from the beginning reader position to begin reading symbology
			}

			var symbologyBenchmarkEnd = DateTime.UtcNow;
			long symbologyMemNow = GC.GetTotalMemory(true);
			var memoryUsage = (int)(symbologyMemNow - symbologyTotalMemory);
			var processingTime = symbologyBenchmarkEnd - symbologyBenchmarkStart;
			INTERNAL_debugLog($"Product symbology decompress took {processingTime.TotalMilliseconds} ms to process using {memoryUsage / 1000.0:F2} kb or {memoryUsage / (1000.0 * 1000.0):F2} mb of memory.", ConsoleColor.Yellow);

			ProductSymbology = new(symbologyReader, ProductDescription, this);
		}

		if (ProductDescription.GraphicOffset != 0 && (processingStages & (1 << 1)) != 0)
		{
			var graphicBenchmarkStart = DateTime.UtcNow;
			long graphicTotalMemory = GC.GetTotalMemory(true);

			INTERNAL_debugLog("Non zero graphic alphanumeric offset detected. Processing product graphic alphanumerics...", ConsoleColor.Yellow);
			if (zlib)
				reader.BaseStream.Seek((ProductDescription.GraphicOffset * 2) + SizeNoTextHeader, SeekOrigin.Begin); // skip to graphic alphanumeric block
			else
				reader.BaseStream.Seek((ProductDescription.GraphicOffset * 2) + textHeaderLength, SeekOrigin.Begin); // skip to graphic alphanumeric block

			if (reader.BaseStream.Position < reader.BaseStream.Length)
				GraphicAlphanumeric = new(reader, ProductDescription, this);

			var graphicBenchmarkEnd = DateTime.UtcNow;
			long graphicMemNow = GC.GetTotalMemory(true);
			var memoryUsage = (int)(graphicMemNow - graphicTotalMemory);
			var processingTime = graphicBenchmarkEnd - graphicBenchmarkStart;
			INTERNAL_debugLog($"Graphic alphanuemric took {processingTime.TotalMilliseconds} ms to process using {memoryUsage / 1000.0:F2} kb or {memoryUsage / (1000.0 * 1000.0):F2} mb of memory.", ConsoleColor.Yellow);
		}

		if (ProductDescription.TabularOffset != 0 && (processingStages & (1 << 2)) != 0)
		{
			var tabularBenchmarkStart = DateTime.UtcNow;
			long tabularTotalMemory = GC.GetTotalMemory(true);

			INTERNAL_debugLog("Non zero tabular alphanumeric offset detected. Processing product tabular alphanumerics...", ConsoleColor.Yellow);
			if (zlib)
				reader.BaseStream.Seek((ProductDescription.TabularOffset * 2) + SizeNoTextHeader, SeekOrigin.Begin); // skip to tabular alphanumeric block
			else
				reader.BaseStream.Seek((ProductDescription.TabularOffset * 2) + textHeaderLength, SeekOrigin.Begin); // skip to tabular alphanumeric block

			if (reader.BaseStream.Position < reader.BaseStream.Length)
				TabularAlphanumeric = new(reader, this);

			var tabularBenchmarkEnd = DateTime.UtcNow;
			long tabularMemNow = GC.GetTotalMemory(true);
			var memoryUsage = (int)(tabularMemNow - tabularTotalMemory);
			var processingTime = tabularBenchmarkEnd - tabularBenchmarkStart;
			INTERNAL_debugLog($"Tabular alphanumeric decompress took {processingTime.TotalMilliseconds} ms to process using {memoryUsage / 1000.0:F2} kb or {memoryUsage / (1000.0 * 1000.0):F2} mb of memory.", ConsoleColor.Yellow);
		}

		INTERNAL_debugLog($"Finished processing product \"{Header.MessageCode}\".\nProduct details:\nMessage code: {Header.MessageCode}\nRadar station: {TextHeader.RadarStationId}\nGeneration date: {ProductDescription.GenerationDateOfProduct}", ConsoleColor.Yellow);
		Console.ForegroundColor = ConsoleColor.Gray;

		reader.Close();
		reader.Dispose();
		var benchmarkEnd = DateTime.UtcNow;
		long memNow = GC.GetTotalMemory(true);
		MemoryUsage = (int)(memNow - totalMemory);
		ProcessingTime = benchmarkEnd - benchmarkStart;
		INTERNAL_debugLog($"Total memory used: {(memNow - totalMemory) / 1000.0:F2} kb or {(memNow - totalMemory) / (1000.0 * 1000.0):F2} mb", ConsoleColor.Yellow);
		INTERNAL_debugLog($"Total processing time: {ProcessingTime.TotalMilliseconds} ms", ConsoleColor.Yellow);
	}

	/// <summary>
	/// Internal debug logging. Intended to be used within this class.
	/// </summary>
	/// <param name="msg"></param>
	/// <param name="foreground"></param>
	public void INTERNAL_debugLog(string msg, ConsoleColor foreground)
	{
		if (debugLoggingEnabled)
		{
			Console.ForegroundColor = foreground;
			Console.Write("[NEXRAD LVL3] " + msg + "\n");
			Console.ForegroundColor = ConsoleColor.Gray;
		}
	}

	public static readonly Enums.MessageCode[] unsupportedProducts =
	[
		Enums.MessageCode.StormStructure,
		Enums.MessageCode.MeltingLayer,
		Enums.MessageCode.DigitalPrecipitationArray,
		Enums.MessageCode.SupplementalPrecipitationData
	];
}