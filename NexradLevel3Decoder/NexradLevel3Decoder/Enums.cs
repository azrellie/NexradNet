namespace Azrellie.Meteorology.NexradNet.Level3;

public struct Enums
{
	public enum MessageCode
	{
		LegacyBaseReflectivity1 = 16,
		LegacyBaseReflectivity2 = 17,
		LegacyBaseReflectivity3 = 18,
		LegacyBaseReflectivity4 = 19,
		LegacyBaseReflectivityLongRange = 20,
		LegacyBaseReflectivity6 = 21,
		LegacyBaseVelocity1 = 22,
		LegacyBaseVelocity2 = 23,
		LegacyBaseVelocity3 = 24,
		LegacyBaseVelocity4 = 25,
		LegacyBaseVelocity5 = 26,
		LegacyBaseVelocity6 = 27,
		LegacyBaseSpectrumWidth1 = 28,
		LegacyBaseSpectrumWidth2 = 29,
		LegacyBaseSpectrumWidth3 = 30,
		StormTotalPrecipitation = 31,
		DigitalHybridScanReflectivity = 32,
		HybridScanReflectivity = 33,
		CompositeReflectivity1 = 35,
		CompositeReflectivity2 = 36,
		CompositeReflectivity3 = 37,
		CompositeReflectivity4 = 38,
		EchoTops = 41,
		VelocityAzimuthDisplayWindProfile = 48,
		StormRelativeVelocity1 = 55,
		StormRelativeVelocity2 = 56,
		VerticallyIntegratedLiquid = 57,
		StormTrackingInformation = 58,
		HailIndex = 59,
		TornadoVortexSignature = 61,
		StormStructure = 62,
		LayerCompositeReflectivity = 63,
		MidlayerCompositeReflectivity = 66,
		LowLayerCompositeReflectivity = 67,
		FreeTextMessage = 75,
		SurfaceRainfallAccumulation1Hour = 78,
		SurfaceRainfallAccumulation3Hours = 79,
		StormTotalRainfallAccumulation = 80,
		DigitalPrecipitationArray = 81,
		SupplementalPrecipitationData = 82,
		HighLayerCompositeReflectivity = 90,
		DigitalBaseReflectivity = 94,
		BaseVelocityDataArray = 99,
		DigitalVerticallyIntegratedLiquid = 134,
		EnhancedEchoTops = 135,
		UserSelectableLayerCompositeReflectivity = 137,
		DigitalStormTotalPrecipitation = 138,
		MesocycloneDetection = 141,
		RadarStatusLog = 152,
		SuperResolutionDigitalBaseReflectivity = 153,
		SuperResolutionDigitalBaseVelocity = 154,
		DifferentialReflectivity = 158,
		DigitalDifferentialReflectivity = 159,
		CorrelationCoefficient = 160,
		DigitalCorrelationCoefficient = 161,
		SpecificDifferentialPhase = 162,
		DigitalSpecificDifferentialPhase = 163,
		HydrometeorClassification = 164,
		DigitalHydrometeorClassification = 165,
		MeltingLayer = 166,
		OneHourAccumulation = 169,
		DigitalAccumulationArray = 170,
		StormTotalAccumulation = 171,
		DigitalStormTotalAccumulation = 172,
		DigitalUserSelectableTotalAccumulation = 173,
		DigitalOneHourDifferenceAccumulation = 174,
		DigitalStormTotalDifferenceAccumulation = 175,
		DigitalInstantaneousPrecipitationRate = 176,
		HybridHydrometeorClassification = 177,
		BaseReflectivityShortRange = 180,
		BaseVelocityShortRange = 182,
		BaseReflectivityLongRange = 186,
		RainRateClassification = 197
	}

	public enum OperationalMode
	{
		Maintenance = 0,
		CleanAir = 1,
		PrecipitaitonSevereWeather = 2
	}

	public enum OperationalMode2
	{
		Test = 1,
		CleanAir = 2,
		PrecipitaitonSevereWeather = 3
	}

	/// <summary>
	/// The volume coverage pattern that was used by the radar at the time of product generation.
	/// </summary>
	/// <remarks>
	/// VCP definitions (does not include retired):
	/// <para>CloseSevereWeather:</para>
	/// <para>VCP 12 (CloseSevereWeather) is used for severe weather, including tornadoes that are closer to the radar (within 85 miles for storms traveling up 55 mph, but shorter distances for faster moving precipitation)</para>
	/// <para>FarSevereWeather:</para>
	/// <para>VCP 212 (FarSevereWeather) is used for severe weather, including tornadoes that are over 70 miles away from the radar, or widespread severe convection.</para>
	/// <para>TropicalWeather:</para>
	/// <para>VCP 112 (TropicalWeather) is used for tropical systems and strong non severe wind shear events.</para>
	/// <para>GeneralPurpose:</para>
	/// <para>VCP 215 (GeneralPurpose) is used for general precipitation, including tropical systems capable of producing tornadoes.</para>
	/// <para>LongPulseWintryClearAir:</para>
	/// <para>VCP 31 (LongPulseWintryClearAir) is a long pulse mode used for maximum sensitivity. Used for detecting light snow or subtle boundaries. Prone to detecting ground clutter, and may be prone to detecting virga precipitation.</para>
	/// <para>ShortPulseLightPrecipClearAir:</para>
	/// <para>VCP 32 (ShortPulseLightPrecipClearAir) is a short pulse mode used for clear air or isolated rain and/or wintry precipitation. Ideal to use when no precipitation is in radar range, to reduce wear on antenna and mechanical components.</para>
	/// <para>ShortPulseNonConvectiveClearAir:</para>
	/// <para>VCP 35 (ShortPulseNonConvectiveClearAir) is a short pulse mode used for scattered to widespread light to moderate precipitation from non convective systems, especially nimbostratus. Almost never used for convection, except for pop up thundershowers produced by cumulus congestus clouds 30+ miles away from the radar.</para>
	/// </remarks>
	public enum VolumeCoveragePattern
	{
		// modern VCPs
		/// <summary>
		/// Fast updates for close severe weather.
		/// </summary>
		CloseSevereWeather = 12,

		/// <summary>
		/// Same as VCP 12 but with SAILS, meaning better low-level updates.
		/// </summary>
		FarSevereWeather = 212,

		/// <summary>
		/// Wide-area tropical or stratiform events. Used for tropical cyclones and retains all velocity data.
		/// </summary>
		TropicalWeather = 121,

		/// <summary>
		/// General purpose VCP, used for general precipitation.
		/// </summary>
		GeneralPurpose = 215,

		/// <summary>
		/// Modern clear-air surveillance. Used when no severe weather or precipitation is present (replaces VCP 31 and 32).
		/// </summary>
		ShortPulseNonConvectiveCleanAir = 35,

		// classic VCPs (retired or rarely used)
		/// <summary>
		/// Legacy clear-air mode with a long pulse.
		/// </summary>
		LongPulseWintryCleanAir = 31,

		/// <summary>
		/// Legacy clear-air mode with a short pulse.
		/// </summary>
		ShortPulseLightPrecipCleanAir = 32,

		/// <summary>
		/// High-resolution severe weather VCP, replaced by VCP 12.
		/// </summary>
		ClassicSevereWeather = 11,

		/// <summary>
		/// Used for tropical cyclones and widespread precipitation, replaced by VCP 121.
		/// </summary>
		ClassicWidespreadRain = 21,

		/// <summary>
		/// VCP 11 with SAILS enhancements.
		/// </summary>
		ClassicSevereWeatherSAILS = 211,

		/// <summary>
		/// VCP 21 with SAILS enhancements.
		/// </summary>
		ClassicWidespreadRainSAILS = 221
	}
}