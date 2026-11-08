namespace FactionColonies.UrbanRural;

/// <summary>
/// <see cref="SeasonalMultipliers"/> selection per season state, and the shipped
/// <see cref="SeasonalMultiplierDef"/>s loading their values from the nested
/// <c>&lt;multipliers&gt;</c> element. No world required.
/// </summary>
public static class SeasonalMultipliersTests
{
    private static SeasonalMultipliers Sentinels()
    {
        return new SeasonalMultipliers { inGrowing = 1.3, outOfGrowing = 0.6, yearRound = 1.1, noGrowing = 0.2 };
    }

    [EmpireTest("CF.Seasonal", GameState = TestGameState.Any)]
    public static void For_PicksFieldPerState()
    {
        SeasonalMultipliers m = Sentinels();
        TestAssert.AreEqual(1.3, m.For(SeasonalMath.SeasonState.InGrowing), 0.0001, "InGrowing");
        TestAssert.AreEqual(0.6, m.For(SeasonalMath.SeasonState.OutOfGrowing), 0.0001, "OutOfGrowing");
        TestAssert.AreEqual(1.1, m.For(SeasonalMath.SeasonState.YearRound), 0.0001, "YearRound");
        TestAssert.AreEqual(0.2, m.For(SeasonalMath.SeasonState.NoGrowing), 0.0001, "NoGrowing");
    }

    [EmpireTest("CF.Seasonal", GameState = TestGameState.Any)]
    public static void For_UnknownState_IsNeutral()
    {
        TestAssert.AreEqual(1.0, Sentinels().For((SeasonalMath.SeasonState)99), 0.0001,
            "An out-of-range state should give a neutral multiplier");
    }

    [EmpireTest("CF.Seasonal", GameState = TestGameState.Any)]
    public static void Defaults_AreNeutral()
    {
        SeasonalMultipliers m = new();
        foreach (SeasonalMath.SeasonState state in Enum.GetValues(typeof(SeasonalMath.SeasonState)))
            TestAssert.AreEqual(1.0, m.For(state), 0.0001, $"Default multiplier for {state} should be 1.0");
    }

    [EmpireTest("CF.Seasonal", GameState = TestGameState.Any)]
    public static void Entry_SelectsLikeTheSharedType()
    {
        SeasonalMultiplierEntry e = URTestHelper.MakeSeasonalEntry(1.3, 0.6, 1.1, 0.2);
        SeasonalMultipliers m = Sentinels();
        foreach (SeasonalMath.SeasonState state in Enum.GetValues(typeof(SeasonalMath.SeasonState)))
            TestAssert.AreEqual(m.For(state), e.For(state), 0.0001, $"Entry and shared type disagree on {state}");
    }

    [EmpireTest("CF.Seasonal", GameState = TestGameState.Any)]
    public static void NewDef_HasNonNullNeutralMultipliers()
    {
        SeasonalMultiplierDef def = new();
        TestAssert.IsNotNull(def.multipliers, "A def without a <multipliers> element should still have neutral values");
        TestAssert.AreEqual(1.0, def.multipliers.For(SeasonalMath.SeasonState.InGrowing), 0.0001);
    }

    // The food def's values, as written under <multipliers> in SeasonalMultipliers.xml. If the
    // nested element were not read, every value would stay at the neutral 1.0 default.
    [EmpireTest("CF.Seasonal", GameState = TestGameState.Any)]
    public static void FoodDef_LoadsNestedValues()
    {
        ResourceTypeDef food = TestAssert.NotNull(ResourceTypeDefOf.RTD_Food, "RTD_Food not loaded");
        SeasonalMultiplierDef def = TestAssert.NotNull(
            DefDatabase<SeasonalMultiplierDef>.AllDefsListForReading.FirstOrDefault(d => d.resource == food),
            "No SeasonalMultiplierDef for Food");
        SeasonalMultipliers m = TestAssert.NotNull(def.multipliers, "Food multipliers are null");
        TestAssert.AreEqual(1.2, m.inGrowing, 0.0001, "Food inGrowing");
        TestAssert.AreEqual(0.5, m.outOfGrowing, 0.0001, "Food outOfGrowing");
        TestAssert.AreEqual(1.1, m.yearRound, 0.0001, "Food yearRound");
        TestAssert.AreEqual(0.4, m.noGrowing, 0.0001, "Food noGrowing");
    }

    // Every def this mod ships has a no-growing value below 1.0, so one that reads 1.0 lost its
    // <multipliers> element on load. Defs added by other mods are left out.
    [EmpireTest("CF.Seasonal", GameState = TestGameState.Any)]
    public static void ShippedDefs_LoadedNonNeutralValues()
    {
        ModContentPack ours = TestAssert.NotNull(LoadedModManager.GetMod<UrbanRuralMod>()?.Content,
            "Cities & Fields mod content not found");
        List<SeasonalMultiplierDef> defs = DefDatabase<SeasonalMultiplierDef>.AllDefsListForReading
            .Where(d => d.modContentPack == ours).ToList();
        TestAssert.IsNotEmpty(defs, "No SeasonalMultiplierDefs loaded from this mod");
        foreach (SeasonalMultiplierDef def in defs)
        {
            SeasonalMultipliers m = TestAssert.NotNull(def.multipliers, $"{def.defName}: multipliers is null");
            TestAssert.LessThan(m.noGrowing, 1.0, $"{def.defName}: noGrowing should be below 1.0");
        }
    }
}
