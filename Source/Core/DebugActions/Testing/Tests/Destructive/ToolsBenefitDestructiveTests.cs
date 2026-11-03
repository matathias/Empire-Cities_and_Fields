using System.Collections.Generic;
using System.Linq;

namespace FactionColonies.UrbanRural
{
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
        private static bool TryReadMultiplier(WorldSettlementFC rural, out double multiplier)
        {
            multiplier = 1.0;
            WorldObjectComp_ToolsBenefit comp = rural.GetComponent<WorldObjectComp_ToolsBenefit>();
            ResourceFC anyResource = rural.Resources?.FirstOrDefault();
            if (comp is null || anyResource is null) return false;
            multiplier = comp.GetResourceMultiplierModifier(anyResource);
            return true;
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
            if (rural is null) TestAssert.Skip("No rural settlement and could not spawn one");

            var snap = URTestHelper.SnapshotSettings();
            try
            {
                FCURSettings.toolsRange = 0; // nothing is in range -> every rural falls back to 1.0
                WorldObjectComp_ToolsBenefit.RecalculateAllCities(f);

                if (!TryReadMultiplier(rural, out double mult))
                    TestAssert.Skip("Rural has no ToolsBenefit comp / resources");
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
            if (rural is null) TestAssert.Skip("No rural settlement and could not spawn one");

            WorldSettlementFC spawnedCity = null;
            if (URDestructiveTestUtil.FirstCity(f) is null)
            {
                spawnedCity = URDestructiveTestUtil.GetOrCreateCity(f, out bool _);
                if (spawnedCity is null)
                {
                    if (ruralCreated) DestructiveTestUtil.SafeRemoveSettlement(rural);
                    TestAssert.Skip("No city and could not spawn one");
                }
            }

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
                if (!seededAny) TestAssert.Skip("No city could hold Tools");

                WorldObjectComp_ToolsBenefit.RecalculateAllCities(f);

                if (!TryReadMultiplier(rural, out double mult))
                    TestAssert.Skip("Rural has no ToolsBenefit comp / resources");
                TestAssert.GreaterThan(mult, 1.0, "A rural near a well-supplied city should get a Tools bonus");
                TestAssert.LessThanOrEqual(mult, 1.0 + FCURSettings.toolsProductionBonus + 0.0001,
                    "Bonus must not exceed the full production-bonus ceiling");
            }
            finally
            {
                URTestHelper.RestoreSettings(snap);
                if (spawnedCity != null) DestructiveTestUtil.SafeRemoveSettlement(spawnedCity);
                if (ruralCreated) DestructiveTestUtil.SafeRemoveSettlement(rural);
                WorldObjectComp_ToolsBenefit.RecalculateAllCities(f); // restore the real multipliers
            }

            DestructiveTestUtil.AssertEmpireInvariants(f, "Rural_NearSuppliedCities");
        }
    }
}
