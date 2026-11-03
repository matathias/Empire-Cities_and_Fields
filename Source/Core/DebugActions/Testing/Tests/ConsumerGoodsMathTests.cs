namespace FactionColonies.UrbanRural
{
    /// <summary>
    /// Pure-math coverage for <see cref="ConsumerGoodsMath"/> (the Consumer-Goods event buffer
    /// arithmetic). No game state required.
    /// </summary>
    public static class ConsumerGoodsMathTests
    {
        [EmpireTest("CF.ConsumerGoods")]
        public static void Required_IsMagnitudeTimesRateTimesWorkerFactor()
        {
            // 10 magnitude, 2.0 rate, 1.0 worker factor -> 20
            TestAssert.AreEqual(20.0, ConsumerGoodsMath.Required(10.0, 2.0, 1.0));
            // Half the workers -> half the requirement
            TestAssert.AreEqual(10.0, ConsumerGoodsMath.Required(10.0, 2.0, 0.5));
        }

        [EmpireTest("CF.ConsumerGoods")]
        public static void Required_ZeroWorkerFactor_IsZero()
        {
            TestAssert.AreEqual(0.0, ConsumerGoodsMath.Required(10.0, 2.0, 0.0));
        }

        [EmpireTest("CF.ConsumerGoods")]
        public static void Consumed_IsMinOfRequiredAndAvailable()
        {
            TestAssert.AreEqual(5.0, ConsumerGoodsMath.Consumed(20.0, 5.0));
            TestAssert.AreEqual(5.0, ConsumerGoodsMath.Consumed(5.0, 20.0));
        }

        [EmpireTest("CF.ConsumerGoods")]
        public static void Mitigation_FullCoverage_IsMaxMitigation()
        {
            // consumed == required -> full mitigation cap
            TestAssert.AreEqual(0.5, ConsumerGoodsMath.Mitigation(20.0, 20.0, 0.5));
        }

        [EmpireTest("CF.ConsumerGoods")]
        public static void Mitigation_PartialCoverage_ScalesByRatio()
        {
            // Covered half the requirement -> half the cap
            TestAssert.AreEqual(0.25, ConsumerGoodsMath.Mitigation(10.0, 20.0, 0.5));
        }

        [EmpireTest("CF.ConsumerGoods")]
        public static void Mitigation_ZeroRequired_IsZeroNotNaN()
        {
            double m = ConsumerGoodsMath.Mitigation(0.0, 0.0, 0.5);
            TestAssert.IsFalse(double.IsNaN(m), "Zero requirement must not divide-by-zero into NaN");
            TestAssert.AreEqual(0.0, m);
        }
    }
}
