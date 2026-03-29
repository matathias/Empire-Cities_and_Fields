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

            ls.CheckboxLabeled("UR_SettingsDebugLog".Translate(), ref printDebug);
            ls.Gap(12f);

            ls.Label("UR_SectionConsumerGoods".Translate());
            ls.Gap(4f);

            GameFont prev = Text.Font;
            Text.Font = GameFont.Tiny;
            ls.Label("UR_SectionConsumerGoodsDesc".Translate());
            Text.Font = prev;
            ls.Gap(4f);

            ls.Label("UR_SettingsCGConversionRate".Translate(cgConversionRate.ToString("F1")));
            cgConversionRate = ls.Slider(cgConversionRate, 0.5f, 5.0f);

            ls.Label("UR_SettingsCGMaxMitigation".Translate(cgMaxMitigation.ToString("F2")));
            cgMaxMitigation = ls.Slider(cgMaxMitigation, 0.1f, 1.0f);

            ls.Label("UR_SettingsCGBaseWorkers".Translate(cgBaseWorkers.ToString("F0")));
            cgBaseWorkers = ls.Slider(cgBaseWorkers, 5.0f, 20.0f);

            ls.Gap(12f);
            ls.Label("UR_SectionTools".Translate());
            ls.Gap(4f);

            prev = Text.Font;
            Text.Font = GameFont.Tiny;
            ls.Label("UR_SectionToolsDesc".Translate());
            Text.Font = prev;
            ls.Gap(4f);

            ls.Label("UR_SettingsToolsRange".Translate(toolsRange.ToString()));
            toolsRange = (int)ls.Slider(toolsRange, 2, 15);

            ls.Label("UR_SettingsToolsCost".Translate(toolsCostPerRural.ToString("F1")));
            toolsCostPerRural = ls.Slider(toolsCostPerRural, 0.25f, 3.0f);

            ls.Label("UR_SettingsToolsBonus".Translate((toolsProductionBonus * 100f).ToString("F0")));
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

        public override string SettingsCategory() => "UR_SettingsCategory".Translate();

        public override void DoSettingsWindowContents(Rect inRect) => settings.DoWindowContents(inRect);
    }
}
