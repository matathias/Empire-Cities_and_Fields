using System.Text;

namespace FactionColonies.UrbanRural;

/// <summary>
/// The hilliness gate on the shipped rural specializations, checked for every Hilliness value
/// against the rule each type has always had: Farm and Ranch need SmallHills or flatter, Mine
/// needs SmallHills or hillier, and every other rural type accepts any terrain. Reads the
/// loaded defs' extensions, so this also covers the XML hilliness fields. No world required.
/// </summary>
public static class RuralTerrainTests
{
    private static readonly Hilliness[] AllHilliness = (Hilliness[])Enum.GetValues(typeof(Hilliness));

    private static SettlementTypeExtension_Rural RuralExt(WorldSettlementDef def, string label)
    {
        TestAssert.NotNull(def, $"{label} def failed to resolve");
        SettlementTypeExtension ext = TestAssert.NotNull(def.GetSettlementTypeExtension(),
            $"{label} has no settlement type extension");
        return TestAssert.NotNull(ext as SettlementTypeExtension_Rural,
            $"{label}'s extension is not a SettlementTypeExtension_Rural");
    }

    private static void AssertRule(WorldSettlementDef def, string label, Func<Hilliness, bool> expected)
    {
        SettlementTypeExtension_Rural ext = RuralExt(def, label);
        foreach (Hilliness h in AllHilliness)
        {
            TestAssert.AreEqual(expected(h), ext.AllowsHilliness(h),
                $"{label} on {h}: expected {(expected(h) ? "allowed" : "rejected")}");
        }
    }

    [EmpireTest("CF.Terrain", GameState = TestGameState.Any)]
    public static void Farm_AllowsSmallHillsOrFlatter()
    {
        AssertRule(CFSettlementDefOf.WorldSettlementDef_Farm, "Farm", h => h <= Hilliness.SmallHills);
    }

    [EmpireTest("CF.Terrain", GameState = TestGameState.Any)]
    public static void Ranch_AllowsSmallHillsOrFlatter()
    {
        AssertRule(CFSettlementDefOf.WorldSettlementDef_Ranch, "Ranch", h => h <= Hilliness.SmallHills);
    }

    [EmpireTest("CF.Terrain", GameState = TestGameState.Any)]
    public static void Mine_AllowsSmallHillsOrHillier()
    {
        AssertRule(CFSettlementDefOf.WorldSettlementDef_Mine, "Mine", h => h >= Hilliness.SmallHills);
    }

    [EmpireTest("CF.Terrain", GameState = TestGameState.Any)]
    public static void OtherRuralTypes_AllowAnyTerrain()
    {
        AssertRule(CFSettlementDefOf.WorldSettlementDef_LumberCamp, "LumberCamp", _ => true);
        AssertRule(CFSettlementDefOf.WorldSettlementDef_Refinery, "Refinery", _ => true);
        AssertRule(CFSettlementDefOf.WorldSettlementDef_Herbalist, "Herbalist", _ => true);
        AssertRule(CFSettlementDefOf.WorldSettlementDef_PowerPlant, "PowerPlant", _ => true);
    }

    [EmpireTest("CF.Terrain", GameState = TestGameState.Any)]
    public static void DefaultExtension_AllowsAnyTerrain()
    {
        SettlementTypeExtension_Rural ext = new();
        foreach (Hilliness h in AllHilliness)
            TestAssert.IsTrue(ext.AllowsHilliness(h), $"A default rural extension should allow {h}");
    }

    [EmpireTest("CF.Terrain", GameState = TestGameState.Any)]
    public static void TooHilly_AppendsFlatTerrainReason()
    {
        SettlementTypeExtension_Rural farm = RuralExt(CFSettlementDefOf.WorldSettlementDef_Farm, "Farm");
        StringBuilder reason = new();
        TestAssert.IsFalse(farm.AllowsHilliness(Hilliness.Mountainous, reason), "Farm should reject Mountainous");
        TestAssert.AreEqual((string)"UR_RequiresFlatTerrain".Translate(), reason.ToString(),
            "A too-hilly tile should give the flat-terrain reason");
    }

    [EmpireTest("CF.Terrain", GameState = TestGameState.Any)]
    public static void TooFlat_AppendsHillyTerrainReason()
    {
        SettlementTypeExtension_Rural mine = RuralExt(CFSettlementDefOf.WorldSettlementDef_Mine, "Mine");
        StringBuilder reason = new();
        TestAssert.IsFalse(mine.AllowsHilliness(Hilliness.Flat, reason), "Mine should reject Flat");
        TestAssert.AreEqual((string)"UR_RequiresHillyTerrain".Translate(), reason.ToString(),
            "A too-flat tile should give the hilly-terrain reason");
    }

    [EmpireTest("CF.Terrain", GameState = TestGameState.Any)]
    public static void Allowed_AppendsNoReason()
    {
        SettlementTypeExtension_Rural farm = RuralExt(CFSettlementDefOf.WorldSettlementDef_Farm, "Farm");
        StringBuilder reason = new();
        TestAssert.IsTrue(farm.AllowsHilliness(Hilliness.Flat, reason), "Farm should allow Flat");
        TestAssert.AreEqual(0, reason.Length, "An allowed tile should leave the reason empty");
    }
}
