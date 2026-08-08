using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTaxi
{
    /// <summary>
    /// Resolve booked trip from shuttle Comp first, then GameComponent backup.
    /// </summary>
    public static class TaxiTripLookup
    {
        public static CompRimTaxiTrip GetComp(TransportShip ship)
        {
            return ship?.shipThing?.TryGetComp<CompRimTaxiTrip>();
        }

        public static bool TryGetTrip(TransportShip ship, out PlanetTile destination, out int distance)
        {
            destination = PlanetTile.Invalid;
            distance = 0;
            if (ship == null)
            {
                return false;
            }

            CompRimTaxiTrip comp = GetComp(ship);
            if (comp != null && comp.TryGet(out destination, out distance))
            {
                return true;
            }

            TaxiGameComponent gc = TaxiGameComponent.Get();
            if (gc != null && gc.TryGetTrip(ship.loadID, out TaxiTripInfo info) && info != null && info.destination.Valid)
            {
                destination = info.destination;
                distance = info.distance;
                // Heal missing comp booking if ship still has the thing
                comp?.Book(destination, distance);
                return true;
            }

            return false;
        }

        public static void Book(TransportShip ship, PlanetTile destination, int distance)
        {
            if (ship == null)
            {
                return;
            }

            CompRimTaxiTrip comp = GetComp(ship);
            comp?.Book(destination, distance);
            TaxiGameComponent.Get()?.RegisterTrip(ship.loadID, destination, distance);
        }

        public static void Clear(TransportShip ship)
        {
            if (ship == null)
            {
                return;
            }

            // Clear trip booking but keep pre-depart landing for arrival resolution.
            CompRimTaxiTrip comp = GetComp(ship);
            if (comp != null)
            {
                bool keepLand = comp.hasDestLanding;
                IntVec3 cell = comp.destLandingCell;
                int mapId = comp.destLandingMapId;
                comp.Clear();
                if (keepLand)
                {
                    comp.destLandingCell = cell;
                    comp.destLandingMapId = mapId;
                    comp.hasDestLanding = true;
                }
            }

            TaxiGameComponent.Get()?.ClearTrip(ship.loadID);
        }

        /// <summary>Full clear including landing (after successful map land or cancel trip).</summary>
        public static void ClearAll(TransportShip ship)
        {
            if (ship == null)
            {
                return;
            }

            GetComp(ship)?.Clear();
            TaxiGameComponent.Get()?.ClearTrip(ship.loadID);
        }
    }
}
