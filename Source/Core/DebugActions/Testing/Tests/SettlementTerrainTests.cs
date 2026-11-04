namespace FactionColonies.UrbanRural;

/// <summary>
/// Read-only terrain-restriction checks for the rural specializations that gate on hilliness.
/// Only the rejection direction is asserted: rejection short-circuits, so it is a robust signal
/// of our CheckTerrain override regardless of the base validator's other biome/adjacency checks
/// (the acceptance direction would be confounded by those). Skips if no world is loaded or no
/// tile of the needed hilliness exists in the sample. No world mutation.
/// </summary>
public static class SettlementTerrainTests
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

    private static SettlementTypeExtension ExtOf(WorldSettlementDef def)
    {
        return def?.GetSettlementTypeExtension();
    }

    [EmpireTest("CF.Terrain")]
    public static void Farm_RejectsMountainousTerrain()
    {
        if (Find.WorldGrid is null) TestAssert.Skip("No world loaded");
        PlanetTile hilly = FindSurfaceTile(h => h > Hilliness.SmallHills && h != Hilliness.Impassable);
        if (!hilly.Valid) TestAssert.Skip("No large-hill/mountainous tile in world");

        SettlementTypeExtension ext = ExtOf(CFSettlementDefOf.WorldSettlementDef_Farm);
        if (ext is null) TestAssert.Skip("No Farm extension");
        TestAssert.IsFalse(ext.TileIsValidForTypeTransition(hilly),
            "Farm must reject terrain hillier than SmallHills");
    }

    [EmpireTest("CF.Terrain")]
    public static void Ranch_RejectsMountainousTerrain()
    {
        if (Find.WorldGrid is null) TestAssert.Skip("No world loaded");
        PlanetTile hilly = FindSurfaceTile(h => h > Hilliness.SmallHills && h != Hilliness.Impassable);
        if (!hilly.Valid) TestAssert.Skip("No large-hill/mountainous tile in world");

        SettlementTypeExtension ext = ExtOf(CFSettlementDefOf.WorldSettlementDef_Ranch);
        if (ext is null) TestAssert.Skip("No Ranch extension");
        TestAssert.IsFalse(ext.TileIsValidForTypeTransition(hilly),
            "Ranch must reject terrain hillier than SmallHills");
    }

    [EmpireTest("CF.Terrain")]
    public static void Mine_RejectsFlatTerrain()
    {
        if (Find.WorldGrid is null) TestAssert.Skip("No world loaded");
        PlanetTile flat = FindSurfaceTile(h => h == Hilliness.Flat);
        if (!flat.Valid) TestAssert.Skip("No flat tile in world");

        SettlementTypeExtension ext = ExtOf(CFSettlementDefOf.WorldSettlementDef_Mine);
        if (ext is null) TestAssert.Skip("No Mine extension");
        TestAssert.IsFalse(ext.TileIsValidForTypeTransition(flat),
            "Mine must reject flat terrain (requires at least SmallHills)");
    }
}
