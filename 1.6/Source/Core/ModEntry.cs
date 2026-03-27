using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace FactionColonies.UrbanRural
{
    public class FCURSettings : ModSettings
    {
        private static bool printDebug = false;
        public static bool PrintDebug => printDebug;

        // Consumer Goods auto-consume settings
        public static float cgConversionRate = 2.0f;
        public static float cgMaxMitigation = 0.5f;
        public static float cgBaseWorkers = 10.0f;

        // Tools proximity settings
        public static int toolsRange = 5;
        public static float toolsCostPerRural = 1.0f;
        public static float toolsProductionBonus = 0.15f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref printDebug, "printDebug", false);
            Scribe_Values.Look(ref cgConversionRate, "cgConversionRate", 2.0f);
            Scribe_Values.Look(ref cgMaxMitigation, "cgMaxMitigation", 0.5f);
            Scribe_Values.Look(ref cgBaseWorkers, "cgBaseWorkers", 10.0f);
            Scribe_Values.Look(ref toolsRange, "toolsRange", 5);
            Scribe_Values.Look(ref toolsCostPerRural, "toolsCostPerRural", 1.0f);
            Scribe_Values.Look(ref toolsProductionBonus, "toolsProductionBonus", 0.15f);
        }

        public void DoWindowContents(Rect inRect)
        {
            Listing_Standard ls = new Listing_Standard();
            ls.Begin(inRect);

            ls.CheckboxLabeled("Enable debug logging", ref printDebug);
            ls.Gap(12f);

            ls.Label("── Consumer Goods ──");
            ls.Gap(4f);

            ls.Label("CG conversion rate (CG per penalty point): " + cgConversionRate.ToString("F1"));
            cgConversionRate = ls.Slider(cgConversionRate, 0.5f, 5.0f);

            ls.Label("Max mitigation (fraction): " + cgMaxMitigation.ToString("F2"));
            cgMaxMitigation = ls.Slider(cgMaxMitigation, 0.1f, 1.0f);

            ls.Label("Base workers for CG scaling: " + cgBaseWorkers.ToString("F0"));
            cgBaseWorkers = ls.Slider(cgBaseWorkers, 5.0f, 20.0f);

            ls.Gap(12f);
            ls.Label("── Tools & Machinery ──");
            ls.Gap(4f);

            ls.Label("Tools proximity range (tiles): " + toolsRange);
            toolsRange = (int)ls.Slider(toolsRange, 2, 15);

            ls.Label("Tools cost per rural per tick: " + toolsCostPerRural.ToString("F1"));
            toolsCostPerRural = ls.Slider(toolsCostPerRural, 0.25f, 3.0f);

            ls.Label("Production bonus from Tools: " + (toolsProductionBonus * 100f).ToString("F0") + "%");
            toolsProductionBonus = ls.Slider(toolsProductionBonus, 0.05f, 0.30f);

            ls.End();
        }
    }

    public class UrbanRuralMod : Mod
    {
        public FCURSettings settings;

        public UrbanRuralMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<FCURSettings>();
            new Harmony("empire.urbanrural").PatchAll(Assembly.GetExecutingAssembly());
        }

        public override string SettingsCategory() => "Empire Refactored: Cities & Fields";

        public override void DoSettingsWindowContents(Rect inRect) => settings.DoWindowContents(inRect);
    }
}
