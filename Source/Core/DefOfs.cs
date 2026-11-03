using RimWorld;

namespace FactionColonies.UrbanRural
{
    [DefOf]
    public class CFSettlementDefOf
    {
        public static WorldSettlementDef WorldSettlementDef_City;
        public static WorldSettlementDef WorldSettlementDef_Rural;
        public static WorldSettlementDef WorldSettlementDef_Farm;
        public static WorldSettlementDef WorldSettlementDef_Mine;
        public static WorldSettlementDef WorldSettlementDef_LumberCamp;
        public static WorldSettlementDef WorldSettlementDef_Ranch;
        public static WorldSettlementDef WorldSettlementDef_Refinery;
        public static WorldSettlementDef WorldSettlementDef_Herbalist;
        public static WorldSettlementDef WorldSettlementDef_PowerPlant;
            
        static CFSettlementDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(CFSettlementDefOf));
        }
    }

    [DefOf]
    public class CFResourceDefOf
    {
        public static ResourceTypeDef RTD_ConsumerGoods;
        public static ResourceTypeDef RTD_Tools;
        
        static CFResourceDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(CFResourceDefOf));
        }
    }
}