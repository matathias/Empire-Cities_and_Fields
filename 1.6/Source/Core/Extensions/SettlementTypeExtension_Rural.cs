using System.Text;
using RimWorld.Planet;

namespace FactionColonies.UrbanRural
{
    /// <summary>
    /// Settlement type extension for rural settlements. Currently a type tag with default behavior.
    /// Rural settlements use standard tile validation from the base class.
    /// </summary>
    public class SettlementTypeExtension_Rural : SettlementTypeExtension
    {
        public override bool TileIsValidForSettlement(PlanetTile tile, StringBuilder reason = null)
        {
            return base.TileIsValidForSettlement(tile, reason);
        }
    }
}
