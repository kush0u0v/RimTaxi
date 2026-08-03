using RimWorld;
using Verse;

namespace RimTaxi
{
    [DefOf]
    public static class RimTaxiDefOf
    {
        public static TransportShipDef Ship_RimTaxi;
        public static WorldObjectDef TravelingRimTaxi;
        public static ThingDef RimTaxiShuttle;
        public static ThingDef RimTaxiIncoming;
        public static ThingDef RimTaxiLeaving;

        static RimTaxiDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(RimTaxiDefOf));
        }
    }
}
