namespace FactionColonies.UrbanRural;

/// <summary>
/// Def-wiring integrity checks (base-mod Tier-1 style): the settlement/resource DefOfs resolve,
/// the rural inheritance chain is intact, seasonal multiplier defs are sane, and the Refinery's
/// x2 creation-cost override is wired through. Pure def inspection; no faction/world required.
/// </summary>
public static class DefIntegrityTests
{
    private static WorldSettlementDef[] RuralSpecializations()
    {
        return new[]
        {
            CFSettlementDefOf.WorldSettlementDef_Farm,
            CFSettlementDefOf.WorldSettlementDef_Mine,
            CFSettlementDefOf.WorldSettlementDef_LumberCamp,
            CFSettlementDefOf.WorldSettlementDef_Ranch,
            CFSettlementDefOf.WorldSettlementDef_Refinery,
            CFSettlementDefOf.WorldSettlementDef_Herbalist,
            CFSettlementDefOf.WorldSettlementDef_PowerPlant,
        };
    }

    private static bool InheritsRural(WorldSettlementDef def)
    {
        WorldSettlementDef rural = CFSettlementDefOf.WorldSettlementDef_Rural;
        while (def is not null)
        {
            if (def == rural) return true;
            def = def.baseSettlementType;
        }
        return false;
    }

    [EmpireTest("CF.Defs")]
    public static void SettlementDefOfs_AllResolve()
    {
        TestAssert.IsNotNull(CFSettlementDefOf.WorldSettlementDef_City, "City def missing");
        TestAssert.IsNotNull(CFSettlementDefOf.WorldSettlementDef_Rural, "Rural base def missing");
        foreach (WorldSettlementDef def in RuralSpecializations())
            TestAssert.IsNotNull(def, "A rural specialization def failed to resolve");
    }

    [EmpireTest("CF.Defs")]
    public static void ResourceDefOfs_AllResolve()
    {
        TestAssert.IsNotNull(CFResourceDefOf.RTD_Tools, "RTD_Tools missing");
        TestAssert.IsNotNull(CFResourceDefOf.RTD_ConsumerGoods, "RTD_ConsumerGoods missing");
    }

    [EmpireTest("CF.Defs")]
    public static void RuralSpecializations_InheritRuralBase()
    {
        foreach (WorldSettlementDef def in RuralSpecializations())
        {
            if (def is null) TestAssert.Skip("A rural specialization def failed to resolve");
            TestAssert.IsTrue(InheritsRural(def),
                $"{def.defName} should chain to WorldSettlementDef_Rural via baseSettlementType");
        }
    }

    [EmpireTest("CF.Defs")]
    public static void City_DoesNotInheritRural()
    {
        WorldSettlementDef city = CFSettlementDefOf.WorldSettlementDef_City;
        if (city is null) TestAssert.Skip("City def failed to resolve");
        TestAssert.IsFalse(InheritsRural(city), "City must not be classified as a rural settlement");
    }

    [EmpireTest("CF.Defs")]
    public static void SeasonalMultiplierDefs_AreSane()
    {
        List<SeasonalMultiplierDef> defs = DefDatabase<SeasonalMultiplierDef>.AllDefsListForReading;
        TestAssert.IsNotEmpty(defs, "No SeasonalMultiplierDefs loaded");
        foreach (SeasonalMultiplierDef def in defs)
        {
            TestAssert.IsNotNull(def.resource, $"{def.defName}: resource is null");
            foreach (double m in new[] { def.inGrowing, def.outOfGrowing, def.yearRound, def.noGrowing })
            {
                TestAssert.IsFalse(double.IsNaN(m), $"{def.defName}: multiplier is NaN");
                TestAssert.IsFalse(double.IsInfinity(m), $"{def.defName}: multiplier is infinite");
                TestAssert.GreaterThan(m, 0.0, $"{def.defName}: multiplier must be positive");
            }
        }
    }

    [EmpireTest("CF.Defs")]
    public static void Refinery_CreationCost_IsDoubleAPlainRural()
    {
        WorldSettlementDef refinery = CFSettlementDefOf.WorldSettlementDef_Refinery;
        WorldSettlementDef farm = CFSettlementDefOf.WorldSettlementDef_Farm;
        if (refinery is null || farm is null) TestAssert.Skip("Refinery/Farm def failed to resolve");

        SettlementTypeExtension refineryExt = refinery.GetSettlementTypeExtension();
        SettlementTypeExtension farmExt = farm.GetSettlementTypeExtension();
        if (refineryExt is null || farmExt is null) TestAssert.Skip("Settlement type extension missing");

        // Refinery's override doubles whatever the base rural cost resolves to under identical
        // faction/window state, so the x2 relationship holds regardless of any distance surcharge.
        TestAssert.AreEqual(2 * farmExt.GetCreationCost(), refineryExt.GetCreationCost(),
            "Refinery creation cost should be exactly 2x a plain rural's");
    }

    [EmpireTest("CF.Defs")]
    public static void RuralSpecializations_UseRuralExtension()
    {
        foreach (WorldSettlementDef def in RuralSpecializations())
        {
            if (def is null) TestAssert.Skip("A rural specialization def failed to resolve");
            SettlementTypeExtension ext = def.GetSettlementTypeExtension();
            TestAssert.IsNotNull(ext, $"{def.defName}: no settlement type extension");
            TestAssert.IsTrue(ext is SettlementTypeExtension_Rural,
                $"{def.defName}: extension should derive from SettlementTypeExtension_Rural");
        }
    }
}
