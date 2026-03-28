using System.Text;
using Verse;
using RimWorld.Planet;

namespace FactionColonies.UrbanRural
{
    /// <summary>
    /// Settlement type extension for urban (city) settlements.
    /// Uses soft founding limits — no hard rural count gate.
    /// The economic reality of zero biome production is the organic constraint.
    /// </summary>
    public class SettlementTypeExtension_Urban : SettlementTypeExtension
    {
        public override bool TileIsValidForSettlement(PlanetTile tile, StringBuilder reason = null)
        {
            if (!base.TileIsValidForSettlement(tile, reason))
            {
                return false;
            }

            reason?.Append("UR_UrbanNoResources".Translate());

            return true;
        }
    }
}
