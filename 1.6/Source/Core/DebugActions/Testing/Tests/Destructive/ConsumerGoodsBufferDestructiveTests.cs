using System.Collections.Generic;

namespace FactionColonies.UrbanRural
{
    /// <summary>
    /// Destructive-tier coverage for the Consumer-Goods event buffer Harmony patch running against a
    /// LIVE settlement. The mitigation arithmetic is unit-tested in <see cref="ConsumerGoodsMathTests"/>;
    /// these exercise the full patch flow: an event's negative stat modifiers trigger CG consumption,
    /// a compensating buffer is applied, the mitigation is recorded in the ledger, and removing the
    /// event cleans both up. Mutates live stockpiles and stat modifiers; not cleaned up automatically.
    /// </summary>
    public static class ConsumerGoodsBufferDestructiveTests
    {
        private static List<FCStatModifier> PenaltyMods(double amount)
        {
            return new List<FCStatModifier>
            {
                new FCStatModifier { stat = FCStatDefOf.happinessLostBase, value = amount }
            };
        }

        [EmpireDestructiveTest("CF.Destructive.ConsumerGoods")]
        public static void CgBuffer_SeededCity_MitigatesAndRoundTripsLedger()
        {
            FactionFC f = DestructiveTestUtil.RequireFaction();
            WorldSettlementFC city = URDestructiveTestUtil.GetOrCreateCity(f, out bool cityCreated);
            if (city is null) TestAssert.Skip("No city and could not spawn one");

            // Mitigation scales with worker count; a freshly spawned city has none, so assign a few.
            // Only mutate worker assignment on a settlement we created, never a live player city.
            if (city.GetTotalWorkers() <= 0)
            {
                if (cityCreated) URDestructiveTestUtil.EnsureWorkers(city, 5);
                if (city.GetTotalWorkers() <= 0)
                {
                    if (cityCreated) DestructiveTestUtil.SafeRemoveSettlement(city);
                    TestAssert.Skip("City has no workers (mitigation would be 0)");
                }
            }

            WorldComponent_UrbanRural coord = URDestructiveTestUtil.Coordinator();
            if (coord is null)
            {
                if (cityCreated) DestructiveTestUtil.SafeRemoveSettlement(city);
                TestAssert.Skip("No WorldComponent_UrbanRural");
            }

            var snap = URTestHelper.SnapshotSettings();
            const string src = "event_urtest_cg";
            List<FCStatModifier> mods = PenaltyMods(10.0);
            try
            {
                if (URDestructiveTestUtil.FillResource(city, CFResourceDefOf.RTD_ConsumerGoods) <= 0)
                    TestAssert.Skip("City could not hold Consumer Goods");
                coord.ClearEventMitigation(city.ID, src); // clean slate

                double before = city.GetSettlementStatValue(FCStatDefOf.happinessLostBase);
                city.AddStatModifiers(mods, src, "UR Test Event");
                double after = city.GetSettlementStatValue(FCStatDefOf.happinessLostBase);

                // The event raised the penalty by 10, but the buffer clawed back a positive fraction.
                TestAssert.GreaterThan(after - before, 0.0, "Event should raise happinessLostBase");
                TestAssert.LessThan(after - before, 10.0, "CG buffer should mitigate part of the +10 penalty");

                TestAssert.IsTrue(coord.TryGetEventMitigation(city.ID, src, out double frac),
                    "Buffer should record its mitigation fraction in the ledger");
                TestAssert.GreaterThan(frac, 0.0, "Recorded mitigation should be positive");

                // Removing the event should tear down the buffer and clear the ledger, restoring baseline.
                city.RemoveStatModifiers(mods, src);
                TestAssert.IsFalse(coord.TryGetEventMitigation(city.ID, src, out _),
                    "Removing the event should clear the recorded mitigation");
                double afterRemove = city.GetSettlementStatValue(FCStatDefOf.happinessLostBase);
                TestAssert.AreEqual(before, afterRemove, 0.001,
                    "Removing event + buffer should restore the pre-event baseline");

                DestructiveTestUtil.AssertEmpireInvariants(f, "CgBuffer_SeededCity");
            }
            finally
            {
                // Ensure no residue even if an assertion failed mid-flow.
                city.RemoveStatModifiers(mods, src);
                coord.ClearEventMitigation(city.ID, src);
                URTestHelper.RestoreSettings(snap);
                if (cityCreated) DestructiveTestUtil.SafeRemoveSettlement(city);
            }
        }

        [EmpireDestructiveTest("CF.Destructive.ConsumerGoods")]
        public static void CgBuffer_NoConsumerGoods_NoMitigation()
        {
            FactionFC f = DestructiveTestUtil.RequireFaction();
            WorldSettlementFC s = DestructiveTestUtil.CreateTransientSettlement();
            if (s is null) TestAssert.Skip("No valid tile for a transient settlement");

            WorldComponent_UrbanRural coord = URDestructiveTestUtil.Coordinator();
            const string src = "event_urtest_nocg";
            List<FCStatModifier> mods = PenaltyMods(10.0);
            try
            {
                if (coord != null) coord.ClearEventMitigation(s.ID, src);

                double before = s.GetSettlementStatValue(FCStatDefOf.happinessLostBase);
                // A bare transient settlement has no seeded CG, so the patch must no-op cleanly.
                TestAssert.DoesNotThrow(() => s.AddStatModifiers(mods, src, "UR Test Event"),
                    "CG buffer patch threw on a settlement with no Consumer Goods");
                double after = s.GetSettlementStatValue(FCStatDefOf.happinessLostBase);

                TestAssert.AreEqual(before + 10.0, after, 0.001,
                    "With no Consumer Goods the full penalty applies (no mitigation)");
                if (coord != null)
                    TestAssert.IsFalse(coord.TryGetEventMitigation(s.ID, src, out _),
                        "No Consumer Goods should mean no recorded mitigation");

                s.RemoveStatModifiers(mods, src);
                DestructiveTestUtil.AssertEmpireInvariants(f, "CgBuffer_NoConsumerGoods");
            }
            finally
            {
                DestructiveTestUtil.SafeRemoveSettlement(s);
            }
        }
    }
}
