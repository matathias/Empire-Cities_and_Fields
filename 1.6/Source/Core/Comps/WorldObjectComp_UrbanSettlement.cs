using System;
using System.Collections.Generic;
using RimWorld.Planet;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionColonies.UrbanRural
{
    public class WorldObjectCompProperties_UrbanSettlement : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_UrbanSettlement()
        {
            compClass = typeof(WorldObjectComp_UrbanSettlement);
        }
    }

    public class WorldObjectComp_UrbanSettlement : WorldObjectComp,
        ISettlementWindowOverview, IResourceProductionModifier, IStatModifierProvider
    {
        private List<int> linkedRuralTileIDs = new List<int>();

        private WorldSettlementFC cachedSettlement;
        private Vector2 scrollPos;

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

        public int LinkedCount => linkedRuralTileIDs.Count;

        public int MissingLinkCount => Math.Max(0, FCURSettings.minRuralsToFound - linkedRuralTileIDs.Count);

        public bool IsFullyLinked => MissingLinkCount == 0;

        public IEnumerable<WorldSettlementFC> GetLinkedRurals()
        {
            FactionFC faction = FactionCache.FactionComp;
            if (faction == null) yield break;

            foreach (int tileID in linkedRuralTileIDs)
            {
                foreach (WorldSettlementFC s in faction.settlements)
                {
                    if (s.Tile == tileID)
                    {
                        yield return s;
                        break;
                    }
                }
            }
        }

        public void LinkRural(int ruralTileID)
        {
            if (!linkedRuralTileIDs.Contains(ruralTileID))
            {
                linkedRuralTileIDs.Add(ruralTileID);
                InvalidateCaches();
            }
        }

        public void UnlinkRural(int ruralTileID)
        {
            if (linkedRuralTileIDs.Remove(ruralTileID))
            {
                InvalidateCaches();
            }
        }

        public void CleanupDeadLinks()
        {
            FactionFC faction = FactionCache.FactionComp;
            if (faction == null) return;

            HashSet<int> validTiles = new HashSet<int>();
            foreach (WorldSettlementFC s in faction.settlements)
            {
                validTiles.Add(s.Tile);
            }

            bool removed = false;
            for (int i = linkedRuralTileIDs.Count - 1; i >= 0; i--)
            {
                if (!validTiles.Contains(linkedRuralTileIDs[i]))
                {
                    linkedRuralTileIDs.RemoveAt(i);
                    removed = true;
                }
            }

            if (removed)
            {
                InvalidateCaches();
            }
        }

        private void InvalidateCaches()
        {
            if (Settlement != null)
            {
                Settlement.InvalidateStatCache();
                Settlement.InvalidateResourceCaches();
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref linkedRuralTileIDs, "linkedRuralTileIDs", LookMode.Value);
            if (linkedRuralTileIDs == null)
            {
                linkedRuralTileIDs = new List<int>();
            }
        }

        // ── IResourceProductionModifier ──

        public double GetResourceAdditiveModifier(ResourceFC resource)
        {
            double bonus = 0;
            foreach (WorldSettlementFC rural in GetLinkedRurals())
            {
                ResourceFC ruralResource = rural.GetResource(resource.def);
                if (ruralResource == null) continue;
                bonus += ruralResource.rawTotalProduction * FCURSettings.linkEfficiency;
            }
            return bonus;
        }

        public double GetResourceMultiplierModifier(ResourceFC resource)
        {
            return 1.0;
        }

        public string GetResourceModifierDesc(ResourceFC resource)
        {
            double bonus = 0;
            int count = 0;
            foreach (WorldSettlementFC rural in GetLinkedRurals())
            {
                ResourceFC ruralResource = rural.GetResource(resource.def);
                if (ruralResource == null) continue;
                bonus += ruralResource.rawTotalProduction * FCURSettings.linkEfficiency;
                count++;
            }
            if (count == 0) return null;
            return "Linked rural supply: +" + bonus.ToString("F1") + " (" + count + " linked)";
        }

        // ── IStatModifierProvider ──

        public double GetStatModifier(FCStatDef stat)
        {
            int missing = MissingLinkCount;
            if (missing == 0) return stat.IdentityValue;

            double penaltyUnits = missing * missing;

            if (stat == FCStatDefOf.happinessLostBase)
            {
                return FCURSettings.basePenaltyHappiness * penaltyUnits;
            }
            if (stat == FCStatDefOf.prosperityBaseRecovery)
            {
                return -(FCURSettings.basePenaltyProsperity * penaltyUnits);
            }

            return stat.IdentityValue;
        }

        public string GetStatModifierDesc(FCStatDef stat)
        {
            int missing = MissingLinkCount;
            if (missing == 0) return null;

            if (stat == FCStatDefOf.happinessLostBase)
            {
                double penalty = FCURSettings.basePenaltyHappiness * missing * missing;
                return "Missing urban links (" + missing + "): +" + penalty.ToString("F1") + " happiness loss";
            }
            if (stat == FCStatDefOf.prosperityBaseRecovery)
            {
                double penalty = FCURSettings.basePenaltyProsperity * missing * missing;
                return "Missing urban links (" + missing + "): -" + penalty.ToString("F1") + " prosperity recovery";
            }

            return null;
        }

        // ── ISettlementWindowOverview ──

        private WorldSettlementFC uiSettlement;

        public string OverviewTabName() => "Urban Links";

        public void PreOpenWindow(WorldSettlementFC settlement)
        {
            uiSettlement = settlement;
            scrollPos = Vector2.zero;
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

            // Calculate content height for scroll
            int linkedCount = linkedRuralTileIDs.Count;
            List<NearbyRuralInfo> available = GetAvailableRurals();
            float contentHeight = 35f + 30f; // header + section label
            contentHeight += linkedCount * 30f;
            contentHeight += 35f; // "Available" header
            contentHeight += available.Count * 30f;
            if (available.Count == 0) contentHeight += 24f;
            contentHeight += 40f; // padding

            Rect viewRect = new Rect(0f, 0f, inner.width - 16f, contentHeight);
            Widgets.BeginScrollView(inner, ref scrollPos, viewRect);
            float curY = 0f;

            // Header
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, curY, viewRect.width, 30f), "City — Urban Links");
            Text.Font = GameFont.Small;
            curY += 35f;

            // Penalty warning
            int missing = MissingLinkCount;
            if (missing > 0)
            {
                GUI.color = new Color(1f, 0.3f, 0.3f);
                Widgets.Label(new Rect(0f, curY, viewRect.width, 24f),
                    "WARNING: " + missing + " missing link(s)! Happiness and prosperity penalties active.");
                GUI.color = Color.white;
                curY += 28f;
            }

            // Linked settlements
            Widgets.Label(new Rect(0f, curY, viewRect.width, 24f),
                "Linked Settlements (" + linkedCount + "/" + FCURSettings.minRuralsToFound + "):");
            curY += 26f;

            // Iterate a copy to avoid modification during iteration
            List<int> linkedCopy = new List<int>(linkedRuralTileIDs);
            foreach (int tileID in linkedCopy)
            {
                WorldSettlementFC rural = FindSettlementByTile(tileID);
                if (rural == null) continue;

                string specialty = GetSpecialtyLabel(rural);
                float dist = Find.WorldGrid.ApproxDistanceInTiles(Settlement.Tile, rural.Tile);
                string label = rural.Name + " — " + specialty + " (" + dist.ToString("F1") + " tiles)";

                Widgets.Label(new Rect(10f, curY, viewRect.width - 140f, 24f), label);

                if (Widgets.ButtonText(new Rect(viewRect.width - 120f, curY, 100f, 24f), "Unlink"))
                {
                    WorldObjectComp_RuralSettlement ruralComp =
                        rural.GetComponent<WorldObjectComp_RuralSettlement>();
                    if (ruralComp != null)
                    {
                        ruralComp.Unlink();
                    }
                    UnlinkRural(tileID);
                }

                curY += 30f;
            }

            curY += 10f;

            // Available settlements
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(0f, curY, viewRect.width, 24f), "Available Rural Settlements:");
            curY += 26f;

            if (available.Count == 0)
            {
                GUI.color = Color.gray;
                Widgets.Label(new Rect(10f, curY, viewRect.width - 10f, 24f),
                    "No unlinked rural settlements within " + FCURSettings.maxLinkRange + " tiles.");
                GUI.color = Color.white;
                curY += 24f;
            }
            else
            {
                foreach (NearbyRuralInfo info in available)
                {
                    string label = info.settlement.Name + " — " + info.specialty
                        + " (" + info.distance.ToString("F1") + " tiles)";

                    Widgets.Label(new Rect(10f, curY, viewRect.width - 140f, 24f), label);

                    if (Widgets.ButtonText(new Rect(viewRect.width - 120f, curY, 100f, 24f), "Link"))
                    {
                        WorldObjectComp_RuralSettlement ruralComp =
                            info.settlement.GetComponent<WorldObjectComp_RuralSettlement>();
                        if (ruralComp != null)
                        {
                            ruralComp.Link(Settlement.Tile);
                        }
                        LinkRural(info.settlement.Tile);
                    }

                    curY += 30f;
                }
            }

            Widgets.EndScrollView();
        }

        // ── Helpers ──

        private struct NearbyRuralInfo
        {
            public WorldSettlementFC settlement;
            public string specialty;
            public float distance;
        }

        private List<NearbyRuralInfo> GetAvailableRurals()
        {
            List<NearbyRuralInfo> result = new List<NearbyRuralInfo>();
            FactionFC faction = FactionCache.FactionComp;
            if (faction == null || Settlement == null) return result;

            foreach (WorldSettlementFC s in faction.settlements)
            {
                WorldObjectComp_RuralSettlement ruralComp = s.GetComponent<WorldObjectComp_RuralSettlement>();
                if (ruralComp == null) continue;
                if (ruralComp.IsLinked) continue;

                float dist = Find.WorldGrid.ApproxDistanceInTiles(Settlement.Tile, s.Tile);
                if (dist > FCURSettings.maxLinkRange) continue;

                result.Add(new NearbyRuralInfo
                {
                    settlement = s,
                    specialty = GetSpecialtyLabel(s),
                    distance = dist
                });
            }

            return result;
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

        private static WorldSettlementFC FindSettlementByTile(int tileID)
        {
            FactionFC faction = FactionCache.FactionComp;
            if (faction == null) return null;

            foreach (WorldSettlementFC s in faction.settlements)
            {
                if (s.Tile == tileID) return s;
            }
            return null;
        }
    }
}
