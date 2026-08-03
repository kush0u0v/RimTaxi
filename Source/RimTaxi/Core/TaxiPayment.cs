using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTaxi
{
    /// <summary>
    /// Silver count/spend on maps (stockpiles) or caravan inventory.
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

        public static int CountSilver(Caravan caravan)
        {
            if (caravan == null)
            {
                return 0;
            }

            int total = 0;
            List<Thing> items = CaravanInventoryUtility.AllInventoryItems(caravan);
            for (int i = 0; i < items.Count; i++)
            {
                Thing t = items[i];
                if (t != null && t.def == ThingDefOf.Silver)
                {
                    total += t.stackCount;
                }
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

        public static bool CanAfford(Caravan caravan, int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            return CountSilver(caravan) >= amount;
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

        /// <summary>
        /// Removes silver from caravan inventory. Returns false if not enough (nothing spent).
        /// </summary>
        public static bool TryPay(Caravan caravan, int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (caravan == null || !CanAfford(caravan, amount))
            {
                return false;
            }

            int remaining = amount;
            List<Thing> taken = CaravanInventoryUtility.TakeThings(caravan, t =>
            {
                if (t == null || t.def != ThingDefOf.Silver || remaining <= 0)
                {
                    return 0;
                }

                int take = remaining < t.stackCount ? remaining : t.stackCount;
                remaining -= take;
                return take;
            });

            for (int i = 0; i < taken.Count; i++)
            {
                if (taken[i] != null && !taken[i].Destroyed)
                {
                    taken[i].Destroy(DestroyMode.Vanish);
                }
            }

            return remaining <= 0;
        }

        public static void RefundToCaravan(Caravan caravan, int amount)
        {
            if (caravan == null || amount <= 0)
            {
                return;
            }

            Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            silver.stackCount = amount;
            CaravanInventoryUtility.GiveThing(caravan, silver);
        }
    }
}
