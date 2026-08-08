using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTaxi
{
    /// <summary>
    /// Taxi is ready at a world caravan — set destination then board/depart without opening a map.
    /// </summary>
    public class TaxiCaravanBoarding : IExposable
    {
        public int caravanId = -1;
        public int leaveByTick;
        public PlanetTile destination = PlanetTile.Invalid;
        public int tripDistance;
        public bool booked;
        public int callFeePaid;

        /// <summary>Pre-depart landing on open dest map (optional).</summary>
        public IntVec3 destLandingCell = IntVec3.Invalid;
        public int destLandingMapId = -1;
        public bool hasDestLanding;

        public bool HasDestination => booked && destination.Valid;

        public int WaitTicksRemaining
        {
            get
            {
                int t = leaveByTick - Find.TickManager.TicksGame;
                return t > 0 ? t : 0;
            }
        }

        public bool WaitExpired => Find.TickManager.TicksGame >= leaveByTick;

        public Caravan ResolveCaravan()
        {
            return TaxiCaravanUtility.FindCaravanById(caravanId);
        }

        public void Book(PlanetTile dest, int dist)
        {
            destination = dest;
            tripDistance = dist < 0 ? 0 : dist;
            booked = dest.Valid;
            ClearLanding();
        }

        public void BookLanding(Map map, IntVec3 cell)
        {
            if (map == null || !cell.IsValid)
            {
                ClearLanding();
                return;
            }

            destLandingMapId = map.uniqueID;
            destLandingCell = cell;
            hasDestLanding = true;
        }

        public void ClearLanding()
        {
            hasDestLanding = false;
            destLandingCell = IntVec3.Invalid;
            destLandingMapId = -1;
        }

        public bool TryGetLandingForMap(Map map, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (!hasDestLanding || map == null || destLandingMapId != map.uniqueID || !destLandingCell.IsValid)
            {
                return false;
            }

            cell = destLandingCell;
            return true;
        }

        public void ClearBooking()
        {
            booked = false;
            destination = PlanetTile.Invalid;
            tripDistance = 0;
            ClearLanding();
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref caravanId, "caravanId", -1);
            Scribe_Values.Look(ref leaveByTick, "leaveByTick", 0);
            Scribe_Values.Look(ref destination, "destination");
            Scribe_Values.Look(ref tripDistance, "tripDistance", 0);
            Scribe_Values.Look(ref booked, "booked", false);
            Scribe_Values.Look(ref callFeePaid, "callFeePaid", 0);
            Scribe_Values.Look(ref destLandingCell, "destLandingCell", IntVec3.Invalid);
            Scribe_Values.Look(ref destLandingMapId, "destLandingMapId", -1);
            Scribe_Values.Look(ref hasDestLanding, "hasDestLanding", false);
        }
    }
}
