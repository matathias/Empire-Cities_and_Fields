using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FactionColonies.util;
using LudeonTK;
using RimWorld.Planet;
using Verse;

namespace FactionColonies.UrbanRural
{
    /// <summary>
    /// Dev-mode debug actions for the Cities &amp; Fields submod. Grouped under the same debug menu
    /// category as the submod's test runner.
    /// </summary>
    public static class URDebugActions
    {
        private const string DebugCategory = "Empire Refactored: Cities & Fields";
        private const int MaxTileAttempts = 500;

        /// <summary>
        /// Places one of each concrete settlement type (City + the 7 rural specializations) on random
        /// valid tiles, levels each to a random level, and staffs it up to its normal worker capacity.
        /// For a rural, half the workers go to its specialty resource and the rest are spread randomly;
        /// the City has no specialty, so all its workers are spread randomly.
        /// </summary>
        [DebugAction(DebugCategory, "Spawn one of each settlement type (leveled, staffed)",
            allowedGameStates = AllowedGameStates.Playing)]
        public static void SpawnOneOfEachSettlementType()
        {
            FactionFC faction = FindFC.FactionComp;
            if (faction is null)
            {
                LogUtil.MessageForce("Debug - FactionFC WorldComponent is null, cannot spawn settlements.");
                return;
            }

            // The 8 concrete, foundable types. WorldSettlementDef_Rural is the hidden base (available=false)
            // and is intentionally excluded.
            List<WorldSettlementDef> types = new List<WorldSettlementDef>
            {
                CFSettlementDefOf.WorldSettlementDef_City,
                CFSettlementDefOf.WorldSettlementDef_Farm,
                CFSettlementDefOf.WorldSettlementDef_Mine,
                CFSettlementDefOf.WorldSettlementDef_LumberCamp,
                CFSettlementDefOf.WorldSettlementDef_Ranch,
                CFSettlementDefOf.WorldSettlementDef_Refinery,
                CFSettlementDefOf.WorldSettlementDef_Herbalist,
                CFSettlementDefOf.WorldSettlementDef_PowerPlant,
            };

            int created = 0;
            int skipped = 0;
            StringBuilder summary = new StringBuilder();

            foreach (WorldSettlementDef def in types)
            {
                if (def is null) continue;

                PlanetTile tile = FindTileFor(def);
                if (!tile.Valid)
                {
                    LogUtil.Warning($"Spawn one of each - no placeable tile found for {def.defName}, skipping.");
                    summary.AppendLine($"  {def.defName}: SKIPPED (no tile)");
                    skipped++;
                    continue;
                }

                WorldSettlementFC s = ColonyUtil.CreatePlayerColonySettlement(tile, def);
                created++;

                LevelRandomly(s);
                AssignWorkers(s);

                summary.AppendLine($"  {def.defName}: {s.Name} L{s.settlementLevel}, {s.GetTotalWorkers()} workers");
            }

            LogUtil.MessageForce($"Debug - Spawn one of each settlement type: created {created}, skipped {skipped}.\n{summary}");
        }

        /// <summary>Finds a random surface tile valid for the given settlement type, or Invalid if none.</summary>
        private static PlanetTile FindTileFor(WorldSettlementDef def)
        {
            for (int attempt = 0; attempt < MaxTileAttempts; attempt++)
            {
                PlanetTile tile = TileFinder.RandomSettlementTileFor(Find.WorldGrid.Surface, FindFC.EmpireFaction);
                if (!tile.Valid) continue;
                if (!WorldTileChecker.IsValidTileForNewSettlement(tile, def)) continue;
                return tile;
            }
            return PlanetTile.Invalid;
        }

        /// <summary>Raises the settlement to a random level in [1, maxLevel].</summary>
        private static void LevelRandomly(WorldSettlementFC s)
        {
            int max = Math.Min(FCSettings.settlementMaxLevel, s.settlementDef.maxSettlementLevel);
            int target = Rand.RangeInclusive(1, Math.Max(1, max));
            s.UpgradeSettlement(target - s.settlementLevel);
        }

        /// <summary>
        /// Staffs the settlement up to its normal worker capacity. Half go to the settlement's primary
        /// (specialty) resource when it has one; the remainder are assigned to random resources. A City
        /// has no specialty resource, so all of its workers are assigned randomly.
        /// </summary>
        private static void AssignWorkers(WorldSettlementFC s)
        {
            // Read workersMax AFTER leveling so it reflects the settlement's new level.
            int total = (int)s.workersMax;
            if (total <= 0 || s.Resources.Count == 0) return;

            ResourceTypeDef primaryDef = s.settlementDef.resources.FirstOrDefault(r => r.multiplier > 1)?.resourceDef;
            // Match against the live resource list directly (GetResource logs when a def is absent).
            ResourceFC primary = primaryDef is null
                ? null
                : s.Resources.FirstOrDefault(r => r.def == primaryDef);

            if (primary != null)
                s.IncreaseWorkers(primary, total / 2);

            // Spread the remainder randomly across all resources (with replacement).
            while (s.GetTotalWorkers() < total)
            {
                ResourceFC r = s.Resources[Rand.Range(0, s.Resources.Count)];
                if (!s.IncreaseWorkers(r, 1))
                    break; // hit a cap
            }
        }
    }
}
