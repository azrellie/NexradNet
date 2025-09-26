using static Azrellie.Meteorology.NexradNet.Level3.Util;

namespace Azrellie.Meteorology.NexradNet.Level3;

public record TextHeader
{
	public string FileType { get; init; }
	public string SiteId { get; init; }
	public DateTime? TimeStamp { get; init; }
	public string DataType { get; init; }
	public string RadarStationId { get; init; }
	public string City { get; init; }
	public string State { get; init; }
	public string StateAbbreviated { get; init; }

	public static readonly Dictionary<string, (string City, string StateOrCountryFull, string Abbreviation)> radarIdToLocation = new()
	{
		{ "KABR", ("Aberdeen", "South Dakota", "SD") },
		{ "KENX", ("Albany", "New York", "NY") },
		{ "KABX", ("Albuquerque", "New Mexico", "NM") },
		{ "KAMA", ("Amarillo", "Texas", "TX") },
		{ "PAHG", ("Anchorage / Kenai", "Alaska", "AK") },
		{ "PGUA", ("Andersen AFB", "Guam", "GU") },
		{ "KFFC", ("Atlanta", "Georgia", "GA") },
		{ "KBBX", ("Beale AFB", "California", "CA") },
		{ "PABC", ("Bethel", "Alaska", "AK") },
		{ "KBLX", ("Billings", "Montana", "MT") },
		{ "KBGM", ("Binghamton", "New York", "NY") },
		{ "PACG", ("Biorka Island / Sitka", "Alaska", "AK") },
		{ "KBMX", ("Birmingham", "Alabama", "AL") },
		{ "KBIS", ("Bismarck", "North Dakota", "ND") },
		{ "KFCX", ("Blacksburg", "Virginia", "VA") },
		{ "KCBX", ("Boise", "Idaho", "ID") },
		{ "KBOX", ("Boston / Taunton", "Massachusetts", "MA") },
		{ "KBRO", ("Brownsville", "Texas", "TX") },
		{ "KBUF", ("Buffalo", "New York", "NY") },
		{ "KCXX", ("Burlington", "Vermont", "VT") },
		{ "RKSG", ("Camp Humphreys", "South Korea", "KR") },
		{ "KFDX", ("Cannon AFB", "New Mexico", "NM") },
		{ "KCBW", ("Caribou", "Maine", "ME") },
		{ "KICX", ("Cedar City", "Utah", "UT") },
		{ "KGRK", ("Central Texas / Ft Hood", "Texas", "TX") },
		{ "KCLX", ("Charleston", "South Carolina", "SC") },
		{ "KRLX", ("Charleston", "West Virginia", "WV") },
		{ "KCYS", ("Cheyenne", "Wyoming", "WY") },
		{ "KLOT", ("Chicago", "Illinois", "IL") },
		{ "KILN", ("Cincinnati / Wilmington", "Ohio", "OH") },
		{ "KCLE", ("Cleveland", "Ohio", "OH") },
		{ "KCAE", ("Columbia", "South Carolina", "SC") },
		{ "KGWX", ("Columbus AFB", "Mississippi", "MS") },
		{ "KCRP", ("Corpus Christi", "Texas", "TX") },
		{ "KFTG", ("Denver / Front Range", "Colorado", "CO") },
		{ "KDMX", ("Des Moines", "Iowa", "IA") },
		{ "KDTX", ("Detroit", "Michigan", "MI") },
		{ "KDDC", ("Dodge City", "Kansas", "KS") },
		{ "KDOX", ("Dover AFB", "Delaware", "DE") },
		{ "KDLH", ("Duluth", "Minnesota", "MN") },
		{ "KDYX", ("Dyess AFB", "Texas", "TX") },
		{ "KEYX", ("Edwards AFB", "California", "CA") },
		{ "KEPZ", ("El Paso", "Texas", "TX") },
		{ "KLRX", ("Elko", "Nevada", "NV") },
		{ "KBHX", ("Eureka", "California", "CA") },
		{ "KVWX", ("Evansville", "Indiana", "IN") },
		{ "PAPD", ("Fairbanks / Pedro Dome", "Alaska", "AK") },
		{ "KFSX", ("Flagstaff", "Arizona", "AZ") },
		{ "KSRX", ("Fort Smith", "Arkansas", "AR") },
		{ "KFDR", ("Frederick / Altus AFB", "Oklahoma", "OK") },
		{ "KHPX", ("Ft Campbell", "Kentucky", "KY") },
		{ "KPOE", ("Ft Polk", "Louisiana", "LA") },
		{ "KEOX", ("Ft Rucker", "Alabama", "AL") },
		{ "KFWS", ("Ft Worth", "Texas", "TX") },
		{ "KAPX", ("Gaylord / Alpena area", "Michigan", "MI") },
		{ "KGGW", ("Glasgow", "Montana", "MT") },
		{ "KGLD", ("Goodland", "Kansas", "KS") },
		{ "KMVX", ("Grand Forks", "North Dakota", "ND") },
		{ "KGJX", ("Grand Junction", "Colorado", "CO") },
		{ "KGRR", ("Grand Rapids", "Michigan", "MI") },
		{ "KTFX", ("Great Falls", "Montana", "MT") },
		{ "KGRB", ("Green Bay", "Wisconsin", "WI") },
		{ "KGSP", ("Greer / Greenville", "South Carolina", "SC") },
		{ "KUEX", ("Hastings", "Nebraska", "NE") },
		{ "KHDX", ("Holloman AFB", "New Mexico", "NM") },
		{ "KHGX", ("Houston / Galveston", "Texas", "TX") },
		{ "KHTX", ("Huntsville / Hytop", "Alabama", "AL") },
		{ "KIND", ("Indianapolis", "Indiana", "IN") },
		{ "KJKL", ("Jackson", "Kentucky", "KY") },
		{ "KDGX", ("Jackson", "Mississippi", "MS") },
		{ "KJAX", ("Jacksonville", "Florida", "FL") },
		{ "RODN", ("Kadena AB", "Japan", "JP") },
		{ "PHKM", ("Kamuela / Kohala", "Hawaii", "HI") },
		{ "KEAX", ("Kansas City", "Missouri", "MO") },
		{ "KBYX", ("Key West", "Florida", "FL") },
		{ "PAKC", ("King Salmon", "Alaska", "AK") },
		{ "KMRX", ("Knoxville / Morristown", "Tennessee", "TN") },
		{ "RKJK", ("Kunsan", "Korea", "KO") },
		{ "KIWX", ("Northern Indiana / North Webster", "Indiana", "IN") },
		{ "KEVX", ("Eglin AFB / NW Florida", "Florida", "FL") },
		{ "KTLX", ("Oklahoma City", "Oklahoma", "OK") },
		{ "KOAX", ("Omaha", "Nebraska", "NE") },
		{ "KPAH", ("Paducah", "Kentucky", "KY") },
		{ "KPDT", ("Pendleton", "Oregon", "OR") },
		{ "KDIX", ("Philadelphia / Dover", "Pennsylvania / Delaware", "PA / DE") },
		{ "KIWA", ("Phoenix", "Arizona", "AZ") },
		{ "KPBZ", ("Pittsburgh", "Pennsylvania", "PA") },
		{ "KSFX", ("Pocatello", "Idaho", "ID") },
		{ "KGYX", ("Portland (ME)", "Maine", "ME") },
		{ "KRTX", ("Portland (OR)", "Oregon", "OR") },
		{ "KPUX", ("Pueblo", "Colorado", "CO") },
		{ "KDVN", ("Quad Cities / Davenport", "Iowa / Illinois", "IA / IL") },
		{ "KRAX", ("Raleigh / Durham", "North Carolina", "NC") },
		{ "KUDX", ("Rapid City", "South Dakota", "SD") },
		{ "KRGX", ("Reno", "Nevada", "NV") },
		{ "KRIW", ("Riverton", "Wyoming", "WY") },
		{ "KDAX", ("Sacramento", "California", "CA") },
		{ "KMTX", ("Salt Lake City", "Utah", "UT") },
		{ "KSJT", ("San Angelo", "Texas", "TX") },
		{ "KEWX", ("San Antonio / Austin", "Texas", "TX") },
		{ "KNKX", ("San Diego", "California", "CA") },
		{ "KMUX", ("San Francisco", "California", "CA") },
		{ "KHNX", ("San Joaquin Valley", "California", "CA") },
		{ "KSOX", ("Santa Ana Mountains", "California", "CA") },
		{ "KLWX", ("Sterling", "Virginia", "VA") },
		{ "KAKQ", ("Wakefield / Norfolk / Richmond", "Virginia", "VA") },
		{ "KICT", ("Wichita", "Kansas", "KS") },
		{ "KLTX", ("Wilmington", "North Carolina", "NC") },
		{ "KYUX", ("Yuma", "Arizona", "AZ") },
		{ "PHKI", ("South Kauai", "Hawaii", "HI") },
		{ "PHWA", ("South Shore", "Hawaii", "HI") },
		{ "PHMO", ("Molokai", "Hawaii", "HI") },
		{ "TJUA", ("San Juan", "Puerto Rico", "PR") },
		{ "KMLB", ("Melbourne", "Florida", "FL") },
		{ "KMKX", ("Milwaukee", "Wisconsin", "WI") },
		{ "KLBB", ("Lubbock", "Texas", "TX") },
		{ "KNQA", ("Memphis", "Tennessee", "TN") },
		{ "KAMX", ("Miami", "Florida", "FL") },
		{ "PAIH", ("Middleton Island", "Alaska", "AK") },
		{ "KMAF", ("Midland / Odessa", "Texas", "TX") },
		{ "KMQT", ("Marquette", "Michigan", "MI") },
		{ "KMXX", ("Maxwell AFB", "Alabama", "AL") },
		{ "KMAX", ("Medford", "Oregon", "OR") },
		{ "KLIX", ("New Orleans", "Louisiana", "LA") },
		{ "KOKX", ("New York / Upton", "New York", "NY") },
		{ "PAEC", ("Nome", "Alaska", "AK") },
		{ "KMPX", ("Minneapolis", "Minnesota", "MN") },
		{ "KMBX", ("Minot AFB", "North Dakota", "ND") },
		{ "KMSX", ("Missoula", "Montana", "MT") },
		{ "KMOB", ("Mobile", "Alabama", "AL") },
		{ "KTYX", ("Montague / Ft Drum", "New York", "NY") },
		{ "KVAX", ("Moody AFB", "Georgia", "GA") },
		{ "KMHX", ("Morehead City", "North Carolina", "NC") },
		{ "KJGX", ("Robins AFB", "Georgia", "GA") },
		{ "KTLH", ("Tallahassee", "Florida", "FL") },
		{ "KDFX", ("Laughlin AFB", "Texas", "TX") },
		{ "KILX", ("Licoln", "Illinois", "IL") },
		{ "KLZK", ("Little Rock", "Arkansas", "AR") },
		{ "KVTX", ("Los Angeles", "California", "CA") },
		{ "KLVX", ("Louisville", "Kentucky", "KY") },
		{ "KCCX", ("State College", "Pennsylvania", "PA") },
		{ "KSGF", ("Springfield", "Missouri", "MO") },
		{ "KOTX", ("Spokane", "Washington", "WA") },
		{ "KOHX", ("Nashville / Old Hickory Lake area", "Tennessee", "TN") },
		{ "KLCH", ("Lake Charles", "Louisiana", "LA") },
		{ "KLGX", ("Langley Hill", "Washington", "WA") },
		{ "KESX", ("Las Vegas", "Nevada", "NV") },
		{ "KARX", ("La Crosse", "Wisconsin", "WI") },
		{ "KLSX", ("St. Louis", "Missouri", "MO") },
		{ "KATX", ("Seattle / Langley Hill", "Washington", "WA") },
		{ "KSHV", ("Shreveport", "Louisiana", "LA") },
		{ "KFSD", ("Sioux Falls", "South Dakota", "SD") },
		{ "KLNX", ("North Platte", "Nebraska", "NE") },
		{ "KTBW", ("Tampa Bay / Ruskin", "Florida", "FL") },
		{ "KTWX", ("Topeka / Kansas City area", "Kansas", "KS") },
		{ "KEMX", ("Tucson", "Arizona", "AZ") },
		{ "KINX", ("Tulsa / Oklahoma area", "Oklahoma", "OK") },
		{ "KVNX", ("Vance AFB", "Oklahoma", "OK") },
		{ "KVBX", ("Vandenberg AFB", "California", "CA") }
	};

	public TextHeader(BinaryReader reader, int textHeaderLength, bool zlib)
	{
		if (!zlib)
		{
			int bytesToSkip = textHeaderLength % 30;
			reader.BaseStream.Seek(bytesToSkip, SeekOrigin.Current);
		}

		FileType = readString(reader, 6);

		// invalid format. abort
		if (!FileType.Contains("SDUS") && !zlib)
			throw new("File format is not valid for parsing. Did you check the text header length?");

		if (zlib)
			reader.BaseStream.Seek(21, SeekOrigin.Current);
		else
			reader.BaseStream.Seek(1, SeekOrigin.Current);

		SiteId = readString(reader, 4);
		reader.BaseStream.Seek(1, SeekOrigin.Current);
		string timeStamp = readString(reader, 6);
		DateTime utcNow = DateTime.UtcNow;
		int day = int.Parse(timeStamp[..2]);
		int hour = int.Parse(timeStamp.Substring(2, 2));
		int min = int.Parse(timeStamp.Substring(4, 2));
		if (zlib) // exclude time stamp in text header for non compressed products
			TimeStamp = new(utcNow.Year, utcNow.Month, day, hour, min, 0);
		reader.BaseStream.Seek(3, SeekOrigin.Current);
		DataType = readString(reader, 3);
		var radarStationId = readString(reader, 3);

		string stationMatch = string.Empty;
		foreach (var entry in radarIdToLocation.Keys)
			if (radarStationId == entry[1..])
			{
				stationMatch = entry;
				break;
			}
		var locationData = radarIdToLocation[stationMatch];
		City = locationData.City;
		State = locationData.StateOrCountryFull;
		StateAbbreviated = locationData.Abbreviation;
		RadarStationId = stationMatch;

		reader.BaseStream.Seek(3, SeekOrigin.Current);
	}
}
