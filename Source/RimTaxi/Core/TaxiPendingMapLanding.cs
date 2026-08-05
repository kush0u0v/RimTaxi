using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimTaxi
{
    /// <summary>
    /// Flight arrived on a player map; player must pick landing cell + Q/E rot while map is visible.
    /// </summary>
    public class TaxiPendingMapLanding : IExposable
    {
        public int mapId = -1;
        public List<ActiveTransporterInfo> transporters = new List<ActiveTransporterInfo>();
        public int createdTick;

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

        public void ExposeData()
        {
            Scribe_Values.Look(ref mapId, "mapId", -1);
            Scribe_Values.Look(ref createdTick, "createdTick", 0);
            Scribe_Collections.Look(ref transporters, "transporters", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && transporters == null)
            {
                transporters = new List<ActiveTransporterInfo>();
            }
        }
    }
}
