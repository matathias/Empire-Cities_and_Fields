using System.Text;
using RimWorld.Planet;

namespace FactionColonies.UrbanRural
{
    public class SettlementTypeExtension_LumberCamp : SettlementTypeExtension
    {
        public override bool TileIsValidForSettlement(PlanetTile tile, StringBuilder reason = null)
        {
            return base.TileIsValidForSettlement(tile, reason);
        }
    }
}
