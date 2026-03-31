using HarmonyLib;

namespace FactionColonies.UrbanRural
{
    /// <summary>
    /// Triggers Tools distribution from city stockpiles to nearby rurals once per tax tick,
    /// before tax calculations use the production multipliers.
    /// </summary>
    [HarmonyPatch(typeof(FactionFC))]
    [HarmonyPatch("AddTax")]
    public static class Patch_AddTax_ToolsDistribution
    {
        [HarmonyPrefix]
        public static void Prefix(FactionFC __instance)
        {
            WorldObjectComp_ToolsBenefit.RecalculateAllCities(__instance);
        }
    }
}
