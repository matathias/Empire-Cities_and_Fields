using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace FactionColonies.UrbanRural
{
    /// <summary>
    /// Faction-level overview tab showing all urban/rural settlements and their link status.
    /// Registered with MainTableRegistry and displayed in MainTabWindow_EmpireExtensions.
    /// </summary>
    public class NetworkOverviewTab : IMainTabWindowOverview
    {
        private FactionFC uiFaction;
        private Vector2 scrollPos;

        public string TabName() => "Urban/Rural Network";

        public void PreOpenWindow(FactionFC faction)
        {
            uiFaction = faction;
            scrollPos = Vector2.zero;
        }

        public void OnTabSwitch() { }

        public void PostCloseWindow()
        {
            uiFaction = null;
        }

        public void DrawOverviewTab(Rect boundingBox)
        {
            if (uiFaction == null) return;

            Rect inner = boundingBox.ContractedBy(10f);

            // Gather data
            List<UrbanInfo> cities = new List<UrbanInfo>();
            List<RuralInfo> rurals = new List<RuralInfo>();

            foreach (WorldSettlementFC s in uiFaction.settlements)
            {
                WorldObjectComp_UrbanSettlement urbanComp = s.GetComponent<WorldObjectComp_UrbanSettlement>();
                if (urbanComp != null)
                {
                    cities.Add(new UrbanInfo { settlement = s, comp = urbanComp });
                    continue;
                }

                WorldObjectComp_RuralSettlement ruralComp = s.GetComponent<WorldObjectComp_RuralSettlement>();
                if (ruralComp != null)
                {
                    rurals.Add(new RuralInfo { settlement = s, comp = ruralComp });
                }
            }

            // Calculate content height
            float contentHeight = 35f; // header
            contentHeight += 26f; // "Cities" section header
            contentHeight += cities.Count * 55f;
            if (cities.Count == 0) contentHeight += 24f;
            contentHeight += 36f; // gap + "Rural" section header
            contentHeight += rurals.Count * 30f;
            if (rurals.Count == 0) contentHeight += 24f;
            contentHeight += 20f; // padding

            Rect viewRect = new Rect(0f, 0f, inner.width - 16f, contentHeight);
            Widgets.BeginScrollView(inner, ref scrollPos, viewRect);
            float curY = 0f;

            // Header
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, curY, viewRect.width, 30f), "Urban/Rural Network Overview");
            Text.Font = GameFont.Small;
            curY += 35f;

            // Cities section
            GUI.color = new Color(0.8f, 0.8f, 1f);
            Widgets.Label(new Rect(0f, curY, viewRect.width, 24f), "Cities:");
            GUI.color = Color.white;
            curY += 26f;

            if (cities.Count == 0)
            {
                GUI.color = Color.gray;
                Widgets.Label(new Rect(10f, curY, viewRect.width - 10f, 24f), "No cities founded yet.");
                GUI.color = Color.white;
                curY += 24f;
            }
            else
            {
                foreach (UrbanInfo info in cities)
                {
                    string status = info.comp.IsFullyLinked
                        ? info.comp.LinkedCount + "/" + FCURSettings.minRuralsToFound + " linked"
                        : info.comp.LinkedCount + "/" + FCURSettings.minRuralsToFound + " linked — PENALTIES ACTIVE";

                    Color statusColor = info.comp.IsFullyLinked
                        ? new Color(0.4f, 0.8f, 0.4f)
                        : new Color(1f, 0.4f, 0.4f);

                    Widgets.Label(new Rect(10f, curY, viewRect.width - 10f, 24f), info.settlement.Name);
                    curY += 22f;

                    GUI.color = statusColor;
                    Widgets.Label(new Rect(20f, curY, viewRect.width - 20f, 22f), status);
                    GUI.color = Color.white;
                    curY += 24f;

                    // List linked rurals inline
                    Text.Font = GameFont.Tiny;
                    GUI.color = Color.gray;
                    List<string> linkedNames = new List<string>();
                    foreach (WorldSettlementFC rural in info.comp.GetLinkedRurals())
                    {
                        linkedNames.Add(rural.Name);
                    }
                    if (linkedNames.Count > 0)
                    {
                        Widgets.Label(new Rect(20f, curY, viewRect.width - 20f, 20f),
                            "Linked: " + string.Join(", ", linkedNames));
                    }
                    GUI.color = Color.white;
                    Text.Font = GameFont.Small;
                    curY += 9f;
                }
            }

            curY += 10f;

            // Rural section
            GUI.color = new Color(0.8f, 1f, 0.8f);
            Widgets.Label(new Rect(0f, curY, viewRect.width, 24f), "Rural Settlements:");
            GUI.color = Color.white;
            curY += 26f;

            if (rurals.Count == 0)
            {
                GUI.color = Color.gray;
                Widgets.Label(new Rect(10f, curY, viewRect.width - 10f, 24f),
                    "No rural settlements founded yet.");
                GUI.color = Color.white;
                curY += 24f;
            }
            else
            {
                foreach (RuralInfo info in rurals)
                {
                    string specialty = GetSpecialtyLabel(info.settlement);
                    string linkStatus;
                    Color linkColor;

                    if (info.comp.IsLinked)
                    {
                        WorldSettlementFC urban = info.comp.GetLinkedUrban();
                        linkStatus = "Linked to " + (urban != null ? urban.Name : "Unknown");
                        linkColor = new Color(0.4f, 0.8f, 0.4f);
                    }
                    else
                    {
                        linkStatus = "Unlinked";
                        linkColor = Color.gray;
                    }

                    Widgets.Label(new Rect(10f, curY, 200f, 24f),
                        info.settlement.Name + " — " + specialty);

                    GUI.color = linkColor;
                    Widgets.Label(new Rect(220f, curY, viewRect.width - 220f, 24f), linkStatus);
                    GUI.color = Color.white;

                    curY += 30f;
                }
            }

            Widgets.EndScrollView();
        }

        private static string GetSpecialtyLabel(WorldSettlementFC settlement)
        {
            if (settlement.settlementDef == null || settlement.settlementDef.resources == null)
            {
                return "Unknown";
            }

            List<string> names = new List<string>();
            foreach (ResourceAvailability ra in settlement.settlementDef.resources)
            {
                if (ra.resourceDef != null)
                {
                    names.Add(ra.resourceDef.LabelCap);
                }
            }

            return names.Count > 0 ? string.Join(", ", names) : "None";
        }

        private struct UrbanInfo
        {
            public WorldSettlementFC settlement;
            public WorldObjectComp_UrbanSettlement comp;
        }

        private struct RuralInfo
        {
            public WorldSettlementFC settlement;
            public WorldObjectComp_RuralSettlement comp;
        }
    }
}
