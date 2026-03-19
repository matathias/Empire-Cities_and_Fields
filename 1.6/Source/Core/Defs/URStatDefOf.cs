using RimWorld;
using Verse;

namespace FactionColonies.UrbanRural
{
    [DefOf]
    public class URStatDefOf
    {
        public static FCStatDef urbanRural_linkEfficiencyBonus;

        static URStatDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(URStatDefOf));
        }
    }
}
