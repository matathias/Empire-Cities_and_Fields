namespace FactionColonies.UrbanRural;

/// <summary>
/// Pure coverage for <see cref="SeasonalMath"/> season-state resolution and multiplier selection.
/// No game state required (growing twelfths are supplied directly).
/// </summary>
public static class SeasonalMathTests
{
    [EmpireTest("CF.Seasonal")]
    public static void ResolveState_NoGrowingMonths_IsNoGrowing()
    {
        TestAssert.IsTrue(
            SeasonalMath.ResolveState(0, Twelfth.First, null) == SeasonalMath.SeasonState.NoGrowing);
    }

    [EmpireTest("CF.Seasonal")]
    public static void ResolveState_TwelveMonths_IsYearRound()
    {
        var all = new List<Twelfth> { Twelfth.First };
        TestAssert.IsTrue(
            SeasonalMath.ResolveState(12, Twelfth.First, all) == SeasonalMath.SeasonState.YearRound);
    }

    [EmpireTest("CF.Seasonal")]
    public static void ResolveState_CurrentInGrowingList_IsInGrowing()
    {
        var growing = new List<Twelfth> { Twelfth.Third, Twelfth.Fourth, Twelfth.Fifth };
        TestAssert.IsTrue(
            SeasonalMath.ResolveState(3, Twelfth.Fourth, growing) == SeasonalMath.SeasonState.InGrowing);
    }

    [EmpireTest("CF.Seasonal")]
    public static void ResolveState_CurrentOutsideGrowingList_IsOutOfGrowing()
    {
        var growing = new List<Twelfth> { Twelfth.Third, Twelfth.Fourth, Twelfth.Fifth };
        TestAssert.IsTrue(
            SeasonalMath.ResolveState(3, Twelfth.Tenth, growing) == SeasonalMath.SeasonState.OutOfGrowing);
    }

    [EmpireTest("CF.Seasonal")]
    public static void SelectMultiplier_PicksFieldPerState()
    {
        // Distinct sentinel per field so a mis-mapped case is caught.
        TestAssert.AreEqual(1.2, SeasonalMath.SelectMultiplier(SeasonalMath.SeasonState.InGrowing, 1.2, 0.5, 1.0, 0.1));
        TestAssert.AreEqual(0.5, SeasonalMath.SelectMultiplier(SeasonalMath.SeasonState.OutOfGrowing, 1.2, 0.5, 1.0, 0.1));
        TestAssert.AreEqual(1.0, SeasonalMath.SelectMultiplier(SeasonalMath.SeasonState.YearRound, 1.2, 0.5, 1.0, 0.1));
        TestAssert.AreEqual(0.1, SeasonalMath.SelectMultiplier(SeasonalMath.SeasonState.NoGrowing, 1.2, 0.5, 1.0, 0.1));
    }
}
