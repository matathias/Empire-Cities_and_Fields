namespace FactionColonies.UrbanRural;

/// <summary>
/// Terrain-restriction checks for the rural specializations that gate on hilliness. Only the
/// rejection direction is asserted: rejection short-circuits, so it is a robust signal of our
/// CheckTerrain override regardless of the base validator's other biome/adjacency checks (the
/// acceptance direction would be confounded by those). Uses a live surface tile of the needed
/// hilliness; if the world has none, one tile's hilliness is temporarily rewritten and restored.
/// </summary>
public static class SettlementTerrainDestructiveTests
{
    // Scans the surface layer for the first tile matching the predicate; returns Invalid if none.
    private static PlanetTile FindSurfaceTile(Func<Hilliness, bool> match)
    {
        PlanetLayer surface = Find.WorldGrid.Surface;
        int count = surface.TilesCount;
        for (int i = 0; i < count; i++)
        {
            PlanetTile pt = new(i, surface);
            Tile t = pt.Tile;
            if (t is not null && match(t.hilliness))
                return pt;
        }
        return PlanetTile.Invalid;
    }

    /// <summary>
    /// Runs <paramref name="check"/> on a surface tile whose hilliness satisfies <paramref name="match"/>.
    /// When the world has no such tile, the first surface tile is set to <paramref name="forced"/>
    /// for the duration of the check and restored afterwards.
    /// </summary>
    private static void WithTerrain(Func<Hilliness, bool> match, Hilliness forced, Action<PlanetTile> check)
    {
        PlanetTile tile = FindSurfaceTile(match);
        if (tile.Valid)
        {
            check(tile);
            return;
        }

        tile = new PlanetTile(0, Find.WorldGrid.Surface);
        Tile t = TestAssert.NotNull(tile.Tile, "Surface tile 0 has no Tile data");
        Hilliness original = t.hilliness;
        t.hilliness = forced;
        try
        {
            check(tile);
        }
        finally
        {
            t.hilliness = original;
        }
    }

    private static SettlementTypeExtension ExtOf(WorldSettlementDef def, string label)
    {
        TestAssert.NotNull(def, $"{label} def failed to resolve");
        return TestAssert.NotNull(def.GetSettlementTypeExtension(), $"No {label} settlement type extension");
    }

    [EmpireDestructiveTest("CF.Destructive.Terrain")]
    public static void Farm_RejectsMountainousTerrain()
    {
        SettlementTypeExtension ext = ExtOf(CFSettlementDefOf.WorldSettlementDef_Farm, "Farm");
        WithTerrain(h => h > Hilliness.SmallHills && h != Hilliness.Impassable, Hilliness.Mountainous,
            hilly => TestAssert.IsFalse(ext.TileIsValidForTypeTransition(hilly),
                "Farm must reject terrain hillier than SmallHills"));
    }

    [EmpireDestructiveTest("CF.Destructive.Terrain")]
    public static void Ranch_RejectsMountainousTerrain()
    {
        SettlementTypeExtension ext = ExtOf(CFSettlementDefOf.WorldSettlementDef_Ranch, "Ranch");
        WithTerrain(h => h > Hilliness.SmallHills && h != Hilliness.Impassable, Hilliness.Mountainous,
            hilly => TestAssert.IsFalse(ext.TileIsValidForTypeTransition(hilly),
                "Ranch must reject terrain hillier than SmallHills"));
    }

    [EmpireDestructiveTest("CF.Destructive.Terrain")]
    public static void Mine_RejectsFlatTerrain()
    {
        SettlementTypeExtension ext = ExtOf(CFSettlementDefOf.WorldSettlementDef_Mine, "Mine");
        WithTerrain(h => h == Hilliness.Flat, Hilliness.Flat,
            flat => TestAssert.IsFalse(ext.TileIsValidForTypeTransition(flat),
                "Mine must reject flat terrain (requires at least SmallHills)"));
    }
}
