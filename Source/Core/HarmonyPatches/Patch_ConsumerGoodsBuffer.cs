using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using FactionColonies.SupplyChain;

namespace FactionColonies.UrbanRural;

/// <summary>
/// Harmony patches that auto-consume Consumer Goods from settlement stockpiles
/// when negative event outcomes are applied, reducing the penalty proportionally.
/// </summary>
[HarmonyPatch(typeof(WorldSettlementFC))]
[HarmonyPatch("AddStatModifiers")]
public static class Patch_AddStatModifiers_ConsumerGoodsBuffer
{
    [HarmonyPostfix]
    public static void Postfix(WorldSettlementFC __instance, List<FCStatModifier> mods, string sourceId, string sourceLabel)
    {
        if (mods == null || string.IsNullOrEmpty(sourceId)) return;
        if (!sourceId.StartsWith("event_")) return;

        // Sum penalty magnitude from the event's stat modifiers.
        double magnitude = 0;
        foreach (FCStatModifier mod in mods)
        {
            if (mod.stat == FCStatDefOf.happinessLostBase && mod.value > 0)
                magnitude += mod.value;
            else if (mod.stat == FCStatDefOf.unrestGainedBase && mod.value > 0)
                magnitude += mod.value;
        }

        if (magnitude <= 0) return;

        WorldComponent_UrbanRural coordinator = Find.World?.GetComponent<WorldComponent_UrbanRural>();

        /* Load-reapply path: statModifiers isn't serialized, so WorldSettlementFC.PostLoadInit
         * rebuilds it from sources and re-fires this postfix (Scribe.mode != Inactive). Never
         * draw CG on load — instead re-add the buffer mods using the mitigation fraction that
         * was persisted when the event first fired, so the player keeps the mitigation they
         * already paid for. No draw, no worker mutation, no player message during load. */
        if (Scribe.mode != LoadSaveMode.Inactive)
        {
            double storedMitigation;
            if (coordinator != null
                && coordinator.TryGetEventMitigation(__instance.ID, sourceId, out storedMitigation)
                && storedMitigation > 0)
            {
                ApplyMitigationMods(__instance, mods, sourceId, storedMitigation);
            }
            return;
        }

        // Get Consumer Goods from this settlement's stockpile.
        double cgAvailable = GetConsumerGoodsAmount(__instance);
        if (cgAvailable <= 0) return;

        // Calculate consumption using worker-scaled formula.
        double workerFactor = __instance.GetTotalWorkers() / (double)FCURSettings.cgBaseWorkers;
        double cgRequired = ConsumerGoodsMath.Required(magnitude, FCURSettings.cgConversionRate, workerFactor);
        // With 0 assigned workers cgRequired is 0; guard here so mitigation never divides by zero
        // (a NaN result would slip past the mitigation <= 0 check below and poison stat aggregation).
        if (cgRequired <= 0) return;

        double cgConsumed = ConsumerGoodsMath.Consumed(cgRequired, cgAvailable);
        double mitigation = ConsumerGoodsMath.Mitigation(cgConsumed, cgRequired, FCURSettings.cgMaxMitigation);

        if (mitigation <= 0) return;

        // Apply compensating modifiers and consume the goods, then persist the mitigation
        // fraction so the load path can re-add the buffer mods without re-drawing CG.
        ApplyMitigationMods(__instance, mods, sourceId, mitigation);
        ConsumeConsumerGoods(__instance, cgConsumed);
        coordinator?.SetEventMitigation(__instance.ID, sourceId, mitigation);

        // Notify the player.
        double mitigationPct = mitigation * 100.0;
        string eventName = sourceLabel ?? sourceId.Replace("event_", "");
        string notification = "UR_ConsumerGoodsConsumed".Translate(
            cgConsumed.ToString("F1"),
            __instance.Name,
            mitigationPct.ToString("F0"),
            eventName);
        Messages.Message(notification, MessageTypeDefOf.NeutralEvent, false);
        LogUR.Message(notification);
    }

    /// <summary>
    /// Builds and applies the compensating stat modifiers that reduce the negative event impact
    /// by the given mitigation fraction. Uses a distinct source ID that won't recurse (doesn't
    /// start with "event_"). Called from both the in-game and load-reapply paths with the same
    /// mods, so the buffer is reconstructed identically after a reload.
    /// </summary>
    private static void ApplyMitigationMods(WorldSettlementFC settlement, List<FCStatModifier> mods, string sourceId, double mitigation)
    {
        List<FCStatModifier> mitigationMods = new List<FCStatModifier>();
        foreach (FCStatModifier mod in mods)
        {
            bool isNegative = (mod.stat == FCStatDefOf.happinessLostBase && mod.value > 0)
                           || (mod.stat == FCStatDefOf.unrestGainedBase && mod.value > 0);
            if (isNegative)
            {
                mitigationMods.Add(new FCStatModifier
                {
                    stat = mod.stat,
                    value = -mod.value * mitigation
                });
            }
        }

        settlement.AddStatModifiers(mitigationMods, "cg_buffer_" + sourceId, "UR_ConsumerGoodsSource".Translate());
    }

    private static double GetConsumerGoodsAmount(WorldSettlementFC settlement)
    {
        IStockpile stockpile = settlement.GetComponent<WorldObjectComp_SupplyChain>()?.GetStockpile();
        if (stockpile == null) return 0;

        return stockpile.GetAmount(CFResourceDefOf.RTD_ConsumerGoods);
    }

    private static void ConsumeConsumerGoods(WorldSettlementFC settlement, double amount)
    {
        IStockpile stockpile = settlement.GetComponent<WorldObjectComp_SupplyChain>()?.GetStockpile();
        if (stockpile == null) return;

        stockpile.TryDraw(CFResourceDefOf.RTD_ConsumerGoods, amount, out double drawn);

        if (drawn != amount)
        {
            LogUR.Warning($"ConsumeConsumerGoods on settlement {settlement.Name}: drew {drawn}, requested {amount}");
        }
    }
}

/// <summary>
/// Cleans up CG buffer stat modifiers when the originating event's modifiers are removed.
/// </summary>
[HarmonyPatch(typeof(WorldSettlementFC))]
[HarmonyPatch("RemoveStatModifiers")]
public static class Patch_RemoveStatModifiers_ConsumerGoodsCleanup
{
    [HarmonyPostfix]
    public static void Postfix(WorldSettlementFC __instance, List<FCStatModifier> mods, string sourceId)
    {
        if (string.IsNullOrEmpty(sourceId)) return;
        if (!sourceId.StartsWith("event_")) return;

        __instance.RemoveStatModifiersBySource("cg_buffer_" + sourceId);
        Find.World?.GetComponent<WorldComponent_UrbanRural>()?.ClearEventMitigation(__instance.ID, sourceId);
    }
}
