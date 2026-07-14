using Verse;

namespace FactionColonies.UrbanRural
{
    /// <summary>
    /// Coverage for the Consumer-Goods mitigation ledger on <see cref="WorldComponent_UrbanRural"/>
    /// (Set / TryGet / Clear and settlement+source key isolation). Uses a throwaway component
    /// instance so nothing on the live world is mutated; skips if no world is loaded.
    /// </summary>
    public static class MitigationLedgerTests
    {
        private static WorldComponent_UrbanRural NewLedger()
        {
            if (Find.World is null) TestAssert.Skip("No world loaded");
            return new WorldComponent_UrbanRural(Find.World);
        }

        [EmpireTest("CF.Ledger")]
        public static void SetThenTryGet_ReturnsStoredValue()
        {
            var ledger = NewLedger();
            ledger.SetEventMitigation(1, "event_test", 0.4);
            TestAssert.IsTrue(ledger.TryGetEventMitigation(1, "event_test", out double v),
                "TryGet should find the value that was just set");
            TestAssert.AreEqual(0.4, v);
        }

        [EmpireTest("CF.Ledger")]
        public static void TryGet_UnknownKey_ReturnsFalse()
        {
            var ledger = NewLedger();
            TestAssert.IsFalse(ledger.TryGetEventMitigation(999, "event_absent", out _),
                "TryGet on an unset key should return false");
        }

        [EmpireTest("CF.Ledger")]
        public static void Clear_RemovesEntry()
        {
            var ledger = NewLedger();
            ledger.SetEventMitigation(2, "event_x", 0.3);
            ledger.ClearEventMitigation(2, "event_x");
            TestAssert.IsFalse(ledger.TryGetEventMitigation(2, "event_x", out _),
                "Cleared entry should no longer be found");
        }

        [EmpireTest("CF.Ledger")]
        public static void Keys_IsolateBySettlementAndSource()
        {
            var ledger = NewLedger();
            ledger.SetEventMitigation(1, "event_a", 0.3);
            ledger.SetEventMitigation(2, "event_a", 0.7); // same source, different settlement
            ledger.SetEventMitigation(1, "event_b", 0.9); // same settlement, different source

            ledger.TryGetEventMitigation(1, "event_a", out double a);
            ledger.TryGetEventMitigation(2, "event_a", out double b);
            ledger.TryGetEventMitigation(1, "event_b", out double c);

            TestAssert.AreEqual(0.3, a);
            TestAssert.AreEqual(0.7, b);
            TestAssert.AreEqual(0.9, c);
        }
    }
}
