namespace FactionColonies.UrbanRural
{
    /// <summary>
    /// Coverage for the rural building-slot step function in <see cref="SettlementTypeExtension_Rural"/>.
    /// The override is pure (level in, slot count out) so it needs no game state; maxCount is
    /// unused by the override and passed as a fixed placeholder.
    /// </summary>
    public static class RuralSlotProgressionTests
    {
        private const int MaxCount = 8;

        private static SettlementTypeExtension_Rural Ext()
        {
            return new SettlementTypeExtension_Rural();
        }

        [EmpireTest("CF.Slots")]
        public static void GetBuildingSlots_StepFunction()
        {
            var ext = Ext();
            TestAssert.AreEqual(0, ext.GetBuildingSlots(0, MaxCount));
            TestAssert.AreEqual(0, ext.GetBuildingSlots(1, MaxCount));
            TestAssert.AreEqual(1, ext.GetBuildingSlots(2, MaxCount));
            TestAssert.AreEqual(1, ext.GetBuildingSlots(3, MaxCount));
            TestAssert.AreEqual(4, ext.GetBuildingSlots(4, MaxCount));
            TestAssert.AreEqual(4, ext.GetBuildingSlots(5, MaxCount));
            TestAssert.AreEqual(4, ext.GetBuildingSlots(6, MaxCount));
            TestAssert.AreEqual(4, ext.GetBuildingSlots(7, MaxCount));
            TestAssert.AreEqual(8, ext.GetBuildingSlots(8, MaxCount));
            TestAssert.AreEqual(8, ext.GetBuildingSlots(9, MaxCount));
            TestAssert.AreEqual(8, ext.GetBuildingSlots(20, MaxCount));
        }

        [EmpireTest("CF.Slots")]
        public static void GetRequiredLevelForSlot_MatchesSlotProgression()
        {
            var ext = Ext();
            TestAssert.AreEqual(2, ext.GetRequiredLevelForSlot(0, MaxCount));
            TestAssert.AreEqual(4, ext.GetRequiredLevelForSlot(1, MaxCount));
            TestAssert.AreEqual(4, ext.GetRequiredLevelForSlot(2, MaxCount));
            TestAssert.AreEqual(4, ext.GetRequiredLevelForSlot(3, MaxCount));
            TestAssert.AreEqual(8, ext.GetRequiredLevelForSlot(4, MaxCount));
            TestAssert.AreEqual(8, ext.GetRequiredLevelForSlot(7, MaxCount));
        }
    }
}
