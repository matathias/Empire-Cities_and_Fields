namespace FactionColonies.UrbanRural;

/// <summary>
/// Destructive-tier coverage for the Tools-proximity coordinator running over the LIVE faction.
/// The numeric distribution math is unit-tested in <see cref="ToolsDistributionMathTests"/>; these
/// exercise <see cref="WorldObjectComp_ToolsBenefit.RecalculateAllCities"/> end-to-end for crash /
/// invariant safety and the zero- and positive-coverage effects on a real rural's cached multiplier.
/// Mutates live stockpiles; not cleaned up.
/// </summary>
public static class ToolsBenefitDestructiveTests
{
    // GetResourceMultiplierModifier ignores the specific resource, so any of the rural's resources works.
    // Every rural def carries the ToolsBenefit comp and produces resources, so a missing one is a failure.
    private static double ReadMultiplier(WorldSettlementFC rural)
    {
        WorldObjectComp_ToolsBenefit comp = TestAssert.NotNull(rural.GetComponent<WorldObjectComp_ToolsBenefit>(),
            $"Rural '{rural.Name}' has no ToolsBenefit comp");
        ResourceFC anyResource = TestAssert.NotNull(rural.Resources?.FirstOrDefault(),
            $"Rural '{rural.Name}' has no resources");
        return comp.GetResourceMultiplierModifier(anyResource);
    }

    [EmpireDestructiveTest("CF.Destructive.Tools")]
    public static void RecalculateAllCities_RunsCleanly_AndHoldsInvariants()
    {
        FactionFC f = DestructiveTestUtil.RequireFaction();
        TestAssert.DoesNotThrow(() => WorldObjectComp_ToolsBenefit.RecalculateAllCities(f),
            "RecalculateAllCities threw on the live faction");
        DestructiveTestUtil.AssertEmpireInvariants(f, "RecalculateAllCities");
    }

    [EmpireDestructiveTest("CF.Destructive.Tools")]
    public static void Rural_WithNoInRangeCity_HasNeutralMultiplier()
    {
        FactionFC f = DestructiveTestUtil.RequireFaction();
        WorldSettlementFC rural = URDestructiveTestUtil.GetOrCreateRural(f, out bool ruralCreated);

        var snap = URTestHelper.SnapshotSettings();
        try
        {
            FCURSettings.toolsRange = 0; // nothing is in range -> every rural falls back to 1.0
            WorldObjectComp_ToolsBenefit.RecalculateAllCities(f);

            double mult = ReadMultiplier(rural);
            TestAssert.AreEqual(1.0, mult, 0.0001, "Out-of-range rural should have a neutral (1.0) multiplier");
        }
        finally
        {
            URTestHelper.RestoreSettings(snap);
            if (ruralCreated) DestructiveTestUtil.SafeRemoveSettlement(rural);
            WorldObjectComp_ToolsBenefit.RecalculateAllCities(f); // restore the real multipliers
        }
    }

    [EmpireDestructiveTest("CF.Destructive.Tools")]
    public static void Rural_NearSuppliedCities_GetsToolsBonus()
    {
        FactionFC f = DestructiveTestUtil.RequireFaction();
        WorldSettlementFC rural = URDestructiveTestUtil.GetOrCreateRural(f, out bool ruralCreated);

        WorldSettlementFC firstCity = URDestructiveTestUtil.GetOrCreateCity(f, out bool cityCreated);
        WorldSettlementFC spawnedCity = cityCreated ? firstCity : null;

        List<WorldSettlementFC> cities = f.settlements
            .Where(s => s.settlementDef == CFSettlementDefOf.WorldSettlementDef_City).ToList();

        var snap = URTestHelper.SnapshotSettings();
        try
        {
            // Huge range so the rural is in range of some city; seed EVERY city so whichever one it
            // is assigned to is supplied -> the rural must end up with a positive Tools bonus.
            FCURSettings.toolsRange = 99999;
            FCURSettings.toolsProductionBonus = 0.15f;
            FCURSettings.toolsCostPerRural = 0.2f;

            bool seededAny = false;
            foreach (WorldSettlementFC city in cities)
                seededAny |= URDestructiveTestUtil.FillResource(city, CFResourceDefOf.RTD_Tools) > 0;
            TestAssert.IsTrue(seededAny, "No city could hold Tools (missing SupplyChain stockpile or Tools cap)");

            WorldObjectComp_ToolsBenefit.RecalculateAllCities(f);

            double mult = ReadMultiplier(rural);
            TestAssert.GreaterThan(mult, 1.0, "A rural near a well-supplied city should get a Tools bonus");
            TestAssert.LessThanOrEqual(mult, 1.0 + FCURSettings.toolsProductionBonus + 0.0001,
                "Bonus must not exceed the full production-bonus ceiling");
        }
        finally
        {
            URTestHelper.RestoreSettings(snap);
            if (spawnedCity is not null) DestructiveTestUtil.SafeRemoveSettlement(spawnedCity);
            if (ruralCreated) DestructiveTestUtil.SafeRemoveSettlement(rural);
            WorldObjectComp_ToolsBenefit.RecalculateAllCities(f); // restore the real multipliers
        }

        DestructiveTestUtil.AssertEmpireInvariants(f, "Rural_NearSuppliedCities");
    }

    // Total Tools across the distinct stockpiles the cities read. In SupplyChain Simple mode every
    // city reads the same shared pile, which is counted once.
    private static double TotalCityTools(List<WorldSettlementFC> cities)
    {
        HashSet<SupplyChain.IStockpile> seen = [];
        double total = 0.0;
        foreach (WorldSettlementFC city in cities)
        {
            SupplyChain.IStockpile sp = URDestructiveTestUtil.StockpileOf(city);
            if (sp is not null && seen.Add(sp))
                total += sp.GetAmount(CFResourceDefOf.RTD_Tools);
        }
        return total;
    }

    // The billed city's Tools come out of the stockpile it reads (the shared faction pile in
    // SupplyChain Simple mode), so supplying a rural always costs Tools somewhere.
    [EmpireDestructiveTest("CF.Destructive.Tools")]
    public static void SuppliedRural_DrawsToolsFromCityStockpile()
    {
        FactionFC f = DestructiveTestUtil.RequireFaction();
        WorldSettlementFC rural = URDestructiveTestUtil.GetOrCreateRural(f, out bool ruralCreated);
        WorldSettlementFC firstCity = URDestructiveTestUtil.GetOrCreateCity(f, out bool cityCreated);
        WorldSettlementFC spawnedCity = cityCreated ? firstCity : null;

        List<WorldSettlementFC> cities = f.settlements
            .Where(s => s.settlementDef == CFSettlementDefOf.WorldSettlementDef_City).ToList();

        var snap = URTestHelper.SnapshotSettings();
        try
        {
            FCURSettings.toolsRange = 99999;
            FCURSettings.toolsProductionBonus = 0.15f;
            FCURSettings.toolsCostPerRural = 0.2f;

            bool seededAny = false;
            foreach (WorldSettlementFC city in cities)
                seededAny |= URDestructiveTestUtil.FillResource(city, CFResourceDefOf.RTD_Tools) > 0;
            TestAssert.IsTrue(seededAny, "No city could hold Tools (no SupplyChain stockpile or no Tools cap)");

            double before = TotalCityTools(cities);
            WorldObjectComp_ToolsBenefit.RecalculateAllCities(f);
            double after = TotalCityTools(cities);

            TestAssert.GreaterThan(ReadMultiplier(rural), 1.0, "The rural should be supplied");
            TestAssert.LessThan(after, before, "Supplying a rural should draw Tools from a city's stockpile");
        }
        finally
        {
            URTestHelper.RestoreSettings(snap);
            if (spawnedCity is not null) DestructiveTestUtil.SafeRemoveSettlement(spawnedCity);
            if (ruralCreated) DestructiveTestUtil.SafeRemoveSettlement(rural);
            WorldObjectComp_ToolsBenefit.RecalculateAllCities(f);
        }

        DestructiveTestUtil.AssertEmpireInvariants(f, "SuppliedRural_DrawsToolsFromCityStockpile");
    }
}
