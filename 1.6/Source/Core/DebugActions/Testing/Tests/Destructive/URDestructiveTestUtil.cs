using System.Linq;
using FactionColonies.SupplyChain;

namespace FactionColonies.UrbanRural
{
    /// <summary>
    /// Cities &amp; Fields destructive-tier helpers, layered on the base mod's
    /// <see cref="DestructiveTestUtil"/>. Provides city/rural lookup, a stockpile seeder (which
    /// forces the SupplyChain local stockpile to exist and credits into it), and the shared
    /// rural-classification used by the Tools coordinator. Preferring live settlements over
    /// transient ones lets the positive paths engage on a mature save (real workers/buildings);
    /// callers skip when the environment is too bare.
    /// </summary>
    public static class URDestructiveTestUtil
    {
        public static bool IsRural(WorldSettlementFC settlement)
        {
            WorldSettlementDef rural = CFSettlementDefOf.WorldSettlementDef_Rural;
            if (rural is null || settlement is null) return false;
            WorldSettlementDef def = settlement.settlementDef;
            while (def != null)
            {
                if (def == rural) return true;
                def = def.baseSettlementType;
            }
            return false;
        }

        public static WorldSettlementFC FirstCity(FactionFC f)
        {
            return f?.settlements?.FirstOrDefault(s => s.settlementDef == CFSettlementDefOf.WorldSettlementDef_City);
        }

        public static WorldSettlementFC FirstRural(FactionFC f)
        {
            return f?.settlements?.FirstOrDefault(IsRural);
        }

        /// <summary>
        /// Forces the settlement's SupplyChain local stockpile to exist (so the mod's GetStockpile()
        /// reads it) and credits <paramref name="amount"/> of the resource. Returns true only if the
        /// full amount actually landed (Credit clamps to cap), so callers can skip when the cap
        /// prevents seeding. Returns false if the settlement has no SupplyChain comp.
        /// </summary>
        public static bool Seed(WorldSettlementFC settlement, ResourceTypeDef res, double amount)
        {
            WorldObjectComp_SupplyChain comp = settlement?.GetComponent<WorldObjectComp_SupplyChain>();
            if (comp is null || res is null) return false;

            IStockpile sp = comp.EnsureLocalStockpile();
            if (sp is null) return false;

            sp.Credit(res, amount);
            return sp.GetAmount(res) >= amount;
        }

        public static double StockpileAmount(WorldSettlementFC settlement, ResourceTypeDef res)
        {
            IStockpile sp = settlement?.GetComponent<WorldObjectComp_SupplyChain>()?.GetStockpile();
            return sp is null ? 0.0 : sp.GetAmount(res);
        }

        public static WorldComponent_UrbanRural Coordinator()
        {
            return Verse.Find.World?.GetComponent<WorldComponent_UrbanRural>();
        }
    }
}
