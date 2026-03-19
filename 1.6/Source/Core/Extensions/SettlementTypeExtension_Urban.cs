using System.Text;
using Verse;
using RimWorld.Planet;

namespace FactionColonies.UrbanRural
{
    /// <summary>
    /// Settlement type extension for urban (city) settlements.
    /// Restricts founding to tiles with enough rural settlements within founding range.
    /// </summary>
    public class SettlementTypeExtension_Urban : SettlementTypeExtension
    {
        private static WorldSettlementDef cachedRuralDef;

        private static WorldSettlementDef RuralDef
        {
            get
            {
                if (cachedRuralDef == null)
                {
                    cachedRuralDef = DefDatabase<WorldSettlementDef>.GetNamedSilentFail("WorldSettlementDef_Rural");
                }
                return cachedRuralDef;
            }
        }

        public override bool TileIsValidForSettlement(PlanetTile tile, StringBuilder reason = null)
        {
            if (!base.TileIsValidForSettlement(tile, reason))
            {
                return false;
            }

            FactionFC factionFC = FactionCache.FactionComp;
            if (factionFC == null)
            {
                reason?.Append("No empire faction found.");
                return false;
            }

            WorldSettlementDef ruralDef = RuralDef;
            int ruralCount = 0;
            foreach (WorldSettlementFC settlement in factionFC.settlements)
            {
                if (!IsRuralSettlement(settlement, ruralDef)) continue;

                float dist = Find.WorldGrid.ApproxDistanceInTiles(settlement.Tile, tile.tileId);
                if (dist <= FCURSettings.foundingRange)
                {
                    ruralCount++;
                }
            }

            if (ruralCount < FCURSettings.minRuralsToFound)
            {
                reason?.Append("Requires " + FCURSettings.minRuralsToFound + " rural settlements within "
                    + FCURSettings.foundingRange + " tiles (" + ruralCount + " found).");
                return false;
            }

            return true;
        }

        private static bool IsRuralSettlement(WorldSettlementFC settlement, WorldSettlementDef ruralDef)
        {
            if (ruralDef == null) return false;

            WorldSettlementDef def = settlement.settlementDef;
            while (def != null)
            {
                if (def == ruralDef) return true;
                def = def.baseSettlementType;
            }
            return false;
        }
    }
}
