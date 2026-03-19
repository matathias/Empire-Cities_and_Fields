using UnityEngine;
using Verse;

namespace FactionColonies.UrbanRural
{
    public class FCURSettings : ModSettings
    {
        private static bool printDebug = false;
        public static bool PrintDebug => printDebug;

        public static int minRuralsToFound = 3;
        public static int foundingRange = 5;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref printDebug, "printDebug", false);
            Scribe_Values.Look(ref minRuralsToFound, "minRuralsToFound", 3);
            Scribe_Values.Look(ref foundingRange, "foundingRange", 5);
        }

        public void DoWindowContents(Rect inRect)
        {
            Listing_Standard ls = new Listing_Standard();
            ls.Begin(inRect);

            ls.CheckboxLabeled("Enable debug logging", ref printDebug);
            ls.Gap(12f);

            ls.Label("Minimum rural settlements to found a city: " + minRuralsToFound);
            minRuralsToFound = (int)ls.Slider(minRuralsToFound, 1, 6);

            ls.Label("Maximum founding range (tiles): " + foundingRange);
            foundingRange = (int)ls.Slider(foundingRange, 2, 15);

            ls.End();
        }
    }

    public class UrbanRuralMod : Mod
    {
        public FCURSettings settings;

        public UrbanRuralMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<FCURSettings>();
        }

        public override string SettingsCategory() => "Empire - Urban & Rural";

        public override void DoSettingsWindowContents(Rect inRect) => settings.DoWindowContents(inRect);
    }
}
