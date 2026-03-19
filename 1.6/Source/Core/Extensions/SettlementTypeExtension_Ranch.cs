using System.Text;
using RimWorld.Planet;

namespace FactionColonies.UrbanRural
{
    public class SettlementTypeExtension_Ranch : SettlementTypeExtension
    {
        public override bool TileIsValidForSettlement(PlanetTile tile, StringBuilder reason = null)
        {
            if (!base.TileIsValidForSettlement(tile, reason)) return false;
            if (tile.Tile != null && tile.Tile.hilliness > Hilliness.SmallHills)
            {
                reason?.Append("Requires flat or gently hilly terrain.");
                return false;
            }
            return true;
        }
    }
}
