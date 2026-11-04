using FactionColonies.SupplyChain;

namespace FactionColonies.UrbanRural;

/// <summary>
/// Base settlement type extension for all rural settlements.
/// Provides step-function building slot progression:
///  - 0 slots at level 1
///  - 1 slot at level 2
///  - 4 slots at level 4
///  - 8 slots at level 8
/// </summary>
public class SettlementTypeExtension_Rural : SCSettlementTypeExtension
{
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
