using System.Text;
using FactionColonies.SupplyChain;

namespace FactionColonies.UrbanRural;

/// <summary>
/// Base settlement type extension for all rural settlements.
/// Provides step-function building slot progression:
///  - 0 slots at level 1
///  - 1 slot at level 2
///  - 4 slots at level 4
///  - 8 slots at level 8
/// Optionally restricts the tile hilliness the settlement can be founded on or converted to,
/// through <see cref="minHilliness"/> and <see cref="maxHilliness"/>.
/// </summary>
public class SettlementTypeExtension_Rural : SCSettlementTypeExtension
{
    /* Terrain restriction (the defaults allow every tile) */
    public Hilliness minHilliness = Hilliness.Undefined;
    public Hilliness maxHilliness = Hilliness.Impassable;
    public string tooFlatReasonKey = "UR_RequiresHillyTerrain";
    public string tooHillyReasonKey = "UR_RequiresFlatTerrain";

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

    private bool CheckTerrain(PlanetTile tile, StringBuilder reason)
    {
        if (tile.Tile is null) return true;
        return AllowsHilliness(tile.Tile.hilliness, reason);
    }

    /* True if this type may stand on terrain of the given hilliness; otherwise appends the reason */
    internal bool AllowsHilliness(Hilliness hilliness, StringBuilder reason = null)
    {
        if (hilliness > maxHilliness)
        {
            reason?.Append(tooHillyReasonKey.Translate());
            return false;
        }
        if (hilliness < minHilliness)
        {
            reason?.Append(tooFlatReasonKey.Translate());
            return false;
        }
        return true;
    }

    public override int GetBuildingSlots(int level, int maxCount)
    {
        return level switch
        {
            0 or 1 => 0,
            2 or 3 => 1,
            4 or 5 or 6 or 7 => 4,
            _ => 8, //Levels 8 and above: 8 buildings
        };
    }

    public override int GetRequiredLevelForSlot(int slotIndex, int maxCount)
    {
        return slotIndex switch
        {
            0 => 2,
            1 or 2 or 3 => 4,
            _ => 8, //Slots 4 and above: need level 8
        };
    }
}
