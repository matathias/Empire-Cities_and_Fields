using FactionColonies.SupplyChain;

namespace FactionColonies.UrbanRural
{
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
            switch (level)
            {
                case 0:
                case 1:
                    return 0;
                case 2:
                case 3:
                    return 1;
                case 4:
                case 5:
                case 6:
                case 7:
                    return 4;
                default: //Levels 8 and above: 8 buildings
                    return 8;
            }
        }

        public override int GetRequiredLevelForSlot(int slotIndex, int maxCount)
        {
            switch (slotIndex)
            {
                case 0:
                    return 2;
                case 1:
                case 2:
                case 3:
                    return 4;
                default: //Slots 4 and above: need level 8
                    return 8;
            }
        }
    }
}
