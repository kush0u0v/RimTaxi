using RimWorld.Planet;
using Verse;

namespace RimTaxi
{
    /// <summary>
    /// A paid call waiting for the taxi to arrive (dispatch ETA).
    /// Either map landing (mapId + cell) or world caravan pickup (caravanId).
    /// </summary>
    public class TaxiPendingDispatch : IExposable
    {
        public int mapId = -1;
        public IntVec3 landingCell = IntVec3.Invalid;
        public int caravanId = -1;
        public PlanetTile destination = PlanetTile.Invalid;
        public int tripDistance;
        public int arriveGameTick;
        public int callFeePaid;

        public bool IsCaravanPickup => caravanId >= 0;

        public bool IsDue => Find.TickManager.TicksGame >= arriveGameTick;

        public int TicksRemaining
        {
            get
            {
                int t = arriveGameTick - Find.TickManager.TicksGame;
                return t > 0 ? t : 0;
            }
        }

        public Map ResolveMap()
        {
            if (mapId < 0)
            {
                return null;
            }

            for (int i = 0; i < Find.Maps.Count; i++)
            {
                if (Find.Maps[i].uniqueID == mapId)
                {
                    return Find.Maps[i];
                }
            }

            return null;
        }

        public Caravan ResolveCaravan()
        {
            return TaxiCaravanUtility.FindCaravanById(caravanId);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref mapId, "mapId", -1);
            Scribe_Values.Look(ref landingCell, "landingCell", IntVec3.Invalid);
            Scribe_Values.Look(ref caravanId, "caravanId", -1);
            Scribe_Values.Look(ref destination, "destination");
            Scribe_Values.Look(ref tripDistance, "tripDistance", 0);
            Scribe_Values.Look(ref arriveGameTick, "arriveGameTick", 0);
            Scribe_Values.Look(ref callFeePaid, "callFeePaid", 0);
        }
    }
}
