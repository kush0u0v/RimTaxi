using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTaxi
{
    /// <summary>
    /// Silver payment rules (trade beacon = orbital trade beacon on the settlement):
    ///
    /// Player settlement / home:
    ///   - If the map has trade beacons: silver in beacon radius + silver carried by player pawns
    ///   - If no trade beacons: stockpile/storage silver + silver carried by player pawns
    ///
    /// Field / temp maps (not a player settlement):
    ///   - Silver carried by player pawns only (settlement beacons do not apply here)
    ///
    /// Caravan: caravan inventory.
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

        /// <summary>
        /// True if this settlement map has at least one orbital trade beacon (powered or not).
        /// </summary>
        public static bool SettlementHasTradeBeacon(Map map)
        {
            if (map == null)
            {
                return false;
            }

            // Powered beacons first; also any placed beacon (unpowered still counts for rule branch)
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

            CollectPayableSilver(map, tmpSilvers);
            int total = 0;
            for (int i = 0; i < tmpSilvers.Count; i++)
            {
                Thing t = tmpSilvers[i];
                if (t != null && !t.Destroyed)
                {
                    total += t.stackCount;
                }
            }

            tmpSilvers.Clear();
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
        /// Removes payable silver. Order: ground/storage/beacon piles first, then pawn inventory.
        /// </summary>
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

            CollectPayableSilver(map, tmpSilvers);
            tmpSilvers.Sort(ComparePayPriority);

            int remaining = amount;
            for (int i = 0; i < tmpSilvers.Count && remaining > 0; i++)
            {
                Thing silver = tmpSilvers[i];
                if (silver == null || silver.Destroyed || silver.stackCount <= 0)
                {
                    continue;
                }

                int take = remaining < silver.stackCount ? remaining : silver.stackCount;
                Thing split = silver.SplitOff(take);
                split.Destroy(DestroyMode.Vanish);
                remaining -= take;
            }

            tmpSilvers.Clear();
            return remaining <= 0;
        }

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

        private static void CollectPayableSilver(Map map, List<Thing> into)
        {
            into.Clear();
            var seen = new HashSet<Thing>();

            if (IsSettlementPaymentMap(map))
            {
                if (SettlementHasTradeBeacon(map))
                {
                    // Settlement with trade beacon(s): silver in beacon coverage (orbital trade range)
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
                else
                {
                    // Settlement without trade beacon: stockpile / storage silver
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
            }
            // Field maps: no settlement trade-beacon rule — carried silver only (added below)

            // Always on any map: silver carried by player pawns
            AddPawnCarriedSilver(map, into, seen);
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
