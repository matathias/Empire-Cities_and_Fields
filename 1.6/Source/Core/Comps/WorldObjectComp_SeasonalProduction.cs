using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace FactionColonies.UrbanRural
{
    public class WorldObjectCompProperties_SeasonalProduction : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_SeasonalProduction()
        {
            compClass = typeof(WorldObjectComp_SeasonalProduction);
        }
    }

    /// <summary>
    /// Applies seasonal production multipliers to rural settlements based on their tile's growing season.
    /// Attached to Farm, Ranch, and LumberCamp settlement defs. Implements IResourceProductionModifier
    /// so it's automatically picked up by ResourceFC's production formula.
    /// </summary>
    public class WorldObjectComp_SeasonalProduction : WorldObjectComp, IResourceProductionModifier
    {
        private enum SeasonState
        {
            InGrowing,
            OutOfGrowing,
            YearRound,
            NoGrowing
        }

        // Multiplier table: [InGrowing, OutOfGrowing, YearRound, NoGrowing]
        private static readonly Dictionary<string, double[]> MultiplierTable = new Dictionary<string, double[]>
        {
            { "WorldSettlementDef_Farm",       new[] { 1.2,  0.5, 1.1,  0.4 } },
            { "WorldSettlementDef_Ranch",      new[] { 1.15, 0.6, 1.05, 0.5 } },
            { "WorldSettlementDef_LumberCamp", new[] { 1.1,  0.7, 1.05, 0.6 } },
            { "WorldSettlementDef_Herbalist",  new[] { 1.15, 0.6, 1.05, 0.5 } }
        };

        private static readonly string[] SeasonLabelKeys = { "UR_SeasonGrowing", "UR_SeasonOffSeason", "UR_SeasonYearRound", "UR_SeasonNoGrowing" };

        private List<Twelfth> growingTwelfths;
        private int growingMonthCount = -1;
        private Twelfth lastCheckedTwelfth = Twelfth.Undefined;
        private double cachedMultiplier = 1.0;
        private string cachedLabel;
        private const int CHECK_INTERVAL = GenDate.TicksPerDay;

        public override void CompTick()
        {
            if (Find.TickManager.TicksGame % CHECK_INTERVAL != 0)
                return;

            if (Find.WorldGrid == null)
                return;

            // Lazy init growing season data on first tick
            if (growingMonthCount < 0)
                InitGrowingSeason();

            float longitude = Find.WorldGrid.LongLatOf(parent.Tile).x;
            Twelfth currentTwelfth = GenDate.Twelfth(Find.TickManager.TicksAbs, longitude);
            if (currentTwelfth != lastCheckedTwelfth)
            {
                lastCheckedTwelfth = currentTwelfth;
                RecalculateMultiplier();

                WorldSettlementFC settlement = parent as WorldSettlementFC;
                if (settlement != null)
                    settlement.InvalidateResourceCaches();
            }
        }

        private void InitGrowingSeason()
        {
            if (parent.Tile < 0)
            {
                growingTwelfths = new List<Twelfth>();
                growingMonthCount = 12;
                return;
            }

            growingTwelfths = GenTemperature.TwelfthsInAverageTemperatureRange(parent.Tile, 6f, 42f);
            growingMonthCount = growingTwelfths.Count;

            LogUR.Message("Initialized seasonal production for tile " + parent.Tile
                + ": " + growingMonthCount + "/12 growing months");
        }

        private SeasonState GetSeasonState()
        {
            if (growingMonthCount == 0) return SeasonState.NoGrowing;
            if (growingMonthCount == 12) return SeasonState.YearRound;
            if (growingTwelfths.Contains(lastCheckedTwelfth)) return SeasonState.InGrowing;
            return SeasonState.OutOfGrowing;
        }

        private void RecalculateMultiplier()
        {
            WorldSettlementFC settlement = parent as WorldSettlementFC;
            if (settlement == null)
            {
                cachedMultiplier = 1.0;
                cachedLabel = null;
                return;
            }

            WorldSettlementDef sDef = settlement.settlementDef;
            double[] table;
            if (sDef == null || !MultiplierTable.TryGetValue(sDef.defName, out table))
            {
                cachedMultiplier = 1.0;
                cachedLabel = null;
                return;
            }

            SeasonState state = GetSeasonState();
            int idx = (int)state;
            cachedMultiplier = table[idx];
            cachedLabel = SeasonLabelKeys[idx].Translate();
        }

        private bool IsSettlementPrimaryResource(ResourceFC resource)
        {
            WorldSettlementFC settlement = parent as WorldSettlementFC;
            if (settlement == null) return false;

            WorldSettlementDef sDef = settlement.settlementDef;
            if (sDef == null) return false;

            for (int i = 0; i < sDef.resources.Count; i++)
            {
                if (sDef.resources[i].resourceDef == resource.def
                    && sDef.resources[i].multiplier > 1.0)
                    return true;
            }
            return false;
        }

        public double GetResourceAdditiveModifier(ResourceFC resource)
        {
            return 0;
        }

        public double GetResourceMultiplierModifier(ResourceFC resource)
        {
            if (!IsSettlementPrimaryResource(resource))
                return 1.0;

            // Ensure we have a calculated multiplier
            if (growingMonthCount < 0)
                return 1.0;

            return cachedMultiplier;
        }

        public string GetResourceModifierDesc(ResourceFC resource)
        {
            if (!IsSettlementPrimaryResource(resource))
                return null;

            if (growingMonthCount < 0 || cachedLabel == null)
                return null;

            double pct = (cachedMultiplier - 1.0) * 100.0;
            string sign = pct >= 0 ? "+" : "";
            return "UR_SeasonalModifier".Translate(sign + pct.ToString("F0"), cachedLabel);
        }
    }
}
