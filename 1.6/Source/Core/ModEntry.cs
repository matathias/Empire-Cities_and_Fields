using UnityEngine;
using Verse;

namespace FactionColonies.UrbanRural
{
    public class FCURSettings : ModSettings
    {
        private static bool printDebug = false;
        public static bool PrintDebug => printDebug;

        public static int minRuralsToFound = 3;
        public static int maxLinkRange = 5;
        public static float linkEfficiency = 0.5f;
        public static float basePenaltyHappiness = 1.0f;
        public static float basePenaltyProsperity = 5.0f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref printDebug, "printDebug", false);
            Scribe_Values.Look(ref minRuralsToFound, "minRuralsToFound", 3);
            Scribe_Values.Look(ref maxLinkRange, "maxLinkRange", 5);
            Scribe_Values.Look(ref linkEfficiency, "linkEfficiency", 0.5f);
            Scribe_Values.Look(ref basePenaltyHappiness, "basePenaltyHappiness", 1.0f);
            Scribe_Values.Look(ref basePenaltyProsperity, "basePenaltyProsperity", 5.0f);
        }

        public void DoWindowContents(Rect inRect)
        {
            Listing_Standard ls = new Listing_Standard();
            ls.Begin(inRect);

            ls.CheckboxLabeled("Enable debug logging", ref printDebug);
            ls.Gap(12f);

            ls.Label("Minimum rural settlements to found a city: " + minRuralsToFound);
            minRuralsToFound = (int)ls.Slider(minRuralsToFound, 1, 6);

            ls.Label("Maximum link range (tiles): " + maxLinkRange);
            maxLinkRange = (int)ls.Slider(maxLinkRange, 2, 15);

            ls.Label("Link efficiency: " + linkEfficiency.ToString("P0"));
            linkEfficiency = ls.Slider(linkEfficiency, 0.1f, 1.0f);

            ls.Gap(12f);
            ls.Label("Penalty per missing link² (happiness loss): " + basePenaltyHappiness.ToString("F1"));
            basePenaltyHappiness = ls.Slider(basePenaltyHappiness, 0.1f, 5.0f);

            ls.Label("Penalty per missing link² (prosperity recovery): " + basePenaltyProsperity.ToString("F1"));
            basePenaltyProsperity = ls.Slider(basePenaltyProsperity, 1.0f, 20.0f);

            ls.End();
        }
    }

    public class UrbanRuralMod : Mod
    {
        public FCURSettings settings;

        public UrbanRuralMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<FCURSettings>();
            LifecycleRegistry.Register(new UrbanRuralLifecycleHandler());
            MainTableRegistry.Register(new NetworkOverviewTab());
        }

        public override string SettingsCategory() => "Empire - Urban & Rural";

        public override void DoSettingsWindowContents(Rect inRect) => settings.DoWindowContents(inRect);
    }
}
