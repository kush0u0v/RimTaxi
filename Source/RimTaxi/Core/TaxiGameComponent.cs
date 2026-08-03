using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTaxi
{
    public class TaxiTripInfo : IExposable
    {
        public PlanetTile destination = PlanetTile.Invalid;
        public int distance;

        public void ExposeData()
        {
            Scribe_Values.Look(ref destination, "destination");
            Scribe_Values.Look(ref distance, "distance", 0);
        }
    }

    /// <summary>
    /// Cooldown, pending dispatch ETAs, and active trip bookings.
    /// </summary>
    public class TaxiGameComponent : GameComponent
    {
        private int lastCallTick = -999999;
        private Dictionary<int, TaxiTripInfo> trips = new Dictionary<int, TaxiTripInfo>();
        private List<TaxiPendingDispatch> pendingDispatches = new List<TaxiPendingDispatch>();

        public TaxiGameComponent(Game game)
        {
        }

        public static TaxiGameComponent Get()
        {
            return Current.Game?.GetComponent<TaxiGameComponent>();
        }

        public bool OnCooldown
        {
            get
            {
                int cooldown = RimTaxiMod.Settings?.cooldownTicks ?? 0;
                if (cooldown <= 0)
                {
                    return false;
                }

                return Find.TickManager.TicksGame < lastCallTick + cooldown;
            }
        }

        public int CooldownTicksRemaining
        {
            get
            {
                int cooldown = RimTaxiMod.Settings?.cooldownTicks ?? 0;
                if (cooldown <= 0)
                {
                    return 0;
                }

                int remaining = lastCallTick + cooldown - Find.TickManager.TicksGame;
                return remaining > 0 ? remaining : 0;
            }
        }

        public void NotifyCalled()
        {
            lastCallTick = Find.TickManager.TicksGame;
        }

        public bool HasPendingDispatch(Map map)
        {
            if (map == null || pendingDispatches == null)
            {
                return false;
            }

            int id = map.uniqueID;
            for (int i = 0; i < pendingDispatches.Count; i++)
            {
                if (pendingDispatches[i] != null && pendingDispatches[i].mapId == id)
                {
                    return true;
                }
            }

            return false;
        }

        public TaxiPendingDispatch GetPendingDispatch(Map map)
        {
            if (map == null || pendingDispatches == null)
            {
                return null;
            }

            int id = map.uniqueID;
            for (int i = 0; i < pendingDispatches.Count; i++)
            {
                TaxiPendingDispatch d = pendingDispatches[i];
                if (d != null && d.mapId == id)
                {
                    return d;
                }
            }

            return null;
        }

        public void QueueDispatch(Map map, IntVec3 landingCell, PlanetTile destination, int tripDistance, int callFeePaid)
        {
            // Destination may be invalid at call time (chosen later when departing).
            if (map == null || !landingCell.IsValid)
            {
                return;
            }

            if (pendingDispatches == null)
            {
                pendingDispatches = new List<TaxiPendingDispatch>();
            }

            int delay = TaxiCallService.RollDispatchDelayTicks(tripDistance);
            var dispatch = new TaxiPendingDispatch
            {
                mapId = map.uniqueID,
                landingCell = landingCell,
                destination = destination,
                tripDistance = tripDistance < 0 ? 0 : tripDistance,
                arriveGameTick = Find.TickManager.TicksGame + delay,
                callFeePaid = callFeePaid
            };
            pendingDispatches.Add(dispatch);

            Log.Message($"[RimTaxi] Dispatch queued: ETA {delay} ticks (~{delay / 2500f:0.0}h), land={landingCell}, dest={destination}");
        }

        public void RegisterTrip(int transportShipLoadId, PlanetTile destination, int distance)
        {
            if (transportShipLoadId < 0 || !destination.Valid)
            {
                return;
            }

            if (trips == null)
            {
                trips = new Dictionary<int, TaxiTripInfo>();
            }

            trips[transportShipLoadId] = new TaxiTripInfo
            {
                destination = destination,
                distance = distance < 0 ? 0 : distance
            };
        }

        public bool TryGetTrip(int transportShipLoadId, out TaxiTripInfo info)
        {
            info = null;
            if (trips == null || transportShipLoadId < 0)
            {
                return false;
            }

            if (!trips.TryGetValue(transportShipLoadId, out info) || info == null)
            {
                return false;
            }

            return info.destination.Valid;
        }

        public bool TryGetTripDestination(int transportShipLoadId, out PlanetTile destination)
        {
            destination = PlanetTile.Invalid;
            if (!TryGetTrip(transportShipLoadId, out TaxiTripInfo info))
            {
                return false;
            }

            destination = info.destination;
            return true;
        }

        public void ClearTrip(int transportShipLoadId)
        {
            trips?.Remove(transportShipLoadId);
        }

        public override void GameComponentTick()
        {
            if (pendingDispatches == null || pendingDispatches.Count == 0)
            {
                return;
            }

            // Process due dispatches
            for (int i = pendingDispatches.Count - 1; i >= 0; i--)
            {
                TaxiPendingDispatch d = pendingDispatches[i];
                if (d == null)
                {
                    pendingDispatches.RemoveAt(i);
                    continue;
                }

                if (!d.IsDue)
                {
                    continue;
                }

                Map map = d.ResolveMap();
                if (map == null)
                {
                    Log.Warning("[RimTaxi] Dispatch map gone; dropping pending taxi.");
                    pendingDispatches.RemoveAt(i);
                    continue;
                }

                // Landing cell may have become blocked — try original, else nearby.
                IntVec3 cell = d.landingCell;
                if (!TaxiLandingUtility.CanLandHere(cell, map).Accepted)
                {
                    bool found = CellFinder.TryFindRandomCellNear(
                        d.landingCell,
                        map,
                        12,
                        c => TaxiLandingUtility.CanLandHere(c, map).Accepted,
                        out cell);
                    if (!found)
                    {
                        CellFinder.TryFindRandomCell(map, c => TaxiLandingUtility.CanLandHere(c, map).Accepted, out cell);
                    }
                }

                if (!cell.IsValid || !TaxiLandingUtility.CanLandHere(cell, map).Accepted)
                {
                    Messages.Message(
                        "RimTaxi_DispatchLandingFailed".Translate(),
                        MessageTypeDefOf.NegativeEvent);
                    // Refund call fee if possible
                    if (d.callFeePaid > 0)
                    {
                        Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
                        silver.stackCount = d.callFeePaid;
                        GenPlace.TryPlaceThing(silver, map.Center, map, ThingPlaceMode.Near);
                    }

                    pendingDispatches.RemoveAt(i);
                    continue;
                }

                if (TaxiCallService.SpawnTaxi(map, cell, d.destination, d.tripDistance))
                {
                    Messages.Message(
                        "RimTaxi_TaxiArrivedAtColony".Translate(),
                        new LookTargets(cell, map),
                        MessageTypeDefOf.PositiveEvent);
                    Find.LetterStack.ReceiveLetter(
                        "RimTaxi_LetterArrivedLabel".Translate(),
                        "RimTaxi_LetterArrivedTextNoDest".Translate(),
                        LetterDefOf.PositiveEvent,
                        new LookTargets(cell, map));
                    Log.Message($"[RimTaxi] Dispatch arrived at {cell} on map {map}");
                }
                else
                {
                    Messages.Message("RimTaxi_SpawnFailed".Translate(), MessageTypeDefOf.NegativeEvent);
                    if (d.callFeePaid > 0)
                    {
                        Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
                        silver.stackCount = d.callFeePaid;
                        GenPlace.TryPlaceThing(silver, map.Center, map, ThingPlaceMode.Near);
                    }
                }

                pendingDispatches.RemoveAt(i);
            }
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref lastCallTick, "rimTaxiLastCallTick", -999999);
            Scribe_Collections.Look(ref trips, "rimTaxiTrips", LookMode.Value, LookMode.Deep);
            Scribe_Collections.Look(ref pendingDispatches, "rimTaxiPendingDispatches", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (trips == null)
                {
                    trips = new Dictionary<int, TaxiTripInfo>();
                }

                if (pendingDispatches == null)
                {
                    pendingDispatches = new List<TaxiPendingDispatch>();
                }
            }

            base.ExposeData();
        }
    }
}
