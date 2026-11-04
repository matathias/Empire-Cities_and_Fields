namespace FactionColonies.UrbanRural;

/// <summary>
/// Pure arithmetic for the Tools-proximity mechanic, factored out of
/// <see cref="WorldObjectComp_ToolsBenefit.RecalculateAllCities"/> so it can be unit-tested
/// without a live world. The comp keeps the settlement iteration, WorldGrid distance checks,
/// and stockpile I/O; these methods are the settings-parameterized math it calls.
/// </summary>
public static class ToolsDistributionMath
{
    /// <summary>
    /// Fraction of each assigned rural's Tools entitlement a city can actually cover (0.0 to 1.0).
    /// Returns 0 when there is no demand.
    /// </summary>
    public static double Coverage(double available, double totalDemand)
    {
        return totalDemand > 0 ? Math.Min(available / totalDemand, 1.0) : 0.0;
    }

    /// <summary>Amount of Tools the city actually spends this day (never more than it has or owes).</summary>
    public static double ToConsume(double available, double totalDemand)
    {
        return Math.Min(available, totalDemand);
    }

    /// <summary>
    /// Per-rural production multiplier for a given coverage fraction:
    /// full coverage yields 1.0 + productionBonus, no coverage yields 1.0.
    /// </summary>
    public static double Multiplier(double coverageFraction, double productionBonus)
    {
        return 1.0 + coverageFraction * productionBonus;
    }
}
