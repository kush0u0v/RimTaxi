using RimWorld.Planet;
using Verse;

namespace RimTaxi
{
    public class CompProperties_RimTaxiTrip : CompProperties
    {
        public CompProperties_RimTaxiTrip()
        {
            compClass = typeof(CompRimTaxiTrip);
        }
    }

    /// <summary>
    /// Stores booked destination on the shuttle Thing itself (survives load/unload).
    /// More reliable than TransportShip.loadID dictionary alone.
    /// </summary>
    public class CompRimTaxiTrip : ThingComp
    {
        public PlanetTile destination = PlanetTile.Invalid;
        public int distance;
        public bool booked;

        public void Book(PlanetTile dest, int dist)
        {
            destination = dest;
            distance = dist < 0 ? 0 : dist;
            booked = dest.Valid;
            Log.Message($"[RimTaxi] CompRimTaxiTrip booked dest={dest} dist={distance} on {parent}");
        }

        public void Clear()
        {
            booked = false;
            destination = PlanetTile.Invalid;
            distance = 0;
        }

        public bool TryGet(out PlanetTile dest, out int dist)
        {
            dest = destination;
            dist = distance;
            return booked && destination.Valid;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref destination, "rimTaxiDestination");
            Scribe_Values.Look(ref distance, "rimTaxiDistance", 0);
            Scribe_Values.Look(ref booked, "rimTaxiBooked", defaultValue: false);
        }

        public override string CompInspectStringExtra()
        {
            if (!booked || !destination.Valid)
            {
                return null;
            }

            return "RimTaxi_InspectBooked".Translate(distance);
        }
    }
}
