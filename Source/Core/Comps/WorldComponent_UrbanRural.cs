using System.Collections.Generic;
using FactionColonies;
using RimWorld.Planet;
using Verse;

namespace FactionColonies.UrbanRural;

/// <summary>
/// Faction-wide daily hook. Runs the Tools-proximity distribution once per day via the base
/// mod's IDailyAccrualParticipant seam, after every settlement has accrued the day's production
/// and SupplyChain's per-allocation realize() deposits have landed — so the Tools a city
/// produced today are in its stockpile before nearby rurals draw on them. The resulting
/// production multiplier applies to the next daily tick (the one-day predictive lag that the
/// rest of the daily-cadence economy uses).
///
/// Ordering note: both this and SupplyChain register IDailyAccrualParticipant in FinalizeInit,
/// and DailyAccrualRegistry runs participants in registration order, which is not guaranteed
/// between two WorldComponents. If Tools is ever also a building input/need, the order in which
/// the proximity skim vs. SupplyChain's needs draw Tools is undefined. This is an accepted v1
/// edge case (Tools is rarely both); if it matters, sequence this participant after SupplyChain's.
/// </summary>
public class WorldComponent_UrbanRural : WorldComponent, IDailyAccrualParticipant
{
    private bool firstTick = true;

    /* Persisted Consumer-Goods event-buffer mitigation, keyed by "settlementID|sourceId".
     * The buffer's cg_buffer_* stat modifiers live in the non-serialized statModifiers list,
     * so we record the mitigation fraction here to re-add them on load without re-drawing CG.
     * See Patch_ConsumerGoodsBuffer. */
    private Dictionary<string, double> eventMitigations = new Dictionary<string, double>();

    public WorldComponent_UrbanRural(World world) : base(world)
    {
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref eventMitigations, "eventMitigations", LookMode.Value, LookMode.Value);
        if (eventMitigations is null) eventMitigations = new Dictionary<string, double>();
    }

    private static string MitigationKey(int settlementId, string sourceId) => settlementId + "|" + sourceId;

    public bool TryGetEventMitigation(int settlementId, string sourceId, out double mitigation)
    {
        return eventMitigations.TryGetValue(MitigationKey(settlementId, sourceId), out mitigation);
    }

    public void SetEventMitigation(int settlementId, string sourceId, double mitigation)
    {
        eventMitigations[MitigationKey(settlementId, sourceId)] = mitigation;
    }

    public void ClearEventMitigation(int settlementId, string sourceId)
    {
        eventMitigations.Remove(MitigationKey(settlementId, sourceId));
    }

    public override void FinalizeInit(bool fromLoad)
    {
        base.FinalizeInit(fromLoad);
        EmpireRegistry.Register(this);
    }

    public override void WorldComponentTick()
    {
        base.WorldComponentTick();
        if (firstTick)
        {
            firstTick = false;
            // Re-register in case a cache/registry clear ran after FinalizeInit (new-game race).
            EmpireRegistry.Register(this);
        }
    }

    public void PostDailyAccrual(FactionFC faction)
    {
        WorldObjectComp_ToolsBenefit.RecalculateAllCities(faction);
    }
}
