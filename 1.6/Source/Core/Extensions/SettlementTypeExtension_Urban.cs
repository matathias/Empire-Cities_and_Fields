using System.Text;
using Verse;
using RimWorld.Planet;

namespace FactionColonies.UrbanRural
{
    /// <summary>
    /// Settlement type extension for urban (city) settlements.
    /// Restricts founding to tiles with enough rural settlements within link range.
    /// </summary>
    public class SettlementTypeExtension_Urban : SettlementTypeExtension
    {
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

            int ruralCount = 0;
            foreach (WorldSettlementFC settlement in factionFC.settlements)
            {
                if (settlement.GetComponent<WorldObjectComp_RuralSettlement>() == null) continue;

                float dist = Find.WorldGrid.ApproxDistanceInTiles(settlement.Tile, tile.tileId);
                if (dist <= FCURSettings.maxLinkRange)
                {
                    ruralCount++;
                }
            }

            if (ruralCount < FCURSettings.minRuralsToFound)
            {
                reason?.Append("Requires " + FCURSettings.minRuralsToFound + " rural settlements within "
                    + FCURSettings.maxLinkRange + " tiles (" + ruralCount + " found).");
                return false;
            }

            return true;
        }
    }
}
