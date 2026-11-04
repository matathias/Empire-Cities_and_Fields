namespace FactionColonies.UrbanRural;

/// <summary>
/// Shared helpers for the Cities &amp; Fields tests: a snapshot/restore for the mutable
/// <see cref="FCURSettings"/> statics (so non-destructive math tests can pin a known value and
/// still leave settings as they found them), plus small throwaway fixture builders for the pure
/// resource / seasonal tests.
/// </summary>
public static class URTestHelper
{
    /// <summary>Captured values of the settings statics that the tests may overwrite.</summary>
    public struct SettingsSnapshot
    {
        public float cgConversionRate;
        public float cgMaxMitigation;
        public float cgBaseWorkers;
        public int toolsRange;
        public float toolsCostPerRural;
        public float toolsProductionBonus;
    }

    public static SettingsSnapshot SnapshotSettings()
    {
        SettingsSnapshot s;
        s.cgConversionRate = FCURSettings.cgConversionRate;
        s.cgMaxMitigation = FCURSettings.cgMaxMitigation;
        s.cgBaseWorkers = FCURSettings.cgBaseWorkers;
        s.toolsRange = FCURSettings.toolsRange;
        s.toolsCostPerRural = FCURSettings.toolsCostPerRural;
        s.toolsProductionBonus = FCURSettings.toolsProductionBonus;
        return s;
    }

    public static void RestoreSettings(SettingsSnapshot s)
    {
        FCURSettings.cgConversionRate = s.cgConversionRate;
        FCURSettings.cgMaxMitigation = s.cgMaxMitigation;
        FCURSettings.cgBaseWorkers = s.cgBaseWorkers;
        FCURSettings.toolsRange = s.toolsRange;
        FCURSettings.toolsCostPerRural = s.toolsCostPerRural;
        FCURSettings.toolsProductionBonus = s.toolsProductionBonus;
    }

    /// <summary>
    /// A throwaway <see cref="ResourceTypeDef"/> usable as a lookup key in pure tests. Reference
    /// identity is all the tests rely on, so it never needs to be registered in the DefDatabase.
    /// </summary>
    public static ResourceTypeDef MakeResourceType(string defName)
    {
        return new ResourceTypeDef { defName = defName, label = defName };
    }

    /// <summary>Builds a throwaway <see cref="SeasonalMultiplierEntry"/> for multiplier-selection tests.</summary>
    public static SeasonalMultiplierEntry MakeSeasonalEntry(double inGrowing, double outOfGrowing, double yearRound, double noGrowing)
    {
        return new SeasonalMultiplierEntry
        {
            inGrowing = inGrowing,
            outOfGrowing = outOfGrowing,
            yearRound = yearRound,
            noGrowing = noGrowing
        };
    }
}
