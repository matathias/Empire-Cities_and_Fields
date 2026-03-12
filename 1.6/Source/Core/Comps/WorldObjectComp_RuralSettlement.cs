using RimWorld.Planet;
using UnityEngine;
using Verse;
using RimWorld;

namespace FactionColonies.UrbanRural
{
    public class WorldObjectCompProperties_RuralSettlement : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_RuralSettlement()
        {
            compClass = typeof(WorldObjectComp_RuralSettlement);
        }
    }

    public class WorldObjectComp_RuralSettlement : WorldObjectComp, ISettlementWindowOverview
    {
        private int linkedUrbanTileID = -1;

        private WorldSettlementFC cachedSettlement;
        private WorldSettlementFC cachedLinkedUrban;
        private bool linkedUrbanCacheValid;

        public WorldSettlementFC Settlement
        {
            get
            {
                if (cachedSettlement == null)
                {
                    cachedSettlement = parent as WorldSettlementFC;
                }
                return cachedSettlement;
            }
        }

        public bool IsLinked => linkedUrbanTileID != -1;

        public WorldSettlementFC GetLinkedUrban()
        {
            if (linkedUrbanTileID == -1) return null;

            if (linkedUrbanCacheValid && cachedLinkedUrban != null && cachedLinkedUrban.Tile == linkedUrbanTileID)
            {
                return cachedLinkedUrban;
            }

            FactionFC faction = FactionCache.FactionComp;
            if (faction == null) return null;

            foreach (WorldSettlementFC s in faction.settlements)
            {
                if (s.Tile == linkedUrbanTileID)
                {
                    cachedLinkedUrban = s;
                    linkedUrbanCacheValid = true;
                    return s;
                }
            }

            // Not found — settlement was destroyed, clean up
            Unlink();
            return null;
        }

        public void Link(int urbanTileID)
        {
            linkedUrbanTileID = urbanTileID;
            cachedLinkedUrban = null;
            linkedUrbanCacheValid = false;
        }

        public void Unlink()
        {
            linkedUrbanTileID = -1;
            cachedLinkedUrban = null;
            linkedUrbanCacheValid = false;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref linkedUrbanTileID, "linkedUrbanTileID", -1);
        }

        // ── ISettlementWindowOverview ──

        private WorldSettlementFC uiSettlement;

        public string OverviewTabName() => "Rural Info";

        public void PreOpenWindow(WorldSettlementFC settlement)
        {
            uiSettlement = settlement;
        }

        public void OnTabSwitch() { }

        public void PostCloseWindow()
        {
            uiSettlement = null;
        }

        public void DrawOverviewTab(Rect boundingBox)
        {
            if (uiSettlement == null) return;

            Rect inner = boundingBox.ContractedBy(10f);
            float curY = inner.y;

            // Header
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inner.x, curY, inner.width, 30f), "Rural Settlement");
            Text.Font = GameFont.Small;
            curY += 35f;

            // Specialty resources
            Widgets.Label(new Rect(inner.x, curY, inner.width, 24f), "Specialty Resources:");
            curY += 26f;

            if (uiSettlement.settlementDef != null && uiSettlement.settlementDef.resources != null)
            {
                foreach (ResourceAvailability ra in uiSettlement.settlementDef.resources)
                {
                    if (ra.resourceDef == null) continue;
                    Widgets.Label(new Rect(inner.x + 10f, curY, inner.width - 10f, 22f),
                        "- " + ra.resourceDef.LabelCap);
                    curY += 24f;
                }
            }

            curY += 10f;

            // Link status
            if (IsLinked)
            {
                WorldSettlementFC urban = GetLinkedUrban();
                string urbanName = urban != null ? urban.Name : "Unknown";

                GUI.color = new Color(0.4f, 0.8f, 0.4f);
                Widgets.Label(new Rect(inner.x, curY, inner.width, 24f),
                    "Linked to: " + urbanName);
                GUI.color = Color.white;
                curY += 28f;

                if (Widgets.ButtonText(new Rect(inner.x, curY, 120f, 30f), "Unlink"))
                {
                    if (urban != null)
                    {
                        WorldObjectComp_UrbanSettlement urbanComp =
                            urban.GetComponent<WorldObjectComp_UrbanSettlement>();
                        if (urbanComp != null)
                        {
                            urbanComp.UnlinkRural(Settlement.Tile);
                        }
                    }
                    Unlink();
                }
            }
            else
            {
                GUI.color = Color.gray;
                Widgets.Label(new Rect(inner.x, curY, inner.width, 24f), "Not linked to any city.");
                GUI.color = Color.white;
                curY += 28f;

                Text.Font = GameFont.Tiny;
                GUI.color = Color.gray;
                Widgets.Label(new Rect(inner.x, curY, inner.width, 24f),
                    "Link this settlement from a city's Urban Links tab.");
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
            }
        }
    }
}
