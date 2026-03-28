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
    /// </summary>
    public class WorldObjectComp_ToolsBenefit : WorldObjectComp, IResourceProductionModifier
    {
        private double cachedMultiplier = 1.0;
        private string cachedCityName;
        private const int CHECK_INTERVAL = GenDate.TicksPerDay;

        // Static coordinator: prevents multiple rurals from independently draining the same city.
        // Key = city tile ID, Value = per-rural multiplier for this interval.
        private static Dictionary<int, double> cityMultiplierCache = new Dictionary<int, double>();
        private static Dictionary<int, string> cityNameCache = new Dictionary<int, string>();
        private static int lastCoordinatorTick = -1;

        // Which city this rural is assigned to (by tile ID), -1 if none.
        private int assignedCityTile = -1;

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

        public override void CompTick()
        {
            if (Find.TickManager.TicksGame % CHECK_INTERVAL != 0)
                return;

            FactionFC faction = FactionCache.FactionComp;
            if (faction == null || Find.WorldGrid == null)
                return;

            int currentTick = Find.TickManager.TicksGame;

            // If the coordinator hasn't run this interval, recalculate all city allocations.
            if (currentTick != lastCoordinatorTick)
            {
                lastCoordinatorTick = currentTick;
                RecalculateAllCities(faction);
            }

            // Read our cached multiplier from the coordinator.
            double oldMultiplier = cachedMultiplier;
            if (assignedCityTile >= 0 && cityMultiplierCache.TryGetValue(assignedCityTile, out double mult))
            {
                cachedMultiplier = 1.0 + mult * FCURSettings.toolsProductionBonus;
                cityNameCache.TryGetValue(assignedCityTile, out cachedCityName);
            }
            else
            {
                cachedMultiplier = 1.0;
                cachedCityName = null;
            }

            // Invalidate resource caches if multiplier changed.
            if (cachedMultiplier != oldMultiplier)
            {
                WorldSettlementFC settlement = parent as WorldSettlementFC;
                if (settlement != null)
                    settlement.InvalidateResourceCaches();
            }
        }

        /// <summary>
        /// Runs once per interval. For each city, counts nearby rurals, calculates fair share,
        /// consumes Tools from city stockpile, and caches the per-rural multiplier fraction.
        /// </summary>
        private static void RecalculateAllCities(FactionFC faction)
        {
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

            // Assign each rural to its nearest city within range.
            foreach (WorldSettlementFC settlement in faction.settlements)
            {
                if (!IsRuralSettlement(settlement)) continue;

                WorldObjectComp_ToolsBenefit comp = settlement.GetComponent<WorldObjectComp_ToolsBenefit>();
                if (comp == null) continue;

                comp.assignedCityTile = -1;
                float bestDist = float.MaxValue;

                foreach (int cityTile in cityMultiplierCache.Keys)
                {
                    float dist = Find.WorldGrid.ApproxDistanceInTiles(settlement.Tile, cityTile);
                    if (dist <= toolsRange && dist < bestDist)
                    {
                        bestDist = dist;
                        comp.assignedCityTile = cityTile;
                    }
                }
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
