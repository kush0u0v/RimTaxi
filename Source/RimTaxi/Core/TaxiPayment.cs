using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimTaxi
{
    /// <summary>
    /// Colony silver count/spend (stockpiles on map — not orbital-beacon launchable only).
    /// </summary>
    public static class TaxiPayment
    {
        public static int CountSilver(Map map)
        {
            if (map == null)
            {
                return 0;
            }

            int total = 0;
            List<Thing> silvers = map.listerThings.ThingsOfDef(ThingDefOf.Silver);
            for (int i = 0; i < silvers.Count; i++)
            {
                total += silvers[i].stackCount;
            }

            return total;
        }

        public static bool CanAfford(Map map, int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            return CountSilver(map) >= amount;
        }

        /// <summary>
        /// Removes silver from the map. Returns false if not enough (nothing spent).
        /// </summary>
        public static bool TryPay(Map map, int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (!CanAfford(map, amount))
            {
                return false;
            }

            int remaining = amount;
            // Copy list — destroying stacks mutates listerThings.
            List<Thing> silvers = new List<Thing>(map.listerThings.ThingsOfDef(ThingDefOf.Silver));
            for (int i = 0; i < silvers.Count && remaining > 0; i++)
            {
                Thing silver = silvers[i];
                if (silver.Destroyed || silver.stackCount <= 0)
                {
                    continue;
                }

                int take = remaining < silver.stackCount ? remaining : silver.stackCount;
                silver.SplitOff(take).Destroy(DestroyMode.Vanish);
                remaining -= take;
            }

            return remaining <= 0;
        }
    }
}
