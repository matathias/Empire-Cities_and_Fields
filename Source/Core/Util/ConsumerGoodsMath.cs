using System;

namespace FactionColonies.UrbanRural;

/// <summary>
/// Pure arithmetic for the Consumer-Goods event buffer, factored out of
/// <see cref="Patch_AddStatModifiers_ConsumerGoodsBuffer"/> so it can be unit-tested without a
/// live settlement. The patch keeps the event-source detection, stockpile I/O, and stat-modifier
/// application; these methods are the settings-parameterized math it calls.
/// </summary>
public static class ConsumerGoodsMath
{
    /// <summary>
    /// Consumer Goods required to fully mitigate a penalty of the given magnitude, scaled by the
    /// settlement's worker factor (workers / cgBaseWorkers).
    /// </summary>
    public static double Required(double magnitude, double conversionRate, double workerFactor)
    {
        return magnitude * conversionRate * workerFactor;
    }

    /// <summary>Consumer Goods actually consumed: the required amount, capped by what is available.</summary>
    public static double Consumed(double required, double available)
    {
        return Math.Min(required, available);
    }

    /// <summary>
    /// Mitigation fraction applied to the penalty: the share of the requirement that was covered,
    /// scaled by the configured cap. Returns 0 when there is no requirement, which also guards
    /// against a divide-by-zero producing NaN.
    /// </summary>
    public static double Mitigation(double consumed, double required, double maxMitigation)
    {
        return required <= 0 ? 0.0 : (consumed / required) * maxMitigation;
    }
}
