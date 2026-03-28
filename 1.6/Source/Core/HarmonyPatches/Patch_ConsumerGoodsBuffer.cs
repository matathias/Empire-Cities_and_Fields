using System;
using System.Collections.Generic;
using HarmonyLib;
using Verse;
using FactionColonies.SupplyChain;

namespace FactionColonies.UrbanRural
{
    /// <summary>
    /// Harmony patches that auto-consume Consumer Goods from settlement stockpiles
    /// when negative event outcomes are applied, reducing the penalty proportionally.
    /// </summary>
    [HarmonyPatch(typeof(WorldSettlementFC))]
    [HarmonyPatch("AddStatModifiers")]
    public static class Patch_AddStatModifiers_ConsumerGoodsBuffer
    {
        private static FCStatDef happinessLostBase;
        private static FCStatDef unrestGainedBase;
        private static ResourceTypeDef consumerGoodsDef;
        private static bool defsResolved;

        private static void ResolveDefs()
        {
            if (defsResolved) return;
            happinessLostBase = DefDatabase<FCStatDef>.GetNamedSilentFail("happinessLostBase");
            unrestGainedBase = DefDatabase<FCStatDef>.GetNamedSilentFail("unrestGainedBase");
            consumerGoodsDef = DefDatabase<ResourceTypeDef>.GetNamedSilentFail("RTD_ConsumerGoods");
            defsResolved = true;
        }

        [HarmonyPostfix]
        public static void Postfix(WorldSettlementFC __instance, List<FCStatModifier> mods, string sourceId, string sourceLabel)
        {
            if (mods == null || string.IsNullOrEmpty(sourceId)) return;
            if (!sourceId.StartsWith("event_")) return;

            ResolveDefs();
            if (happinessLostBase == null || unrestGainedBase == null || consumerGoodsDef == null) return;

            // Sum penalty magnitude from the event's stat modifiers.
            double magnitude = 0;
            foreach (FCStatModifier mod in mods)
            {
                if (mod.stat == happinessLostBase && mod.value > 0)
                    magnitude += mod.value;
                else if (mod.stat == unrestGainedBase && mod.value > 0)
                    magnitude += mod.value;
            }

            if (magnitude <= 0) return;

            // Get Consumer Goods from this settlement's stockpile.
            double cgAvailable = GetConsumerGoodsAmount(__instance);
            if (cgAvailable <= 0) return;

            // Calculate consumption using worker-scaled formula.
            double workerFactor = __instance.GetTotalWorkers() / (double)FCURSettings.cgBaseWorkers;
            double cgRequired = magnitude * FCURSettings.cgConversionRate * workerFactor;
            double cgConsumed = Math.Min(cgRequired, cgAvailable);
            double mitigation = (cgConsumed / cgRequired) * FCURSettings.cgMaxMitigation;

            if (mitigation <= 0) return;

            // Build compensating stat modifiers that reduce the negative impact.
            List<FCStatModifier> mitigationMods = new List<FCStatModifier>();
            foreach (FCStatModifier mod in mods)
            {
                bool isNegative = (mod.stat == happinessLostBase && mod.value > 0)
                               || (mod.stat == unrestGainedBase && mod.value > 0);
                if (isNegative)
                {
                    mitigationMods.Add(new FCStatModifier
                    {
                        stat = mod.stat,
                        value = -mod.value * mitigation
                    });
                }
            }

            // Apply compensating modifiers with a distinct source ID (won't recurse — doesn't start with "event_").
            string bufferSourceId = "cg_buffer_" + sourceId;
            __instance.AddStatModifiers(mitigationMods, bufferSourceId, "UR_ConsumerGoodsSource".Translate());

            // Consume the Consumer Goods from stockpile.
            ConsumeConsumerGoods(__instance, cgConsumed);

            // Notify the player.
            double mitigationPct = mitigation * 100.0;
            string eventName = sourceLabel ?? sourceId.Replace("event_", "");
            LogUR.Message(__instance.Name + ": " + cgConsumed.ToString("F1")
                + " Consumer Goods consumed to mitigate " + eventName
                + " (-" + mitigationPct.ToString("F0") + "% impact)");
        }

        private static double GetConsumerGoodsAmount(WorldSettlementFC settlement)
        {
            IStockpile stockpile = settlement.GetComponent<WorldObjectComp_SupplyChain>()?.GetStockpile();
            if (stockpile == null) return 0;

            return stockpile.GetAmount(consumerGoodsDef);
        }

        private static void ConsumeConsumerGoods(WorldSettlementFC settlement, double amount)
        {
            IStockpile stockpile = settlement.GetComponent<WorldObjectComp_SupplyChain>()?.GetStockpile();
            if (stockpile == null) return;

            stockpile.TryDraw(consumerGoodsDef, amount, out double drawn);

            if (drawn > amount)
            {
                LogUR.Warning($"ConsumeConsumerGoods on settlement {settlement.Name} drew {drawn}, but requestion {amount}");
            }

            if (drawn < amount)
            {
                LogUR.Warning($"ConsumeConsumerGoods on settlement {settlement.Name} drew {drawn}, but requestion {amount}");
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
        }
    }
}
