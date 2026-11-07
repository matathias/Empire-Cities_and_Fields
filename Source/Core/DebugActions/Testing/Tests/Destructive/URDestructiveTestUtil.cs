using FactionColonies.SupplyChain;

namespace FactionColonies.UrbanRural;

/// <summary>
/// Cities &amp; Fields destructive-tier helpers, layered on the base mod's
/// <see cref="DestructiveTestUtil"/>. Provides get-or-create city/rural fixtures (reusing a live
/// settlement when present, otherwise spawning a transient one the caller tears down), a stockpile
/// seeder (which forces the SupplyChain local stockpile to exist and credits into it), a worker
/// seeder, and the shared rural-classification used by the Tools coordinator.
/// </summary>
public static class URDestructiveTestUtil
{
    public static bool IsRural(WorldSettlementFC settlement)
    {
        WorldSettlementDef rural = CFSettlementDefOf.WorldSettlementDef_Rural;
        if (rural is null || settlement is null) return false;
        WorldSettlementDef def = settlement.settlementDef;
        while (def is not null)
        {
            if (def == rural) return true;
            def = def.baseSettlementType;
        }
        return false;
    }

    public static WorldSettlementFC FirstCity(FactionFC f)
    {
        return f?.settlements?.FirstOrDefault(s => s.settlementDef == CFSettlementDefOf.WorldSettlementDef_City);
    }

    public static WorldSettlementFC FirstRural(FactionFC f)
    {
        return f?.settlements?.FirstOrDefault(IsRural);
    }

    /// <summary>
    /// Returns the faction's first city, or spawns a transient one if none exists.
    /// <paramref name="created"/> is true when a new settlement was made (caller must
    /// <see cref="DestructiveTestUtil.SafeRemoveSettlement"/> it) and false when a live one was reused.
    /// Fails the test if no valid tile could be found for the new settlement.
    /// </summary>
    public static WorldSettlementFC GetOrCreateCity(FactionFC f, out bool created)
    {
        WorldSettlementFC existing = FirstCity(f);
        if (existing is not null) { created = false; return existing; }

        WorldSettlementFC made = DestructiveTestUtil.RequireTransientSettlement(CFSettlementDefOf.WorldSettlementDef_City);
        created = true;
        return made;
    }

    /// <summary>
    /// Returns the faction's first rural, or spawns a transient one if none exists. Uses the Farm
    /// type — the most placeable rural (flat terrain only, no biome/research gate). See
    /// <see cref="GetOrCreateCity"/> for the <paramref name="created"/> contract.
    /// </summary>
    public static WorldSettlementFC GetOrCreateRural(FactionFC f, out bool created)
    {
        WorldSettlementFC existing = FirstRural(f);
        if (existing is not null) { created = false; return existing; }

        WorldSettlementFC made = DestructiveTestUtil.RequireTransientSettlement(CFSettlementDefOf.WorldSettlementDef_Farm);
        created = true;
        return made;
    }

    /// <summary>
    /// Ensures the settlement has at least one assigned worker (a freshly spawned settlement starts
    /// with none), assigning up to <paramref name="count"/> across its resources. Returns the
    /// resulting total worker count; fails the test if no worker could be assigned.
    /// </summary>
    public static int EnsureWorkers(WorldSettlementFC settlement, int count)
    {
        TestAssert.NotNull(settlement, "EnsureWorkers: settlement is null");
        if (settlement.GetTotalWorkers() > 0) return settlement.GetTotalWorkers();

        foreach (ResourceFC target in settlement.Resources ?? [])
        {
            for (int i = settlement.GetTotalWorkers(); i < count; i++)
            {
                if (!settlement.IncreaseWorkers(target, 1)) break;
            }
            if (settlement.GetTotalWorkers() >= count) break;
        }

        int total = settlement.GetTotalWorkers();
        TestAssert.GreaterThan(total, 0, $"EnsureWorkers: could not assign any worker to '{settlement.Name}'");
        return total;
    }

    /// <summary>
    /// Forces the settlement's SupplyChain local stockpile to exist (so the mod's GetStockpile()
    /// reads it) and fills the resource up to its cap. Returns the amount now available. The
    /// SupplyChain per-resource cap (localCapBase, 50 by default; higher with Market/Manufactory
    /// capBonuses) clamps how much can be stored — far more than these tests consume — so callers
    /// should require a POSITIVE return, not a specific amount. Returns 0 if the settlement has no
    /// SupplyChain comp or the resource has no cap.
    /// </summary>
    public static double FillResource(WorldSettlementFC settlement, ResourceTypeDef res)
    {
        WorldObjectComp_SupplyChain comp = settlement?.GetComponent<WorldObjectComp_SupplyChain>();
        if (comp is null || res is null) return 0.0;

        IStockpile sp = comp.EnsureLocalStockpile();
        if (sp is null) return 0.0;

        // Credit clamps to the remaining space, so requesting the full cap tops the resource off.
        double cap = sp.GetCap(res);
        if (cap > 0) sp.Credit(res, cap);
        return sp.GetAmount(res);
    }

    public static double StockpileAmount(WorldSettlementFC settlement, ResourceTypeDef res)
    {
        IStockpile sp = settlement?.GetComponent<WorldObjectComp_SupplyChain>()?.GetStockpile();
        return sp is null ? 0.0 : sp.GetAmount(res);
    }

    /// <summary>The live Cities &amp; Fields world component; this mod always adds it, so a missing one fails the test.</summary>
    public static WorldComponent_UrbanRural Coordinator()
    {
        return TestAssert.NotNull(Verse.Find.World?.GetComponent<WorldComponent_UrbanRural>(),
            "No WorldComponent_UrbanRural on the world");
    }
}
