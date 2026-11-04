namespace FactionColonies.UrbanRural;

/// <summary>
/// Pure season-state resolution and multiplier selection for
/// <see cref="WorldObjectComp_SeasonalProduction"/>, factored out so it can be unit-tested
/// without a live world grid. The comp keeps the tile lookup, growing-season init, and cache
/// invalidation; these methods are the deterministic classification it calls.
/// </summary>
public static class SeasonalMath
{
    public enum SeasonState
    {
        InGrowing,
        OutOfGrowing,
        YearRound,
        NoGrowing
    }

    /// <summary>
    /// Classifies the current season for a tile whose growing twelfths have already been resolved:
    /// no growing months at all → NoGrowing, all twelve → YearRound, otherwise InGrowing when the
    /// current twelfth is a growing one and OutOfGrowing when it is not.
    /// </summary>
    public static SeasonState ResolveState(int growingMonthCount, Twelfth current, List<Twelfth> growingTwelfths)
    {
        if (growingMonthCount == 0) return SeasonState.NoGrowing;
        if (growingMonthCount == 12) return SeasonState.YearRound;
        if (growingTwelfths is not null && growingTwelfths.Contains(current)) return SeasonState.InGrowing;
        return SeasonState.OutOfGrowing;
    }

    /// <summary>Picks the multiplier field matching the given season state.</summary>
    public static double SelectMultiplier(SeasonState state, double inGrowing, double outOfGrowing, double yearRound, double noGrowing)
    {
        return state switch
        {
            SeasonState.InGrowing => inGrowing,
            SeasonState.OutOfGrowing => outOfGrowing,
            SeasonState.YearRound => yearRound,
            SeasonState.NoGrowing => noGrowing,
            _ => 1.0,
        };
    }
}
