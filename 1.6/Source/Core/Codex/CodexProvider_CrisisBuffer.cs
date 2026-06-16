using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using FactionColonies.SupplyChain;

namespace FactionColonies.UrbanRural
{
    /// <summary>
    /// Dynamic Codex provider for the Crisis Buffer entry. Lists each city's current
    /// consumer-goods reserve and roughly how large a negative event that reserve can
    /// fully cushion. Mirrors the auto-consume formula in Patch_ConsumerGoodsBuffer:
    /// an event of total penalty magnitude M needs M * cgConversionRate * (workers /
    /// cgBaseWorkers) consumer goods to reach the maximum mitigation fraction, so the
    /// largest fully-cushioned magnitude is reserve / (cgConversionRate * workerFactor).
    /// </summary>
    public class CodexProvider_CrisisBuffer : ICodexDynamicProvider
    {
        public string GetDynamicContent(FactionFC faction)
        {
            if (faction is null) return null;

            List<WorldSettlementFC> cities = faction.settlements
                .Where(s => s.settlementDef == CFSettlementDefOf.WorldSettlementDef_City)
                .ToList();

            if (!cities.Any())
                return "UR_CodexBufferNoCities".Translate();

            double capPct = FCURSettings.cgMaxMitigation * 100.0;

            string result = "UR_CodexBufferHeader".Translate() + "\n\n";
            foreach (WorldSettlementFC s in cities)
            {
                double reserve = GetConsumerGoods(s);
                result += s.Name + ":\n";

                if (reserve <= 0)
                {
                    result += "  " + "UR_CodexBufferNoReserve".Translate() + "\n\n";
                    continue;
                }

                result += "  " + "UR_CodexBufferReserve".Translate(Math.Round(reserve, 1)) + "\n";

                double workerFactor = s.GetTotalWorkers() / (double)FCURSettings.cgBaseWorkers;
                double denom = FCURSettings.cgConversionRate * workerFactor;
                if (denom > 0)
                {
                    double fullyCushioned = reserve / denom;
                    result += "  " + "UR_CodexBufferCapacity".Translate(
                        Math.Round(fullyCushioned, 1),
                        Math.Round(capPct)) + "\n";
                }
                result += "\n";
            }
            return result.TrimEnd();
        }

        private static double GetConsumerGoods(WorldSettlementFC settlement)
        {
            IStockpile stockpile = settlement.GetComponent<WorldObjectComp_SupplyChain>()?.GetStockpile();
            if (stockpile is null) return 0;
            return stockpile.GetAmount(CFResourceDefOf.RTD_ConsumerGoods);
        }
    }
}
