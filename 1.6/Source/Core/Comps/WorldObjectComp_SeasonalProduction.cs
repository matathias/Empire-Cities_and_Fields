using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace FactionColonies.UrbanRural
{
    /// <summary>
    /// Defines seasonal production multipliers for a single resource type.
    /// One def per resource. The comp auto-discovers these from DefDatabase.
    /// </summary>
    public class SeasonalMultiplierDef : Def
    {
        public ResourceTypeDef resource;
        public double inGrowing = 1.0;
        public double outOfGrowing = 1.0;
        public double yearRound = 1.0;
        public double noGrowing = 1.0;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
                yield return error;
            if (resource == null)
                yield return defName + ": resource is null";
        }
    }

    /// <summary>
    /// Inline seasonal multiplier entry for per-settlement overrides on the comp properties.
    /// Same shape as SeasonalMultiplierDef but not a Def — used in XML lists on the comp.
    /// </summary>
    public class SeasonalMultiplierEntry
    {
        public ResourceTypeDef resource;
        public double inGrowing = 1.0;
        public double outOfGrowing = 1.0;
        public double yearRound = 1.0;
        public double noGrowing = 1.0;
    }

    public class WorldObjectCompProperties_SeasonalProduction : WorldObjectCompProperties
    {
        /// <summary>
        /// Optional per-settlement overrides. Takes priority over global SeasonalMultiplierDefs.
        /// </summary>
        public List<SeasonalMultiplierEntry> overrides;

        public WorldObjectCompProperties_SeasonalProduction()
        {
            compClass = typeof(WorldObjectComp_SeasonalProduction);
        }
    }

    /// <summary>
    /// Applies seasonal production multipliers to settlements based on their tile's growing season.
    /// Multipliers are defined globally per resource type via SeasonalMultiplierDef, with optional
    /// per-settlement overrides on the comp properties. Pool resources are always unaffected.
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

        private static readonly string[] SeasonLabelKeys =
        {
            "UR_SeasonGrowing", "UR_SeasonOffSeason", "UR_SeasonYearRound", "UR_SeasonNoGrowing"
        };

        // Static lookup: ResourceTypeDef → SeasonalMultiplierDef, built lazily from DefDatabase.
        private static Dictionary<ResourceTypeDef, SeasonalMultiplierDef> defLookup;

        private WorldObjectCompProperties_SeasonalProduction Props
        {
            get { return (WorldObjectCompProperties_SeasonalProduction)props; }
        }

        private List<Twelfth> growingTwelfths;
        private int growingMonthCount = -1;
        private Twelfth lastCheckedTwelfth = Twelfth.Undefined;
        private SeasonState cachedSeasonState;
        private string cachedSeasonLabel;
        private const int CHECK_INTERVAL = GenDate.TicksPerDay;

        private static void EnsureDefLookup()
        {
            if (defLookup != null) return;
            defLookup = new Dictionary<ResourceTypeDef, SeasonalMultiplierDef>();
            foreach (SeasonalMultiplierDef def in DefDatabase<SeasonalMultiplierDef>.AllDefs)
            {
                if (def.resource != null)
                    defLookup[def.resource] = def;
            }
        }

        public override void CompTick()
        {
            if (Find.TickManager.TicksGame % CHECK_INTERVAL != 0)
                return;

            if (Find.WorldGrid == null)
                return;

            if (growingMonthCount < 0)
                InitGrowingSeason();

            float longitude = Find.WorldGrid.LongLatOf(parent.Tile).x;
            Twelfth currentTwelfth = GenDate.Twelfth(Find.TickManager.TicksAbs, longitude);
            if (currentTwelfth != lastCheckedTwelfth)
            {
                lastCheckedTwelfth = currentTwelfth;
                cachedSeasonState = GetSeasonState();
                cachedSeasonLabel = SeasonLabelKeys[(int)cachedSeasonState].Translate();

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

        /// <summary>
        /// Resolves the seasonal multiplier for the given resource, checking per-settlement
        /// overrides first, then global SeasonalMultiplierDefs.
        /// Returns 1.0 if no multiplier is defined for this resource.
        /// </summary>
        private double ResolveMultiplier(ResourceTypeDef resourceDef)
        {
            if (growingMonthCount < 0)
                return 1.0;

            // Per-settlement overrides take priority.
            if (Props.overrides != null)
            {
                for (int i = 0; i < Props.overrides.Count; i++)
                {
                    if (Props.overrides[i].resource == resourceDef)
                        return GetMultiplier(Props.overrides[i]);
                }
            }

            // Fall back to global def lookup.
            EnsureDefLookup();
            SeasonalMultiplierDef def;
            if (defLookup.TryGetValue(resourceDef, out def))
                return GetMultiplier(def);

            return 1.0;
        }

        private double GetMultiplier(SeasonalMultiplierEntry entry)
        {
            switch (cachedSeasonState)
            {
                case SeasonState.InGrowing: return entry.inGrowing;
                case SeasonState.OutOfGrowing: return entry.outOfGrowing;
                case SeasonState.YearRound: return entry.yearRound;
                case SeasonState.NoGrowing: return entry.noGrowing;
                default: return 1.0;
            }
        }

        private double GetMultiplier(SeasonalMultiplierDef def)
        {
            switch (cachedSeasonState)
            {
                case SeasonState.InGrowing: return def.inGrowing;
                case SeasonState.OutOfGrowing: return def.outOfGrowing;
                case SeasonState.YearRound: return def.yearRound;
                case SeasonState.NoGrowing: return def.noGrowing;
                default: return 1.0;
            }
        }

        // IResourceProductionModifier

        public double GetResourceAdditiveModifier(ResourceFC resource)
        {
            return 0;
        }

        public double GetResourceMultiplierModifier(ResourceFC resource)
        {
            if (resource.def.isPoolResource)
                return 1.0;

            return ResolveMultiplier(resource.def);
        }

        public string GetResourceModifierDesc(ResourceFC resource)
        {
            if (resource.def.isPoolResource)
                return null;

            double mult = ResolveMultiplier(resource.def);
            if (mult == 1.0 || cachedSeasonLabel == null)
                return null;

            double pct = (mult - 1.0) * 100.0;
            string sign = pct >= 0 ? "+" : "";
            return "UR_SeasonalModifier".Translate(sign + pct.ToString("F0"), cachedSeasonLabel);
        }
    }
}
