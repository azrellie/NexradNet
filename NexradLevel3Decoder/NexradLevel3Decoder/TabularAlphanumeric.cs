using static Azrellie.Meteorology.NexradNet.Level3.Util;

namespace Azrellie.Meteorology.NexradNet.Level3;

public record TabularAlphanumeric
{
	public short BlockID { get; init; }
	public int LengthOfBlock { get; init; }
	public short NumberOfPages { get; init; }
	public List<List<string>> Pages { get; init; } = [];

	public TabularAlphanumeric(BinaryReader reader, Level3 self)
	{
		if (readShort(reader) != -1)
			throw new("Invalid block divider"); // throw the scary error of terror if the block divider is invalid

		BlockID = readShort(reader);
		LengthOfBlock = readInt(reader);
		reader.BaseStream.Seek(self.HeaderLength + self.ProductDescriptionLength, SeekOrigin.Current); // skip header and product description in tabular alphanumeric (for some reason its defined twice in products with tabular data)

		if (readShort(reader) != -1)
			throw new("Invalid block divider"); // throw the scary error of terror if the block divider is invalid

		NumberOfPages = readShort(reader);
		short numberOfCharacters = readShort(reader);

		self.INTERNAL_debugLog("[TABULAR] Reading tabular alphanumeric pages...", ConsoleColor.Yellow);
		for (int i = 0; i < NumberOfPages; i++)
		{
			List<string> lines = [];
			string line = string.Empty;
			short c = readShort(reader);
			self.INTERNAL_debugLog("[TABULAR] Reading page " + (i + 1) + "...", ConsoleColor.Yellow);
			while (c != -1)
			{
				if (c != 0x0050) // line ending (not in documentation)
				{
					byte highByte = (byte)(c >> 8);
					char char1 = (char)highByte;
					line += char1;
					if (line.Length % numberOfCharacters == 0)
					{
						lines.Add(line);
						line = string.Empty;
					}

					byte lowByte = (byte)(c & 0xFF);
					char char2 = (char)lowByte;
					line += char2;
					if (line.Length % numberOfCharacters == 0)
					{
						lines.Add(line);
						line = string.Empty;
					}
				}

				// also not in documentation, but there may be no -1 end of page indicator.
				// to prevent the risk of any errors, check if stream position is equal/higher to streams length
				// good enough substitute for end of page indication nonetheless
				if (reader.BaseStream.Position >= reader.BaseStream.Length)
					break;

				c = readShort(reader);
			}
			Pages.Add(lines);
		}
	}
}