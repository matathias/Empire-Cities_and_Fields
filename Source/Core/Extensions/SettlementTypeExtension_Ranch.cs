using System.Text;

namespace FactionColonies.UrbanRural;

public class SettlementTypeExtension_Ranch : SettlementTypeExtension_Rural
{
    public override bool TileIsValidForSettlement(PlanetTile tile, StringBuilder reason = null)
    {
        if (!base.TileIsValidForSettlement(tile, reason)) return false;
        return CheckTerrain(tile, reason);
    }

    public override bool TileIsValidForTypeTransition(PlanetTile tile, StringBuilder reason = null)
    {
        if (!base.TileIsValidForTypeTransition(tile, reason)) return false;
        return CheckTerrain(tile, reason);
    }

    private static bool CheckTerrain(PlanetTile tile, StringBuilder reason)
    {
        if (tile.Tile != null && tile.Tile.hilliness > Hilliness.SmallHills)
        {
            reason?.Append("UR_RequiresFlatTerrain".Translate());
            return false;
        }
        return true;
    }
}
