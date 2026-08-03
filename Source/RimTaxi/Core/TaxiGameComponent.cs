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
    /// Cooldown, pending dispatch ETAs, caravan boarding sessions, and trip bookings.
    /// </summary>
    public class TaxiGameComponent : GameComponent
    {
        private int lastCallTick = -999999;
        private Dictionary<int, TaxiTripInfo> trips = new Dictionary<int, TaxiTripInfo>();
        private List<TaxiPendingDispatch> pendingDispatches = new List<TaxiPendingDispatch>();
        private List<TaxiCaravanBoarding> caravanBoardings = new List<TaxiCaravanBoarding>();

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

        public bool HasPendingDispatch(Caravan caravan)
        {
            if (caravan == null || pendingDispatches == null)
            {
                return false;
            }

            int id = caravan.ID;
            for (int i = 0; i < pendingDispatches.Count; i++)
            {
                TaxiPendingDispatch d = pendingDispatches[i];
                if (d != null && d.caravanId == id)
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

        public TaxiPendingDispatch GetPendingDispatch(Caravan caravan)
        {
            if (caravan == null || pendingDispatches == null)
            {
                return null;
            }

            int id = caravan.ID;
            for (int i = 0; i < pendingDispatches.Count; i++)
            {
                TaxiPendingDispatch d = pendingDispatches[i];
                if (d != null && d.caravanId == id)
                {
                    return d;
                }
            }

            return null;
        }

        public void QueueDispatch(Map map, IntVec3 landingCell, PlanetTile destination, int tripDistance, int callFeePaid)
        {
            if (map == null || !landingCell.IsValid)
            {
                return;
            }

            if (pendingDispatches == null)
            {
                pendingDispatches = new List<TaxiPendingDispatch>();
            }

            int delay = TaxiCallService.RollDispatchDelayTicks(tripDistance);
            pendingDispatches.Add(new TaxiPendingDispatch
            {
                mapId = map.uniqueID,
                landingCell = landingCell,
                caravanId = -1,
                destination = destination,
                tripDistance = tripDistance < 0 ? 0 : tripDistance,
                arriveGameTick = Find.TickManager.TicksGame + delay,
                callFeePaid = callFeePaid
            });

            Log.Message($"[RimTaxi] Map dispatch queued: ETA {delay} ticks, land={landingCell}");
        }

        public void QueueCaravanDispatch(Caravan caravan, int callFeePaid)
        {
            QueueCaravanDispatch(caravan, callFeePaid, PlanetTile.Invalid, 0);
        }

        public void QueueCaravanDispatch(Caravan caravan, int callFeePaid, PlanetTile destination, int tripDistance)
        {
            if (caravan == null || caravan.Destroyed)
            {
                return;
            }

            if (pendingDispatches == null)
            {
                pendingDispatches = new List<TaxiPendingDispatch>();
            }

            if (!destination.Valid)
            {
                destination = PlanetTile.Invalid;
                tripDistance = 0;
            }

            int delay = TaxiCallService.RollDispatchDelayTicks(tripDistance);
            pendingDispatches.Add(new TaxiPendingDispatch
            {
                mapId = -1,
                landingCell = IntVec3.Invalid,
                caravanId = caravan.ID,
                destination = destination,
                tripDistance = tripDistance < 0 ? 0 : tripDistance,
                arriveGameTick = Find.TickManager.TicksGame + delay,
                callFeePaid = callFeePaid
            });

            // Hold position so the taxi can meet the caravan
            TaxiCaravanUtility.StopMovementForTaxi(caravan);

            Log.Message($"[RimTaxi] Caravan dispatch queued: caravan#{caravan.ID} ETA {delay} ticks dest={destination} dist={tripDistance}");
        }

        public void BookPendingCaravanDestination(Caravan caravan, PlanetTile destination, int tripDistance)
        {
            TaxiPendingDispatch p = GetPendingDispatch(caravan);
            if (p == null)
            {
                return;
            }

            p.destination = destination;
            p.tripDistance = tripDistance < 0 ? 0 : tripDistance;
        }

        public TaxiCaravanBoarding GetBoarding(Caravan caravan)
        {
            if (caravan == null || caravanBoardings == null)
            {
                return null;
            }

            int id = caravan.ID;
            for (int i = 0; i < caravanBoardings.Count; i++)
            {
                TaxiCaravanBoarding b = caravanBoardings[i];
                if (b != null && b.caravanId == id)
                {
                    return b;
                }
            }

            return null;
        }

        public bool HasBoarding(Caravan caravan) => GetBoarding(caravan) != null;

        public void StartCaravanBoarding(Caravan caravan, int callFeePaid)
        {
            StartCaravanBoarding(caravan, callFeePaid, PlanetTile.Invalid, 0);
        }

        public void StartCaravanBoarding(Caravan caravan, int callFeePaid, PlanetTile destination, int tripDistance)
        {
            if (caravan == null)
            {
                return;
            }

            if (caravanBoardings == null)
            {
                caravanBoardings = new List<TaxiCaravanBoarding>();
            }

            // Replace any stale session for this caravan
            for (int i = caravanBoardings.Count - 1; i >= 0; i--)
            {
                if (caravanBoardings[i] != null && caravanBoardings[i].caravanId == caravan.ID)
                {
                    caravanBoardings.RemoveAt(i);
                }
            }

            int wait = RimTaxiMod.Settings?.waitTicks ?? 12500;
            if (wait < 2500)
            {
                wait = 2500;
            }

            var boarding = new TaxiCaravanBoarding
            {
                caravanId = caravan.ID,
                leaveByTick = Find.TickManager.TicksGame + wait,
                callFeePaid = callFeePaid
            };

            if (destination.Valid && tripDistance > 0)
            {
                boarding.Book(destination, tripDistance);
            }

            caravanBoardings.Add(boarding);
            TaxiCaravanUtility.StopMovementForTaxi(caravan);
        }

        public void ClearBoarding(Caravan caravan)
        {
            if (caravan == null || caravanBoardings == null)
            {
                return;
            }

            int id = caravan.ID;
            for (int i = caravanBoardings.Count - 1; i >= 0; i--)
            {
                if (caravanBoardings[i] != null && caravanBoardings[i].caravanId == id)
                {
                    caravanBoardings.RemoveAt(i);
                }
            }
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
            ProcessPendingDispatches();
            ProcessCaravanBoardings();
        }

        private void ProcessPendingDispatches()
        {
            if (pendingDispatches == null || pendingDispatches.Count == 0)
            {
                return;
            }

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

                if (d.IsCaravanPickup)
                {
                    ProcessCaravanDispatchDue(d, i);
                }
                else
                {
                    ProcessMapDispatchDue(d, i);
                }
            }
        }

        private void ProcessCaravanDispatchDue(TaxiPendingDispatch d, int index)
        {
            Caravan caravan = d.ResolveCaravan();
            if (caravan == null || caravan.Destroyed || !caravan.IsPlayerControlled)
            {
                Log.Warning("[RimTaxi] Caravan dispatch: caravan gone; dropping.");
                pendingDispatches.RemoveAt(index);
                return;
            }

            StartCaravanBoarding(caravan, d.callFeePaid, d.destination, d.tripDistance);
            pendingDispatches.RemoveAt(index);

            string letterText = d.destination.Valid
                ? "RimTaxi_LetterArrivedCaravanReadyText".Translate(d.tripDistance)
                : "RimTaxi_LetterArrivedCaravanText".Translate();

            Messages.Message(
                d.destination.Valid
                    ? "RimTaxi_TaxiArrivedAtCaravanReady".Translate()
                    : "RimTaxi_TaxiArrivedAtCaravan".Translate(),
                caravan,
                MessageTypeDefOf.PositiveEvent);
            Find.LetterStack.ReceiveLetter(
                "RimTaxi_LetterArrivedLabel".Translate(),
                letterText,
                LetterDefOf.PositiveEvent,
                caravan);

            Log.Message($"[RimTaxi] Taxi ready at caravan#{caravan.ID} tile={caravan.Tile} dest={d.destination}");
        }

        private void ProcessMapDispatchDue(TaxiPendingDispatch d, int index)
        {
            Map map = d.ResolveMap();
            if (map == null)
            {
                Log.Warning("[RimTaxi] Dispatch map gone; dropping pending taxi.");
                pendingDispatches.RemoveAt(index);
                return;
            }

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
                if (d.callFeePaid > 0)
                {
                    Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
                    silver.stackCount = d.callFeePaid;
                    GenPlace.TryPlaceThing(silver, map.Center, map, ThingPlaceMode.Near);
                }

                pendingDispatches.RemoveAt(index);
                return;
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

            pendingDispatches.RemoveAt(index);
        }

        private void ProcessCaravanBoardings()
        {
            if (caravanBoardings == null || caravanBoardings.Count == 0)
            {
                return;
            }

            for (int i = caravanBoardings.Count - 1; i >= 0; i--)
            {
                TaxiCaravanBoarding b = caravanBoardings[i];
                if (b == null)
                {
                    caravanBoardings.RemoveAt(i);
                    continue;
                }

                Caravan caravan = b.ResolveCaravan();
                if (caravan == null || caravan.Destroyed)
                {
                    caravanBoardings.RemoveAt(i);
                    continue;
                }

                if (!b.WaitExpired)
                {
                    continue;
                }

                // Wait ended: auto-depart if destination booked and silver available; else leave.
                if (b.HasDestination && TaxiCaravanUtility.PassengerCount(caravan) > 0)
                {
                    float mass = TaxiCaravanUtility.GetCaravanMass(caravan);
                    int fare = TaxiFareCalculator.TripFare(mass, b.tripDistance);
                    if (TaxiPayment.CanAfford(caravan, fare))
                    {
                        if (TaxiCallService.TryDepartCaravan(caravan, b, auto: true))
                        {
                            // boarding cleared inside TryDepartCaravan
                            continue;
                        }
                    }
                    else
                    {
                        // Extend wait once more window so player can get silver / set off
                        int wait = RimTaxiMod.Settings?.waitTicks ?? 12500;
                        if (wait < 2500)
                        {
                            wait = 2500;
                        }

                        b.leaveByTick = Find.TickManager.TicksGame + wait;
                        Messages.Message(
                            "RimTaxi_AutoDepartNeedSilver".Translate(fare, TaxiPayment.CountSilver(caravan)),
                            caravan,
                            MessageTypeDefOf.RejectInput);
                        continue;
                    }
                }

                Messages.Message(
                    "RimTaxi_CaravanTaxiLeft".Translate(),
                    caravan,
                    MessageTypeDefOf.NeutralEvent);
                caravanBoardings.RemoveAt(i);
            }
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref lastCallTick, "rimTaxiLastCallTick", -999999);
            Scribe_Collections.Look(ref trips, "rimTaxiTrips", LookMode.Value, LookMode.Deep);
            Scribe_Collections.Look(ref pendingDispatches, "rimTaxiPendingDispatches", LookMode.Deep);
            Scribe_Collections.Look(ref caravanBoardings, "rimTaxiCaravanBoardings", LookMode.Deep);
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

                if (caravanBoardings == null)
                {
                    caravanBoardings = new List<TaxiCaravanBoarding>();
                }
            }

            base.ExposeData();
        }
    }
}
