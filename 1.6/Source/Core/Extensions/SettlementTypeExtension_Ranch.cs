using System.Text;
using RimWorld.Planet;
using Verse;

namespace FactionColonies.UrbanRural
{
    public class SettlementTypeExtension_Ranch : SettlementTypeExtension_Rural
    {
        public override bool TileIsValidForSettlement(PlanetTile tile, StringBuilder reason = null)
        {
            if (!base.TileIsValidForSettlement(tile, reason)) return false;
            if (tile.Tile != null && tile.Tile.hilliness > Hilliness.SmallHills)
            {
                reason?.Append("UR_RequiresFlatTerrain".Translate());
                return false;
            }
            return true;
        }
    }
}
