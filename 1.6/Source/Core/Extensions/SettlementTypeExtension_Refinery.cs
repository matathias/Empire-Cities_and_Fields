using System.Text;
using RimWorld.Planet;

namespace FactionColonies.UrbanRural
{
    public class SettlementTypeExtension_Refinery : SettlementTypeExtension_Rural
    {
        public override int GetCreationCost()
        {
            return base.GetCreationCost() * 2;
        }
    }
}
