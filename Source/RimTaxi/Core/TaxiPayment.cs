using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTaxi
{
    /// <summary>
    /// Silver payment rules:
    /// - Player settlement / home: stockpile (storage) silver + silver carried by player pawns.
    ///   No orbital trade beacon required.
    /// - Other maps (field/quest): silver under trade-beacon coverage + carried by player pawns.
    /// - Caravan: caravan inventory (includes carried).
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
        /// Removes payable silver. Returns false if not enough (nothing spent).
        /// Order: storage/beacon piles first, then pawn inventories.
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
            // Prefer ground/storage first (not held by pawns), then inventory
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

        /// <summary>
        /// Gather unique silver stacks that may be spent on this map.
        /// </summary>
        private static void CollectPayableSilver(Map map, List<Thing> into)
        {
            into.Clear();
            var seen = new HashSet<Thing>();

            if (IsSettlementPaymentMap(map))
            {
                // Settlement: stockpile / storage silver (no beacon required)
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
            else
            {
                // Field / temporary map: only silver in trade-beacon radius (orbital launchable)
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

            // Always: silver carried by player-controlled pawns on this map
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

                // Inventory
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

                // Carried in hands / equipment if any (rare for silver stacks)
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

            // Don't spend silver belonging to other factions / quest stuff if forbidden? Allow all player-accessible.
            if (t.Faction != null && t.Faction != Faction.OfPlayer && t.Faction.HostileTo(Faction.OfPlayer))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Silver in stockpile zones or any storage building (shelf, etc.).
        /// </summary>
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
            if (zone is Zone_Stockpile)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Prefer storage/beacon piles (spawned on map) over pawn inventory.
        /// </summary>
        private static int ComparePayPriority(Thing a, Thing b)
        {
            int pa = PayPriority(a);
            int pb = PayPriority(b);
            return pa.CompareTo(pb);
        }

        private static int PayPriority(Thing t)
        {
            if (t == null)
            {
                return 99;
            }

            // Spawned on map (storage / beacon) first
            if (t.Spawned)
            {
                return 0;
            }

            // In inventory / carried
            return 1;
        }
    }
}
