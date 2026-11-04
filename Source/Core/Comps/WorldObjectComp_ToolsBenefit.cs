namespace FactionColonies.UrbanRural;

public class WorldObjectCompProperties_ToolsBenefit : WorldObjectCompProperties
{
    public WorldObjectCompProperties_ToolsBenefit()
    {
        compClass = typeof(WorldObjectComp_ToolsBenefit);
    }
}

/// <summary>
/// Attached to rural settlement defs. Checks nearby cities for Tools stockpile and applies
/// a production multiplier. Tools are consumed from the city each day.
/// Uses a static coordinator so multiple rurals near the same city split Tools fairly.
/// Triggered daily by WorldComponent_UrbanRural via the base mod's IDailyAccrualParticipant
/// seam (after production has accrued and SupplyChain deposits have landed).
/// </summary>
public class WorldObjectComp_ToolsBenefit : WorldObjectComp, IResourceProductionModifier
{
    private double cachedMultiplier = 1.0;
    private string cachedCityName;

    // Static coordinator: prevents multiple rurals from independently draining the same city.
    // Key = city tile (PlanetTile preserves orbit/surface layer), Value = per-rural multiplier fraction for the current day.
    private static Dictionary<PlanetTile, double> cityMultiplierCache = new Dictionary<PlanetTile, double>();
    private static Dictionary<PlanetTile, string> cityNameCache = new Dictionary<PlanetTile, string>();

    public override void PostExposeData()
    {
        base.PostExposeData();
        // Persist the cached multiplier so a rural keeps its Tools bonus immediately on load,
        // rather than reverting to 1.0 until the next daily RecalculateAllCities pass.
        Scribe_Values.Look(ref cachedMultiplier, "cachedMultiplier", 1.0);
        Scribe_Values.Look(ref cachedCityName, "cachedCityName");
    }

    /// <summary>
    /// Runs once per day (called from WorldComponent_UrbanRural.PostDailyAccrual).
    /// Assigns each rural to its single nearest in-range city, then bills each city once for
    /// only its assigned rurals (so a rural in overlapping city ranges is never double-billed),
    /// consumes Tools from that city's stockpile, and updates cached per-rural multipliers.
    /// </summary>
    public static void RecalculateAllCities(FactionFC faction)
    {
        if (faction is null || Find.WorldGrid is null) return;

        cityMultiplierCache.Clear();
        cityNameCache.Clear();

        WorldSettlementDef cityDef = CFSettlementDefOf.WorldSettlementDef_City;
        ResourceTypeDef toolsDef = CFResourceDefOf.RTD_Tools;
        if (cityDef is null || toolsDef is null) return;

        float toolsRange = FCURSettings.toolsRange;
        float costPerRural = FCURSettings.toolsCostPerRural;

        // Collect cities.
        List<WorldSettlementFC> cities = new List<WorldSettlementFC>();
        foreach (WorldSettlementFC settlement in faction.settlements)
        {
            if (settlement.settlementDef == cityDef)
                cities.Add(settlement);
        }

        // Assign each rural to its single nearest in-range city, tallying per-city demand.
        // A rural counts toward exactly one city, so overlapping ranges never double-bill.
        Dictionary<WorldSettlementFC, WorldSettlementFC> ruralAssignment = new Dictionary<WorldSettlementFC, WorldSettlementFC>();
        Dictionary<WorldSettlementFC, int> assignedRuralCount = new Dictionary<WorldSettlementFC, int>();
        foreach (WorldSettlementFC rural in faction.settlements)
        {
            if (!IsRuralSettlement(rural)) continue;

            WorldSettlementFC nearestCity = null;
            float bestDist = float.MaxValue;
            foreach (WorldSettlementFC city in cities)
            {
                float dist = Find.WorldGrid.ApproxDistanceInTiles(rural.Tile, city.Tile);
                if (dist <= toolsRange && dist < bestDist)
                {
                    bestDist = dist;
                    nearestCity = city;
                }
            }

            if (nearestCity is null) continue;

            ruralAssignment[rural] = nearestCity;
            assignedRuralCount.TryGetValue(nearestCity, out int count);
            assignedRuralCount[nearestCity] = count + 1;
        }

        // Bill each city once from its assigned rurals only, and cache the resulting fraction.
        foreach (WorldSettlementFC city in cities)
        {
            if (!assignedRuralCount.TryGetValue(city, out int count) || count == 0) continue;

            double available = GetToolsStockpile(city, toolsDef);
            double totalDemand = count * costPerRural;

            // Fraction of each rural's entitlement the city can actually cover (0.0 to 1.0).
            double fraction = ToolsDistributionMath.Coverage(available, totalDemand);

            double toConsume = ToolsDistributionMath.ToConsume(available, totalDemand);
            if (toConsume > 0)
                ConsumeFromStockpile(city, toolsDef, toConsume);

            cityMultiplierCache[city.Tile] = fraction;
            cityNameCache[city.Tile] = city.Name;
        }

        // Apply cached multipliers to each rural from its assigned city.
        foreach (WorldSettlementFC rural in faction.settlements)
        {
            if (!IsRuralSettlement(rural)) continue;

            WorldObjectComp_ToolsBenefit comp = rural.GetComponent<WorldObjectComp_ToolsBenefit>();
            if (comp is null) continue;

            double oldMultiplier = comp.cachedMultiplier;
            if (ruralAssignment.TryGetValue(rural, out WorldSettlementFC assignedCity)
                && cityMultiplierCache.TryGetValue(assignedCity.Tile, out double mult))
            {
                comp.cachedMultiplier = ToolsDistributionMath.Multiplier(mult, FCURSettings.toolsProductionBonus);
                cityNameCache.TryGetValue(assignedCity.Tile, out string cityName);
                comp.cachedCityName = cityName;
            }
            else
            {
                comp.cachedMultiplier = 1.0;
                comp.cachedCityName = null;
            }

            if (comp.cachedMultiplier != oldMultiplier)
                rural.InvalidateResourceCaches();
        }
    }

    private static bool IsRuralSettlement(WorldSettlementFC settlement)
    {
        WorldSettlementDef ruralDef = CFSettlementDefOf.WorldSettlementDef_Rural;
        if (ruralDef is null) return false;

        WorldSettlementDef def = settlement.settlementDef;
        while (def is not null)
        {
            if (def == ruralDef) return true;
            def = def.baseSettlementType;
        }
        return false;
    }

    private static double GetToolsStockpile(WorldSettlementFC settlement, ResourceTypeDef toolsDef)
    {
        var comp = settlement.GetComponent<SupplyChain.WorldObjectComp_SupplyChain>();
        if (comp is null) return 0;

        var stockpile = comp.GetStockpile();
        if (stockpile is null) return 0;

        return stockpile.GetAmount(toolsDef);
    }

    private static void ConsumeFromStockpile(WorldSettlementFC settlement, ResourceTypeDef toolsDef, double amount)
    {
        var comp = settlement.GetComponent<SupplyChain.WorldObjectComp_SupplyChain>();
        if (comp is null) return;

        var stockpile = comp.GetStockpile();
        if (stockpile is null) return;

        stockpile.TryDraw(toolsDef, amount, out _);
    }

    // IResourceProductionModifier
    public double GetResourceAdditiveModifier(ResourceFC resource)
    {
        return 0;
    }

    public double GetResourceMultiplierModifier(ResourceFC resource)
    {
        return cachedMultiplier;
    }

    public string GetResourceAdditiveDesc(ResourceFC resource)
    {
        return null;
    }

    public string GetResourceMultiplierDesc(ResourceFC resource)
    {
        if (cachedMultiplier <= 1.0 || cachedCityName is null)
            return null;

        double pct = (cachedMultiplier - 1.0) * 100.0;
        return "UR_ToolsModifier".Translate(pct.ToString("F0"), cachedCityName);
    }
}
