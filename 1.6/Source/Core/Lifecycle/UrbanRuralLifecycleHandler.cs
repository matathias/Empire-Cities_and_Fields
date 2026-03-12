using Verse;

namespace FactionColonies.UrbanRural
{
    /// <summary>
    /// Cleans up urban/rural links when settlements are destroyed.
    /// </summary>
    public class UrbanRuralLifecycleHandler : LifecycleParticipantBase
    {
        public override void OnSettlementRemoved(WorldSettlementFC settlement)
        {
            // If a rural settlement is removed, unlink it from its urban
            WorldObjectComp_RuralSettlement ruralComp = settlement.GetComponent<WorldObjectComp_RuralSettlement>();
            if (ruralComp != null && ruralComp.IsLinked)
            {
                WorldSettlementFC urban = ruralComp.GetLinkedUrban();
                if (urban != null)
                {
                    WorldObjectComp_UrbanSettlement urbanComp =
                        urban.GetComponent<WorldObjectComp_UrbanSettlement>();
                    if (urbanComp != null)
                    {
                        urbanComp.UnlinkRural(settlement.Tile);
                    }
                }
            }

            // If an urban settlement is removed, unlink all its rurals
            WorldObjectComp_UrbanSettlement urbanSelfComp = settlement.GetComponent<WorldObjectComp_UrbanSettlement>();
            if (urbanSelfComp != null)
            {
                FactionFC faction = FactionCache.FactionComp;
                if (faction == null) return;

                foreach (WorldSettlementFC rural in urbanSelfComp.GetLinkedRurals())
                {
                    WorldObjectComp_RuralSettlement rc = rural.GetComponent<WorldObjectComp_RuralSettlement>();
                    if (rc != null)
                    {
                        rc.Unlink();
                    }
                }
            }
        }
    }
}
