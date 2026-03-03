using UnityEngine;
using Verse;

namespace FactionColonies.UrbanRural
{
    public class UrbanRuralSettings : ModSettings
    {
        private static bool printDebug = false;
        public static bool PrintDebug => printDebug;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref printDebug, "printDebug", false);
        }

        public void DoWindowContents(Rect inRect)
        {
            Listing_Standard ls = new Listing_Standard();
            ls.Begin(inRect);
            ls.CheckboxLabeled("Enable debug logging", ref printDebug);
            ls.End();
        }
    }

    public class UrbanRuralMod : Mod
    {
        public UrbanRuralSettings settings;

        public UrbanRuralMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<UrbanRuralSettings>();
        }

        public override string SettingsCategory() => "Empire - Urban & Rural";

        public override void DoSettingsWindowContents(Rect inRect) => settings.DoWindowContents(inRect);
    }
}
