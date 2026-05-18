using HarmonyLib;

namespace FactionColonies.UrbanRural
{
    /// <summary>
    /// Triggers Tools distribution from city stockpiles to nearby rurals once per tax tick,
    /// before tax calculations use the production multipliers.
    /// </summary>
    [HarmonyPatch(typeof(TaxLedger))]
    [HarmonyPatch("AddTax")]
    public static class Patch_AddTax_ToolsDistribution
    {
        [HarmonyPrefix]
        public static void Prefix(FactionFC faction)
        {
            WorldObjectComp_ToolsBenefit.RecalculateAllCities(faction);
        }
    }
}
