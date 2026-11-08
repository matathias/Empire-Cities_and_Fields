using FactionColonies.SupplyChain;

namespace FactionColonies.UrbanRural;

/// <summary>
/// Cities &amp; Fields destructive-tier helpers, layered on the base mod's
/// <see cref="DestructiveTestUtil"/>. Provides get-or-create city/rural fixtures (reusing a live
/// settlement when present, otherwise spawning a transient one the caller tears down), a stockpile
/// seeder (which credits into the stockpile the mod reads for the settlement), a worker
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
    /// The stockpile the mod's Tools and Consumer-Goods code reads for this settlement: the shared
    /// faction pile in SupplyChain Simple mode, the settlement's own (created on demand) in Complex
    /// mode. Null when SupplyChain has no stockpile for it.
    /// </summary>
    public static IStockpile StockpileOf(WorldSettlementFC settlement)
    {
        return WorldComponent_SupplyChain.StockpileFor(settlement);
    }

    /// <summary>
    /// Fills the resource in the settlement's stockpile (see <see cref="StockpileOf"/>) up to its
    /// cap and returns the amount now available. The cap clamps how much can be stored, far more
    /// than these tests consume, so callers should require a POSITIVE return, not a specific
    /// amount. Returns 0 if there is no stockpile or the resource has no cap.
    /// </summary>
    public static double FillResource(WorldSettlementFC settlement, ResourceTypeDef res)
    {
        IStockpile sp = StockpileOf(settlement);
        if (sp is null || res is null) return 0.0;

        // Credit clamps to the remaining space, so requesting the full cap tops the resource off.
        double cap = sp.GetCap(res);
        if (cap > 0) sp.Credit(res, cap);
        return sp.GetAmount(res);
    }

    public static double StockpileAmount(WorldSettlementFC settlement, ResourceTypeDef res)
    {
        IStockpile sp = StockpileOf(settlement);
        return sp is null ? 0.0 : sp.GetAmount(res);
    }

    /// <summary>
    /// Draws every unit of the resource out of the settlement's stockpile and returns how much was
    /// taken, so the caller can put it back with <see cref="Restore"/>.
    /// </summary>
    public static double Drain(WorldSettlementFC settlement, ResourceTypeDef res)
    {
        IStockpile sp = StockpileOf(settlement);
        if (sp is null || res is null) return 0.0;
        double amount = sp.GetAmount(res);
        if (amount <= 0) return 0.0;
        sp.TryDraw(res, amount, out double drawn);
        return drawn;
    }

    /// <summary>Credits back an amount taken by <see cref="Drain"/>.</summary>
    public static void Restore(WorldSettlementFC settlement, ResourceTypeDef res, double amount)
    {
        if (amount <= 0) return;
        StockpileOf(settlement)?.Credit(res, amount);
    }

    /// <summary>The live Cities &amp; Fields world component; this mod always adds it, so a missing one fails the test.</summary>
    public static WorldComponent_UrbanRural Coordinator()
    {
        return TestAssert.NotNull(Verse.Find.World?.GetComponent<WorldComponent_UrbanRural>(),
            "No WorldComponent_UrbanRural on the world");
    }
}
