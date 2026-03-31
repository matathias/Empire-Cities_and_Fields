using System.Collections.Generic;
using FactionColonies;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace FactionColonies.UrbanRural
{
    public class WorldObjectCompProperties_ToolsBenefit : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_ToolsBenefit()
        {
            compClass = typeof(WorldObjectComp_ToolsBenefit);
        }
    }

    /// <summary>
    /// Attached to rural settlement defs. Checks nearby cities for Tools stockpile and applies
    /// a production multiplier. Tools are consumed from the city each tax tick.
    /// Uses a static coordinator so multiple rurals near the same city split Tools fairly.
    /// Triggered by Patch_ToolsTaxTick as a Harmony prefix on FactionFC.AddTax().
    /// </summary>
    public class WorldObjectComp_ToolsBenefit : WorldObjectComp, IResourceProductionModifier
    {
        private double cachedMultiplier = 1.0;
        private string cachedCityName;

        // Static coordinator: prevents multiple rurals from independently draining the same city.
        // Key = city tile ID, Value = per-rural multiplier fraction for this tax cycle.
        private static Dictionary<int, double> cityMultiplierCache = new Dictionary<int, double>();
        private static Dictionary<int, string> cityNameCache = new Dictionary<int, string>();

        private static WorldSettlementDef cachedCityDef;
        private static WorldSettlementDef CityDef
        {
            get
            {
                if (cachedCityDef == null)
                    cachedCityDef = DefDatabase<WorldSettlementDef>.GetNamedSilentFail("WorldSettlementDef_City");
                return cachedCityDef;
            }
        }

        private static ResourceTypeDef cachedToolsDef;
        private static ResourceTypeDef ToolsDef
        {
            get
            {
                if (cachedToolsDef == null)
                    cachedToolsDef = DefDatabase<ResourceTypeDef>.GetNamedSilentFail("RTD_Tools");
                return cachedToolsDef;
            }
        }

        /// <summary>
        /// Runs once per tax tick (called from Patch_ToolsTaxTick).
        /// For each city, counts nearby rurals, calculates fair share, consumes Tools from
        /// city stockpile, assigns each rural to its nearest city, and updates cached multipliers.
        /// </summary>
        public static void RecalculateAllCities(FactionFC faction)
        {
            if (faction == null || Find.WorldGrid == null) return;

            cityMultiplierCache.Clear();
            cityNameCache.Clear();

            WorldSettlementDef cityDef = CityDef;
            ResourceTypeDef toolsDef = ToolsDef;
            if (cityDef == null || toolsDef == null) return;

            float toolsRange = FCURSettings.toolsRange;
            float costPerRural = FCURSettings.toolsCostPerRural;

            // Find all cities and their nearby rurals.
            foreach (WorldSettlementFC settlement in faction.settlements)
            {
                if (settlement.settlementDef != cityDef) continue;

                // Count rurals within range of this city.
                int ruralCount = 0;
                foreach (WorldSettlementFC other in faction.settlements)
                {
                    if (other == settlement) continue;
                    if (!IsRuralSettlement(other)) continue;

                    float dist = Find.WorldGrid.ApproxDistanceInTiles(settlement.Tile, other.Tile);
                    if (dist <= toolsRange)
                        ruralCount++;
                }

                if (ruralCount == 0) continue;

                // Get city's Tools stockpile.
                double available = GetToolsStockpile(settlement, toolsDef);
                double totalDemand = ruralCount * costPerRural;

                // Calculate how much each rural gets (0.0 to 1.0 fraction of costPerRural).
                double fraction = totalDemand > 0 ? System.Math.Min(available / totalDemand, 1.0) : 0;

                // Consume Tools from city stockpile.
                double toConsume = System.Math.Min(available, totalDemand);
                if (toConsume > 0)
                    ConsumeFromStockpile(settlement, toolsDef, toConsume);

                cityMultiplierCache[settlement.Tile] = fraction;
                cityNameCache[settlement.Tile] = settlement.Name;
            }

            // Assign each rural to its nearest city within range and update cached multipliers.
            foreach (WorldSettlementFC settlement in faction.settlements)
            {
                if (!IsRuralSettlement(settlement)) continue;

                WorldObjectComp_ToolsBenefit comp = settlement.GetComponent<WorldObjectComp_ToolsBenefit>();
                if (comp == null) continue;

                int assignedCityTile = -1;
                float bestDist = float.MaxValue;

                foreach (int cityTile in cityMultiplierCache.Keys)
                {
                    float dist = Find.WorldGrid.ApproxDistanceInTiles(settlement.Tile, cityTile);
                    if (dist <= toolsRange && dist < bestDist)
                    {
                        bestDist = dist;
                        assignedCityTile = cityTile;
                    }
                }

                // Update the comp's cached multiplier.
                double oldMultiplier = comp.cachedMultiplier;
                double mult;
                string cityName;
                if (assignedCityTile >= 0 && cityMultiplierCache.TryGetValue(assignedCityTile, out mult))
                {
                    comp.cachedMultiplier = 1.0 + mult * FCURSettings.toolsProductionBonus;
                    cityNameCache.TryGetValue(assignedCityTile, out cityName);
                    comp.cachedCityName = cityName;
                }
                else
                {
                    comp.cachedMultiplier = 1.0;
                    comp.cachedCityName = null;
                }

                if (comp.cachedMultiplier != oldMultiplier)
                    settlement.InvalidateResourceCaches();
            }
        }

        private static bool IsRuralSettlement(WorldSettlementFC settlement)
        {
            WorldSettlementDef ruralDef = DefDatabase<WorldSettlementDef>.GetNamedSilentFail("WorldSettlementDef_Rural");
            if (ruralDef == null) return false;

            WorldSettlementDef def = settlement.settlementDef;
            while (def != null)
            {
                if (def == ruralDef) return true;
                def = def.baseSettlementType;
            }
            return false;
        }

        private static double GetToolsStockpile(WorldSettlementFC settlement, ResourceTypeDef toolsDef)
        {
            var comp = settlement.GetComponent<SupplyChain.WorldObjectComp_SupplyChain>();
            if (comp == null) return 0;

            var stockpile = comp.GetStockpile();
            if (stockpile == null) return 0;

            return stockpile.GetAmount(toolsDef);
        }

        private static void ConsumeFromStockpile(WorldSettlementFC settlement, ResourceTypeDef toolsDef, double amount)
        {
            var comp = settlement.GetComponent<SupplyChain.WorldObjectComp_SupplyChain>();
            if (comp == null) return;

            var stockpile = comp.GetStockpile();
            if (stockpile == null) return;

            double drawn;
            stockpile.TryDraw(toolsDef, amount, out drawn);
        }

        // IResourceProductionModifier
        public double GetResourceAdditiveModifier(ResourceFC resource)
        {
            return 0;
        }

        public double GetResourceMultiplierModifier(ResourceFC resource)
        {
            return cachedMultiplier;
        }

        public string GetResourceModifierDesc(ResourceFC resource)
        {
            if (cachedMultiplier <= 1.0 || cachedCityName == null)
                return null;

            double pct = (cachedMultiplier - 1.0) * 100.0;
            return "UR_ToolsModifier".Translate(pct.ToString("F0"), cachedCityName);
        }
    }
}
