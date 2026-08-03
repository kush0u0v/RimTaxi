using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTaxi
{
    /// <summary>
    /// Silver payment rules (trade beacon = orbital trade beacon on player settlements):
    ///
    /// Player settlement / home (map call / depart on that map):
    ///   - With trade beacon(s): silver in beacon radius + silver carried by player pawns
    ///   - Without trade beacon: stockpile/storage silver + silver carried by player pawns
    ///
    /// Field / temp maps:
    ///   - Silver carried by player pawns only
    ///
    /// Caravan taxi (call / boarding / depart):
    ///   - Silver in trade-beacon radius on any open player settlement map
    ///   - PLUS silver the caravan is carrying
    /// </summary>
    public static class TaxiPayment
    {
        private static readonly List<Thing> tmpSilvers = new List<Thing>();

        public static bool IsSettlementPaymentMap(Map map)
        {
            if (map == null)
            {
                return false;
            }

            if (map.IsPlayerHome)
            {
                return true;
            }

            if (map.Parent is Settlement settlement
                && settlement.Faction != null
                && settlement.Faction.IsPlayer)
            {
                return true;
            }

            return false;
        }

        public static bool SettlementHasTradeBeacon(Map map)
        {
            if (map == null)
            {
                return false;
            }

            if (Building_OrbitalTradeBeacon.AllPowered(map).Any())
            {
                return true;
            }

            List<Thing> built = map.listerThings?.ThingsOfDef(ThingDefOf.OrbitalTradeBeacon);
            return built != null && built.Count > 0;
        }

        public static int CountSilver(Map map)
        {
            if (map == null)
            {
                return 0;
            }

            CollectPayableMapSilver(map, tmpSilvers);
            int total = SumStacks(tmpSilvers);
            tmpSilvers.Clear();
            return total;
        }

        /// <summary>
        /// Caravan taxi pool: caravan inventory + all open player settlements' trade-beacon silver.
        /// </summary>
        public static int CountSilver(Caravan caravan)
        {
            if (caravan == null)
            {
                return 0;
            }

            return CountCaravanInventorySilver(caravan) + CountAllSettlementBeaconSilver();
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

        public static bool TryPay(Map map, int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (map == null || !CanAfford(map, amount))
            {
                return false;
            }

            CollectPayableMapSilver(map, tmpSilvers);
            tmpSilvers.Sort(ComparePayPriority);
            bool ok = SpendFromThingList(tmpSilvers, amount);
            tmpSilvers.Clear();
            return ok;
        }

        /// <summary>
        /// Caravan taxi pay: settlement beacon silver first (all open player colonies), then caravan inventory.
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

            // 1) Settlement trade-beacon silver (open player settlement maps)
            CollectAllSettlementBeaconSilver(tmpSilvers);
            tmpSilvers.Sort(ComparePayPriority);
            remaining = SpendFromThingListPartial(tmpSilvers, remaining);
            tmpSilvers.Clear();

            if (remaining <= 0)
            {
                return true;
            }

            // 2) Caravan-carried silver
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

        public static void RefundToMap(Map map, int amount, IntVec3 near)
        {
            if (map == null || amount <= 0)
            {
                return;
            }

            Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            silver.stackCount = amount;
            if (!near.IsValid)
            {
                near = map.Center;
            }

            GenPlace.TryPlaceThing(silver, near, map, ThingPlaceMode.Near);
        }

        // ─── Map collect ─────────────────────────────────────────

        private static void CollectPayableMapSilver(Map map, List<Thing> into)
        {
            into.Clear();
            var seen = new HashSet<Thing>();

            if (IsSettlementPaymentMap(map))
            {
                if (SettlementHasTradeBeacon(map))
                {
                    AddLaunchableSilver(map, into, seen);
                }
                else
                {
                    AddStorageSilver(map, into, seen);
                }
            }

            AddPawnCarriedSilver(map, into, seen);
        }

        // ─── Caravan / settlement beacon pool ────────────────────

        private static int CountCaravanInventorySilver(Caravan caravan)
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

        /// <summary>
        /// Sum of silver in trade-beacon range on every open player settlement map.
        /// </summary>
        public static int CountAllSettlementBeaconSilver()
        {
            CollectAllSettlementBeaconSilver(tmpSilvers);
            int total = SumStacks(tmpSilvers);
            tmpSilvers.Clear();
            return total;
        }

        private static void CollectAllSettlementBeaconSilver(List<Thing> into)
        {
            into.Clear();
            var seen = new HashSet<Thing>();

            if (Find.Maps == null)
            {
                return;
            }

            for (int i = 0; i < Find.Maps.Count; i++)
            {
                Map map = Find.Maps[i];
                if (!IsSettlementPaymentMap(map) || !SettlementHasTradeBeacon(map))
                {
                    continue;
                }

                AddLaunchableSilver(map, into, seen);
            }
        }

        private static void AddLaunchableSilver(Map map, List<Thing> into, HashSet<Thing> seen)
        {
            foreach (Thing t in TradeUtility.AllLaunchableThingsForTrade(map, null))
            {
                if (t == null || t.def != ThingDefOf.Silver || !IsUsableMapSilver(t))
                {
                    continue;
                }

                if (seen.Add(t))
                {
                    into.Add(t);
                }
            }
        }

        private static void AddStorageSilver(Map map, List<Thing> into, HashSet<Thing> seen)
        {
            List<Thing> mapSilvers = map.listerThings.ThingsOfDef(ThingDefOf.Silver);
            for (int i = 0; i < mapSilvers.Count; i++)
            {
                Thing t = mapSilvers[i];
                if (!IsUsableMapSilver(t) || !IsInColonyStorage(t, map))
                {
                    continue;
                }

                if (seen.Add(t))
                {
                    into.Add(t);
                }
            }
        }

        private static void AddPawnCarriedSilver(Map map, List<Thing> into, HashSet<Thing> seen)
        {
            if (map?.mapPawns == null)
            {
                return;
            }

            List<Pawn> pawns = map.mapPawns.PawnsInFaction(Faction.OfPlayer);
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn.Dead || !pawn.Spawned || pawn.Map != map)
                {
                    continue;
                }

                if (pawn.inventory?.innerContainer != null)
                {
                    for (int j = 0; j < pawn.inventory.innerContainer.Count; j++)
                    {
                        Thing t = pawn.inventory.innerContainer[j];
                        if (t != null && t.def == ThingDefOf.Silver && seen.Add(t))
                        {
                            into.Add(t);
                        }
                    }
                }

                if (pawn.carryTracker?.CarriedThing is Thing carried
                    && carried.def == ThingDefOf.Silver
                    && seen.Add(carried))
                {
                    into.Add(carried);
                }
            }
        }

        private static int SumStacks(List<Thing> things)
        {
            int total = 0;
            for (int i = 0; i < things.Count; i++)
            {
                Thing t = things[i];
                if (t != null && !t.Destroyed)
                {
                    total += t.stackCount;
                }
            }

            return total;
        }

        private static bool SpendFromThingList(List<Thing> things, int amount)
        {
            return SpendFromThingListPartial(things, amount) <= 0;
        }

        /// <returns>Remaining amount not spent.</returns>
        private static int SpendFromThingListPartial(List<Thing> things, int remaining)
        {
            for (int i = 0; i < things.Count && remaining > 0; i++)
            {
                Thing silver = things[i];
                if (silver == null || silver.Destroyed || silver.stackCount <= 0)
                {
                    continue;
                }

                int take = remaining < silver.stackCount ? remaining : silver.stackCount;
                Thing split = silver.SplitOff(take);
                split.Destroy(DestroyMode.Vanish);
                remaining -= take;
            }

            return remaining;
        }

        private static bool IsUsableMapSilver(Thing t)
        {
            if (t == null || t.Destroyed || t.def != ThingDefOf.Silver || t.stackCount <= 0)
            {
                return false;
            }

            if (t.Faction != null && t.Faction != Faction.OfPlayer && t.Faction.HostileTo(Faction.OfPlayer))
            {
                return false;
            }

            return true;
        }

        private static bool IsInColonyStorage(Thing t, Map map)
        {
            if (t == null || !t.Spawned || t.Map != map)
            {
                return false;
            }

            if (t.IsInAnyStorage())
            {
                return true;
            }

            Zone zone = t.Position.GetZone(map);
            return zone is Zone_Stockpile;
        }

        private static int ComparePayPriority(Thing a, Thing b)
        {
            return PayPriority(a).CompareTo(PayPriority(b));
        }

        private static int PayPriority(Thing t)
        {
            if (t == null)
            {
                return 99;
            }

            if (t.Spawned)
            {
                return 0;
            }

            return 1;
        }
    }
}
