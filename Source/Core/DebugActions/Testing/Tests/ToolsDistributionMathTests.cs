namespace FactionColonies.UrbanRural
{
    /// <summary>
    /// Pure-math coverage for <see cref="ToolsDistributionMath"/> (the Tools-proximity arithmetic).
    /// No game state required.
    /// </summary>
    public static class ToolsDistributionMathTests
    {
        [EmpireTest("CF.Tools")]
        public static void Coverage_ZeroAvailable_IsZero()
        {
            TestAssert.AreEqual(0.0, ToolsDistributionMath.Coverage(0.0, 10.0));
        }

        [EmpireTest("CF.Tools")]
        public static void Coverage_AtOrAboveDemand_IsOne()
        {
            TestAssert.AreEqual(1.0, ToolsDistributionMath.Coverage(10.0, 10.0));
            TestAssert.AreEqual(1.0, ToolsDistributionMath.Coverage(25.0, 10.0));
        }

        [EmpireTest("CF.Tools")]
        public static void Coverage_Partial_IsRatio()
        {
            TestAssert.AreEqual(0.5, ToolsDistributionMath.Coverage(5.0, 10.0));
            TestAssert.AreEqual(0.25, ToolsDistributionMath.Coverage(2.5, 10.0));
        }

        [EmpireTest("CF.Tools")]
        public static void Coverage_ZeroDemand_IsZero()
        {
            TestAssert.AreEqual(0.0, ToolsDistributionMath.Coverage(5.0, 0.0));
        }

        [EmpireTest("CF.Tools")]
        public static void ToConsume_IsMinOfAvailableAndDemand()
        {
            TestAssert.AreEqual(5.0, ToolsDistributionMath.ToConsume(5.0, 10.0));
            TestAssert.AreEqual(5.0, ToolsDistributionMath.ToConsume(10.0, 5.0));
        }

        [EmpireTest("CF.Tools")]
        public static void Multiplier_ZeroCoverage_IsOne()
        {
            TestAssert.AreEqual(1.0, ToolsDistributionMath.Multiplier(0.0, 0.15));
        }

        [EmpireTest("CF.Tools")]
        public static void Multiplier_FullCoverage_IsOnePlusBonus()
        {
            TestAssert.AreEqual(1.15, ToolsDistributionMath.Multiplier(1.0, 0.15));
        }

        [EmpireTest("CF.Tools")]
        public static void Multiplier_HalfCoverage_IsHalfBonus()
        {
            TestAssert.AreEqual(1.075, ToolsDistributionMath.Multiplier(0.5, 0.15));
        }

        [EmpireTest("CF.Tools")]
        public static void Multiplier_UsesConfiguredBonus()
        {
            var snap = URTestHelper.SnapshotSettings();
            try
            {
                FCURSettings.toolsProductionBonus = 0.30f;
                TestAssert.AreEqual(1.30, ToolsDistributionMath.Multiplier(1.0, FCURSettings.toolsProductionBonus));
            }
            finally
            {
                URTestHelper.RestoreSettings(snap);
            }
        }
    }
}
