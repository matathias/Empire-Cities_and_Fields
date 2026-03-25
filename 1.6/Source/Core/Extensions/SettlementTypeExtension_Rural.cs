namespace FactionColonies.UrbanRural
{
    /// <summary>
    /// Base settlement type extension for all rural settlements.
    /// Provides step-function building slot progression: 4 slots at founding, 8 at level 5.
    /// </summary>
    public class SettlementTypeExtension_Rural : SettlementTypeExtension
    {
        public override int GetBuildingSlots(int level, int maxCount)
        {
            return level < 5 ? 4 : 8;
        }
    }
}
