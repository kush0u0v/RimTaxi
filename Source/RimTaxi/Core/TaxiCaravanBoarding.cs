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
        }

        public void ClearBooking()
        {
            booked = false;
            destination = PlanetTile.Invalid;
            tripDistance = 0;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref caravanId, "caravanId", -1);
            Scribe_Values.Look(ref leaveByTick, "leaveByTick", 0);
            Scribe_Values.Look(ref destination, "destination");
            Scribe_Values.Look(ref tripDistance, "tripDistance", 0);
            Scribe_Values.Look(ref booked, "booked", false);
            Scribe_Values.Look(ref callFeePaid, "callFeePaid", 0);
        }
    }
}
