using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimTaxi
{
    /// <summary>
    /// Flow:
    /// 1) Call (200 silver) → 2) Dispatch ETA → 3) Arrive at pickup
    /// 4) Set destination (book; fare = mass×distance shown)
    /// 5) Depart (charge trip fare + fly)
    /// </summary>
    public static class TaxiCallService
    {
        public static TaxiGameComponent Comp => Current.Game?.GetComponent<TaxiGameComponent>();

        public static int CallFee => TaxiFareCalculator.CallFee;

        public static int MaxDistance => RimTaxiMod.Settings?.maxLaunchDistance ?? 70;

        public static int RollDispatchDelayTicks(int tripDistance)
        {
            TaxiSettings s = RimTaxiMod.Settings;
            int bas = s?.dispatchBaseTicks ?? 2500;
            int variance = s?.dispatchVarianceTicks ?? 5000;
            int perTile = s?.dispatchTicksPerTripTile ?? 0;
            if (bas < 0)
            {
                bas = 0;
            }

            if (variance < 0)
            {
                variance = 0;
            }

            if (tripDistance < 0)
            {
                tripDistance = 0;
            }

            int delay = bas + (perTile * tripDistance);
            if (variance > 0)
            {
                delay += Rand.RangeInclusive(0, variance);
            }

            if (delay > 0 && delay < 1250)
            {
                delay = 1250;
            }

            return delay;
        }

        public static string GetBlockedReason(Map callMap, Building_CommsConsole console = null, int? requiredSilver = null)
        {
            if (callMap == null || !callMap.IsPlayerHome)
            {
                return "RimTaxi_NotPlayerHome".Translate();
            }

            if (callMap.generatorDef != null && callMap.generatorDef.isUnderground)
            {
                return "RimTaxi_MapUnreachable".Translate();
            }

            if (console != null && !console.CanUseCommsNow)
            {
                if (console.Spawned && console.Map.gameConditionManager.ElectricityDisabled(console.Map))
                {
                    return "CannotUseSolarFlare".Translate();
                }

                CompPowerTrader power = console.TryGetComp<CompPowerTrader>();
                if (power != null && !power.PowerOn)
                {
                    return "CannotUseNoPower".Translate();
                }

                return "RimTaxi_CommsUnavailable".Translate();
            }

            TaxiGameComponent taxiComp = Comp;
            if (taxiComp != null && taxiComp.OnCooldown)
            {
                return "RimTaxi_OnCooldown".Translate(taxiComp.CooldownTicksRemaining.ToStringTicksToPeriod());
            }

            int need = requiredSilver ?? CallFee;
            if (!TaxiPayment.CanAfford(callMap, need))
            {
                return "RimTaxi_NeedSilver".Translate(need, TaxiPayment.CountSilver(callMap));
            }

            if (RimTaxiDefOf.Ship_RimTaxi == null || RimTaxiDefOf.RimTaxiShuttle == null)
            {
                return "RimTaxi_DefsMissing".Translate();
            }

            return null;
        }

        public static Command_Action MakeCallGizmo(Building_CommsConsole console)
        {
            Command_Action cmd = new Command_Action
            {
                defaultLabel = "RimTaxi_CallOptionWithFee".Translate(CallFee),
                defaultDesc = "RimTaxi_CallGizmoDesc".Translate(CallFee, TaxiFareCalculator.FarePerKgPerTile.ToString("0.00")),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/CallShuttle", reportFailure: false)
                    ?? TexCommand.Attack,
                action = () => BeginCallFromConsole(console, null)
            };

            string blocked = GetBlockedReason(console?.Map, console);
            if (blocked != null)
            {
                cmd.Disable(blocked);
            }

            return cmd;
        }

        public static FloatMenuOption MakeCallFloatMenuOption(Pawn pawn, Building_CommsConsole console)
        {
            string label = "RimTaxi_CallOptionWithFee".Translate(CallFee);

            string blocked = GetBlockedReason(console?.Map, console);
            if (blocked != null)
            {
                return new FloatMenuOption(label + ": " + blocked, null);
            }

            if (pawn != null && !pawn.CanReach(console, PathEndMode.InteractionCell, Danger.Deadly))
            {
                return new FloatMenuOption(label + ": " + "NoPath".Translate().CapitalizeFirst(), null);
            }

            return new FloatMenuOption(label, () => BeginCallFromConsole(console, pawn), MenuOptionPriority.Default);
        }

        // ─── 1) Call ─────────────────────────────────────────────

        public static void BeginCallFromConsole(Building_CommsConsole console, Pawn caller)
        {
            if (console == null || !console.Spawned)
            {
                return;
            }

            Map callMap = console.Map;
            string blocked = GetBlockedReason(callMap, console);
            if (blocked != null)
            {
                Messages.Message(blocked, console, MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            ShowPickupSiteMenu(callMap, caller);
        }

        public static void ShowPickupSiteMenu(Map callMap, Pawn caller)
        {
            // Always show menu so remote pickup / caravan / world pick is obvious.
            List<TaxiPickupSite> sites = TaxiPickupSite.GetAll(callMap);

            var options = new List<FloatMenuOption>();
            options.Add(new FloatMenuOption("RimTaxi_PickupMenuHeader".Translate(), null));

            // World map picker first — works even with a single colony
            options.Add(new FloatMenuOption(
                "RimTaxi_PickupFromWorldMap".Translate(),
                () => BeginPickupWorldTargeting(callMap, caller)));

            TaxiGameComponent gc = Comp;
            int listed = 0;
            for (int i = 0; i < sites.Count; i++)
            {
                TaxiPickupSite site = sites[i];
                string blocked = GetPickupSiteBlockedReason(site, gc);
                if (blocked != null)
                {
                    options.Add(new FloatMenuOption(site.label + " — " + blocked, null));
                    listed++;
                    continue;
                }

                TaxiPickupSite captured = site;
                options.Add(new FloatMenuOption(
                    site.label,
                    () => BeginPickupFlow(callMap, caller, captured)));
                listed++;
            }

            if (listed == 0)
            {
                options.Add(new FloatMenuOption("RimTaxi_NoListedPickupsHint".Translate(), null));
            }

            options.Add(new FloatMenuOption("CancelButton".Translate(), null));
            Find.WindowStack.Add(new FloatMenu(options));
            Messages.Message("RimTaxi_ChoosePickup".Translate(), MessageTypeDefOf.NeutralEvent, historical: false);
        }

        private static string GetPickupSiteBlockedReason(TaxiPickupSite site, TaxiGameComponent gc)
        {
            if (site == null || gc == null)
            {
                return null;
            }

            if (site.IsCaravan)
            {
                if (gc.HasPendingDispatch(site.caravan))
                {
                    TaxiPendingDispatch p = gc.GetPendingDispatch(site.caravan);
                    string eta = p != null ? p.TicksRemaining.ToStringTicksToPeriod() : "";
                    return "RimTaxi_TaxiEnRoute".Translate(eta);
                }

                if (gc.HasBoarding(site.caravan))
                {
                    return "RimTaxi_CaravanTaxiReady".Translate();
                }

                return null;
            }

            Map open = site.openMap;
            if (open != null && gc.HasPendingDispatch(open))
            {
                TaxiPendingDispatch p = gc.GetPendingDispatch(open);
                string eta = p != null ? p.TicksRemaining.ToStringTicksToPeriod() : "";
                return "RimTaxi_TaxiEnRoute".Translate(eta);
            }

            return null;
        }

        /// <summary>
        /// Open world map and click a player caravan / settlement / open map with colonists.
        /// </summary>
        public static void BeginPickupWorldTargeting(Map callMap, Pawn caller)
        {
            if (callMap == null)
            {
                return;
            }

            CameraJumper.TryJump(CameraJumper.GetWorldTarget(new GlobalTargetInfo(callMap.Tile)));
            Find.WorldSelector.ClearSelection();

            Find.WorldTargeter.BeginTargeting(
                (GlobalTargetInfo t) => ChosePickupWorldTarget(t, callMap, caller),
                canTargetTiles: true,
                CompLaunchable.TargeterMouseAttachment,
                closeWorldTabWhenFinished: true,
                null,
                (GlobalTargetInfo t) => PickupWorldTargetLabel(t),
                null,
                callMap.Tile,
                showCancelButton: true);

            Messages.Message("RimTaxi_PickWorldPickup".Translate(), MessageTypeDefOf.NeutralEvent, historical: false);
        }

        private static string PickupWorldTargetLabel(GlobalTargetInfo target)
        {
            if (!target.IsValid)
            {
                return null;
            }

            TaxiPickupSite site = TaxiPickupSite.FromWorldTarget(target);
            if (site == null)
            {
                GUI.color = ColorLibrary.RedReadable;
                return "RimTaxi_WorldPickupInvalid".Translate();
            }

            GUI.color = Color.white;
            return "RimTaxi_WorldPickupValid".Translate(site.label);
        }

        private static bool ChosePickupWorldTarget(GlobalTargetInfo target, Map callMap, Pawn caller)
        {
            TaxiPickupSite site = TaxiPickupSite.FromWorldTarget(target);
            if (site == null)
            {
                Messages.Message("RimTaxi_WorldPickupInvalid".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }

            string blocked = GetPickupSiteBlockedReason(site, Comp);
            if (blocked != null)
            {
                Messages.Message(blocked, MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }

            BeginPickupFlow(callMap, caller, site);
            return true;
        }

        public static void BeginPickupFlow(Map callMap, Pawn caller, TaxiPickupSite site)
        {
            if (site == null)
            {
                return;
            }

            // Caravan pickup: no map / landing cell — pay + queue caravan dispatch
            if (site.IsCaravan)
            {
                TryCompleteCallToCaravan(callMap, site.caravan);
                return;
            }

            if (site.HasOpenMap)
            {
                BeginPickupLandingTargeting(callMap, caller, site.openMap);
                return;
            }

            MapParent parent = site.mapParent;
            if (parent == null)
            {
                Messages.Message("RimTaxi_PickupMapFailed".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            LongEventHandler.QueueLongEvent(
                delegate
                {
                    Map map = GetOrGenerateMapUtility.GetOrGenerateMap(parent.Tile, null);
                    LongEventHandler.ExecuteWhenFinished(delegate
                    {
                        if (map == null)
                        {
                            Messages.Message("RimTaxi_PickupMapFailed".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                            return;
                        }

                        BeginPickupLandingTargeting(callMap, caller, map);
                    });
                },
                "GeneratingMap",
                doAsynchronously: false,
                exceptionHandler: null);
        }

        /// <summary>
        /// Comms-origin call that sends the taxi to a world caravan (fee from call map).
        /// </summary>
        public static void TryCompleteCallToCaravan(Map callMap, Caravan caravan)
        {
            if (caravan == null || caravan.Destroyed || !caravan.IsPlayerControlled)
            {
                Messages.Message("RimTaxi_CaravanInvalid".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            string blocked = GetBlockedReason(callMap, console: null);
            if (blocked != null)
            {
                Messages.Message(blocked, MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            TaxiGameComponent taxiComp = Comp;
            if (taxiComp != null && taxiComp.HasPendingDispatch(caravan))
            {
                TaxiPendingDispatch p = taxiComp.GetPendingDispatch(caravan);
                string eta = p != null ? p.TicksRemaining.ToStringTicksToPeriod() : "";
                Messages.Message("RimTaxi_TaxiEnRoute".Translate(eta), MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            if (taxiComp != null && taxiComp.HasBoarding(caravan))
            {
                Messages.Message("RimTaxi_CaravanTaxiReady".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            int callFee = CallFee;
            if (!TaxiPayment.TryPay(callMap, callFee))
            {
                Messages.Message(
                    "RimTaxi_NeedSilver".Translate(callFee, TaxiPayment.CountSilver(callMap)),
                    MessageTypeDefOf.RejectInput,
                    historical: false);
                return;
            }

            if (taxiComp == null)
            {
                // Refund
                Thing refund = ThingMaker.MakeThing(ThingDefOf.Silver);
                refund.stackCount = callFee;
                GenPlace.TryPlaceThing(refund, callMap.Center, callMap, ThingPlaceMode.Near);
                Messages.Message("RimTaxi_SpawnFailed".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            taxiComp.NotifyCalled();
            taxiComp.QueueCaravanDispatch(caravan, callFee);

            TaxiPendingDispatch pending = taxiComp.GetPendingDispatch(caravan);
            string etaText = pending != null
                ? pending.TicksRemaining.ToStringTicksToPeriod()
                : "—";

            string caravanName = caravan.Name ?? caravan.LabelCap;
            Messages.Message(
                "RimTaxi_CallDispatchedSimple".Translate(callFee, caravanName, etaText),
                caravan,
                MessageTypeDefOf.PositiveEvent);

            Find.LetterStack.ReceiveLetter(
                "RimTaxi_LetterDispatchedLabel".Translate(),
                "RimTaxi_LetterDispatchedCaravanText".Translate(callFee, etaText),
                LetterDefOf.PositiveEvent,
                caravan);

            Log.Message($"[RimTaxi] Comms→caravan call caravan#{caravan.ID} fee={callFee} eta={etaText}");
        }

        public static void BeginPickupLandingTargeting(Map callMap, Pawn caller, Map pickupMap)
        {
            if (pickupMap == null)
            {
                return;
            }

            Messages.Message(
                "RimTaxi_ChoosePickupLanding".Translate(pickupMap.Parent?.LabelCap ?? pickupMap.ToString()),
                MessageTypeDefOf.NeutralEvent,
                historical: false);

            CameraJumper.TryJump(pickupMap.Center, pickupMap);
            Current.Game.CurrentMap = pickupMap;

            TargetingParameters parms = new TargetingParameters
            {
                canTargetLocations = true,
                canTargetSelf = false,
                canTargetPawns = false,
                canTargetFires = false,
                canTargetBuildings = true,
                canTargetItems = true,
                validator = (TargetInfo t) =>
                {
                    if (!t.IsValid || t.Map != pickupMap)
                    {
                        return false;
                    }

                    return TaxiLandingUtility.CanLandHere(t.Cell, pickupMap).Accepted;
                }
            };

            Find.Targeter.BeginTargeting(
                parms,
                (LocalTargetInfo target) => TryCompleteCall(caller, callMap, pickupMap, target.Cell),
                (LocalTargetInfo target) => TaxiLandingUtility.DrawGhost(target, pickupMap),
                null,
                null);
        }

        // ─── 1–2) Call fee + Dispatch (no world map, no destination yet) ───

        public static void TryCompleteCall(Pawn negotiator, Map callMap, Map pickupMap, IntVec3 pickupCell)
        {
            if (pickupMap == null)
            {
                return;
            }

            AcceptanceReport landReport = TaxiLandingUtility.CanLandHere(pickupCell, pickupMap);
            if (!landReport.Accepted)
            {
                Messages.Message(landReport.Reason, new LookTargets(pickupCell, pickupMap), MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            TaxiGameComponent taxiComp = Comp;
            if (taxiComp != null && taxiComp.OnCooldown)
            {
                Messages.Message("RimTaxi_OnCooldown".Translate(taxiComp.CooldownTicksRemaining.ToStringTicksToPeriod()), MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            if (taxiComp != null && taxiComp.HasPendingDispatch(pickupMap))
            {
                Messages.Message("RimTaxi_TaxiEnRoute".Translate(""), MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            Map payMap = callMap ?? pickupMap;
            int callFee = CallFee;
            if (!TaxiPayment.TryPay(payMap, callFee))
            {
                Messages.Message("RimTaxi_NeedSilver".Translate(callFee, TaxiPayment.CountSilver(payMap)), MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            if (taxiComp == null)
            {
                if (callFee > 0)
                {
                    Thing refund = ThingMaker.MakeThing(ThingDefOf.Silver);
                    refund.stackCount = callFee;
                    GenPlace.TryPlaceThing(refund, payMap.Center, payMap, ThingPlaceMode.Near);
                }

                Messages.Message("RimTaxi_SpawnFailed".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            // Step 2: dispatch — destination not chosen yet
            taxiComp.NotifyCalled();
            taxiComp.QueueDispatch(pickupMap, pickupCell, PlanetTile.Invalid, 0, callFee);

            TaxiPendingDispatch pending = taxiComp.GetPendingDispatch(pickupMap);
            string eta = pending != null
                ? pending.TicksRemaining.ToStringTicksToPeriod()
                : "—";

            string pickupName = pickupMap.Parent?.LabelCap ?? pickupMap.ToString();

            Messages.Message(
                "RimTaxi_CallDispatchedSimple".Translate(callFee, pickupName, eta),
                new LookTargets(pickupCell, pickupMap),
                MessageTypeDefOf.PositiveEvent);

            Find.LetterStack.ReceiveLetter(
                "RimTaxi_LetterDispatchedLabel".Translate(),
                "RimTaxi_LetterDispatchedSimpleText".Translate(callFee, pickupName, eta),
                LetterDefOf.PositiveEvent,
                new LookTargets(pickupCell, pickupMap));

            Log.Message($"[RimTaxi] Step1-2 Call+Dispatch pickup={pickupName} cell={pickupCell} fee={callFee} eta={eta}");
        }

        // ─── 3) Arrive (spawn at pickup after ETA) ────────────────

        public static bool SpawnTaxi(Map map, IntVec3 cell, PlanetTile destTile, int distance)
        {
            try
            {
                ThingDef shuttleDef = RimTaxiDefOf.RimTaxiShuttle;
                TransportShipDef shipDef = RimTaxiDefOf.Ship_RimTaxi;
                if (shuttleDef == null || shipDef == null || map?.Parent == null)
                {
                    Log.Error("[RimTaxi] Missing defs or map parent.");
                    return false;
                }

                Thing shuttle = ThingMaker.MakeThing(shuttleDef);
                CompShuttle compShuttle = shuttle.TryGetComp<CompShuttle>();
                if (compShuttle != null)
                {
                    compShuttle.permitShuttle = true;
                    compShuttle.acceptChildren = true;
                    compShuttle.acceptColonists = true;
                    compShuttle.acceptColonyPrisoners = true;
                }

                TransportShip transportShip = TransportShipMaker.MakeTransportShip(shipDef, null, shuttle);

                // Destination is set in step 4, not at spawn (unless already provided)
                if (destTile.Valid && distance > 0)
                {
                    TaxiTripLookup.Book(transportShip, destTile, distance);
                }

                int wait = RimTaxiMod.Settings?.waitTicks ?? 12500;
                if (wait < 2500)
                {
                    wait = 2500;
                }

                ShipJob_WaitTime waitJob = (ShipJob_WaitTime)ShipJobMaker.MakeShipJob(ShipJobDefOf.WaitTime);
                waitJob.duration = wait;
                waitJob.showGizmos = true;
                transportShip.AddJob(waitJob);

                // After wait: empty leave, or require destination if loaded (billing patch)
                ShipJob_FlyAway flyJob = (ShipJob_FlyAway)ShipJobMaker.MakeShipJob(ShipJobDefOf.FlyAway);
                if (destTile.Valid)
                {
                    flyJob.destinationTile = destTile;
                    flyJob.dropMode = TransportShipDropMode.None;
                    flyJob.arrivalAction = TaxiArrivalUtility.CreateArrivalAction(destTile);
                }

                transportShip.AddJob(flyJob);
                transportShip.ArriveAt(cell, map.Parent);
                return true;
            }
            catch (Exception e)
            {
                Log.Error("[RimTaxi] SpawnTaxi failed: " + e);
                return false;
            }
        }

        // ─── 4) Set destination only (no payment, no depart) ─────

        public static void BeginSetDestination(TransportShip ship)
        {
            if (ship?.shipThing?.Map == null)
            {
                return;
            }

            Map map = ship.shipThing.Map;
            PlanetTile origin = map.Tile;
            int maxDist = MaxDistance;

            CameraJumper.TryJump(CameraJumper.GetWorldTarget(new GlobalTargetInfo(origin)));
            Find.WorldSelector.ClearSelection();

            Find.WorldTargeter.BeginTargeting(
                (GlobalTargetInfo t) => ChoseSetDestination(t, origin, maxDist, ship),
                canTargetTiles: true,
                CompLaunchable.TargeterMouseAttachment,
                closeWorldTabWhenFinished: true,
                delegate
                {
                    GenDraw.DrawWorldRadiusRing(origin, maxDist);
                },
                (GlobalTargetInfo t) => SetDestinationLabel(t, origin, maxDist, ship),
                null,
                origin,
                showCancelButton: true);
        }

        private static string SetDestinationLabel(GlobalTargetInfo target, PlanetTile origin, int maxDist, TransportShip ship)
        {
            if (!target.IsValid)
            {
                return null;
            }

            int dist = Find.WorldGrid.TraversalDistanceBetween(origin, target.Tile, passImpassable: true, maxDist + 1, canTraverseLayers: true);
            if (dist < 0 || dist > maxDist)
            {
                GUI.color = ColorLibrary.RedReadable;
                return "TransportPodDestinationBeyondMaximumRange".Translate();
            }

            float mass = TaxiTripBilling.GetCargoMass(ship);
            int fare = TaxiFareCalculator.TripFare(mass, dist);
            GUI.color = Color.white;
            return "RimTaxi_SetDestLabel".Translate(dist, mass.ToString("0.0"), fare);
        }

        private static bool ChoseSetDestination(GlobalTargetInfo target, PlanetTile origin, int maxDist, TransportShip ship)
        {
            if (!target.IsValid)
            {
                Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }

            if (target.HasWorldObject && !target.WorldObject.def.validLaunchTarget)
            {
                Messages.Message("MessageWorldObjectIsInvalid".Translate(target.WorldObject.Named("OBJECT")), MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }

            int dist = Find.WorldGrid.TraversalDistanceBetween(origin, target.Tile, passImpassable: true, maxDist + 1, canTraverseLayers: true);
            if (dist < 0 || dist > maxDist)
            {
                Messages.Message("TransportPodDestinationBeyondMaximumRange".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }

            if (dist == 0)
            {
                Messages.Message("RimTaxi_SameTile".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }

            // Step 4 only: book destination + show estimated fare. No payment, no depart.
            TaxiTripLookup.Book(ship, target.Tile, dist);
            float mass = TaxiTripBilling.GetCargoMass(ship);
            int fare = TaxiFareCalculator.TripFare(mass, dist);

            CameraJumper.TryHideWorld();
            Messages.Message(
                "RimTaxi_DestinationSet".Translate(dist, mass.ToString("0.0"), fare),
                ship.shipThing,
                MessageTypeDefOf.TaskCompletion,
                historical: false);

            Log.Message($"[RimTaxi] Step4 destination set ship#{ship.loadID} → {target.Tile} dist={dist} estFare={fare}");
            return true;
        }

        // ─── 5) Depart (charge mass×distance + fly) ──────────────

        public static void Depart(TransportShip ship)
        {
            if (ship == null)
            {
                return;
            }

            if (!TaxiTripLookup.TryGetTrip(ship, out PlanetTile dest, out int distance))
            {
                Messages.Message("RimTaxi_NeedDestinationBeforeDepart".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            CompTransporter transporter = ship.TransporterComp;
            if (transporter?.innerContainer == null || !transporter.innerContainer.Any)
            {
                Messages.Message("RimTaxi_DepartEmpty".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            Map map = ship.shipThing?.Map;
            if (map == null)
            {
                return;
            }

            if (!TaxiTripBilling.TryChargeTripFare(ship, map, distance, out int charged, out float mass))
            {
                int need = TaxiFareCalculator.TripFare(mass, distance);
                Messages.Message("RimTaxi_NeedSilver".Translate(need, TaxiPayment.CountSilver(map)), MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            if (!transporter.LoadingInProgressOrReadyToLaunch)
            {
                TransporterUtility.InitiateLoading(Gen.YieldSingle(transporter));
            }

            ShipJob_FlyAway fly = (ShipJob_FlyAway)ShipJobMaker.MakeShipJob(ShipJobDefOf.FlyAway);
            fly.destinationTile = dest;
            fly.dropMode = TransportShipDropMode.None;
            fly.arrivalAction = TaxiArrivalUtility.CreateArrivalAction(dest);

            ship.ForceJob(fly);
            TaxiTripLookup.Clear(ship);

            if (charged > 0)
            {
                Messages.Message("RimTaxi_DepartingPaid".Translate(charged, mass.ToString("0.0"), distance), ship.shipThing, MessageTypeDefOf.TaskCompletion, historical: false);
            }
            else
            {
                Messages.Message("RimTaxi_Departing".Translate(), ship.shipThing, MessageTypeDefOf.TaskCompletion, historical: false);
            }

            Log.Message($"[RimTaxi] Step5 Depart ship#{ship.loadID} → {dest} mass={mass:0.0} dist={distance} fare={charged}");
        }

        // ─── Caravan call / board (world map, no map landing) ────

        public static string GetBlockedReasonCaravan(Caravan caravan, int? requiredSilver = null)
        {
            if (caravan == null || caravan.Destroyed || !caravan.IsPlayerControlled)
            {
                return "RimTaxi_CaravanInvalid".Translate();
            }

            if (caravan.PawnsListForReading == null || caravan.PawnsListForReading.Count == 0)
            {
                return "RimTaxi_CaravanEmpty".Translate();
            }

            TaxiGameComponent taxiComp = Comp;
            if (taxiComp != null && taxiComp.OnCooldown)
            {
                return "RimTaxi_OnCooldown".Translate(taxiComp.CooldownTicksRemaining.ToStringTicksToPeriod());
            }

            if (taxiComp != null && taxiComp.HasPendingDispatch(caravan))
            {
                TaxiPendingDispatch p = taxiComp.GetPendingDispatch(caravan);
                string eta = p != null ? p.TicksRemaining.ToStringTicksToPeriod() : "";
                return "RimTaxi_TaxiEnRoute".Translate(eta);
            }

            if (taxiComp != null && taxiComp.HasBoarding(caravan))
            {
                return "RimTaxi_CaravanTaxiReady".Translate();
            }

            int need = requiredSilver ?? CallFee;
            if (!TaxiPayment.CanAfford(caravan, need))
            {
                return "RimTaxi_NeedSilver".Translate(need, TaxiPayment.CountSilver(caravan));
            }

            if (RimTaxiDefOf.Ship_RimTaxi == null || RimTaxiDefOf.RimTaxiShuttle == null
                || RimTaxiDefOf.TravelingRimTaxi == null)
            {
                return "RimTaxi_DefsMissing".Translate();
            }

            return null;
        }

        /// <summary>
        /// All caravan top-bar taxi gizmos: idle call/send, en-route status+dest, ready depart.
        /// </summary>
        public static IEnumerable<Gizmo> MakeAllCaravanTaxiGizmos(Caravan caravan)
        {
            if (caravan == null || caravan.Destroyed)
            {
                yield break;
            }

            TaxiGameComponent gc = Comp;
            TaxiCaravanBoarding boarding = gc?.GetBoarding(caravan);
            if (boarding != null)
            {
                foreach (Gizmo g in MakeCaravanBoardingGizmos(caravan, boarding))
                {
                    yield return g;
                }

                yield break;
            }

            TaxiPendingDispatch pending = gc?.GetPendingDispatch(caravan);
            if (pending != null)
            {
                foreach (Gizmo g in MakeCaravanPendingGizmos(caravan, pending))
                {
                    yield return g;
                }

                yield break;
            }

            yield return MakeCaravanSendGizmo(caravan);
        }

        /// <summary>
        /// Primary caravan action: pay call fee, pick destination, taxi en route then depart when ready.
        /// </summary>
        public static Command_Action MakeCaravanSendGizmo(Caravan caravan)
        {
            Command_Action cmd = new Command_Action
            {
                defaultLabel = "RimTaxi_CaravanSend".Translate(CallFee),
                defaultDesc = "RimTaxi_CaravanSendDesc".Translate(
                    CallFee,
                    TaxiFareCalculator.FarePerKgPerTile.ToString("0.00")),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/CallShuttle", reportFailure: false)
                    ?? ContentFinder<Texture2D>.Get("UI/Commands/LaunchShip", reportFailure: false)
                    ?? TexCommand.Attack,
                Order = -50f,
                action = () => BeginSendTaxiFromCaravan(caravan)
            };

            string blocked = GetBlockedReasonCaravan(caravan);
            if (blocked != null)
            {
                cmd.Disable(blocked);
            }

            return cmd;
        }

        // Back-compat alias
        public static Command_Action MakeCaravanCallGizmo(Caravan caravan) => MakeCaravanSendGizmo(caravan);

        public static IEnumerable<Gizmo> MakeCaravanPendingGizmos(Caravan caravan, TaxiPendingDispatch pending)
        {
            if (caravan == null || pending == null)
            {
                yield break;
            }

            string eta = pending.TicksRemaining.ToStringTicksToPeriod();
            float mass = TaxiCaravanUtility.GetCaravanMass(caravan);
            int estFare = pending.destination.Valid
                ? TaxiFareCalculator.TripFare(mass, pending.tripDistance)
                : 0;

            Command_Action status = new Command_Action
            {
                defaultLabel = "RimTaxi_CaravanEnRoute".Translate(eta),
                defaultDesc = "RimTaxi_CaravanEnRouteDesc".Translate(eta),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/CallShuttle", reportFailure: false)
                    ?? TexCommand.Attack,
                Order = -50f,
                action = () =>
                {
                    Messages.Message(
                        "RimTaxi_CaravanEnRouteDesc".Translate(eta),
                        caravan,
                        MessageTypeDefOf.NeutralEvent,
                        historical: false);
                }
            };
            yield return status;

            Command_Action setDest = new Command_Action
            {
                defaultLabel = pending.destination.Valid
                    ? "RimTaxi_SetDestinationChange".Translate(pending.tripDistance, estFare)
                    : "RimTaxi_SetDestination".Translate(),
                defaultDesc = "RimTaxi_CaravanSetDestWhileEnRouteDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchShip", reportFailure: false)
                    ?? TexCommand.Install,
                Order = -49f,
                action = () => BeginSetDestinationPendingCaravan(caravan, pending)
            };
            yield return setDest;
        }

        public static IEnumerable<Gizmo> MakeCaravanBoardingGizmos(Caravan caravan, TaxiCaravanBoarding boarding)
        {
            if (caravan == null || boarding == null)
            {
                yield break;
            }

            float mass = TaxiCaravanUtility.GetCaravanMass(caravan);
            int estFare = boarding.HasDestination
                ? TaxiFareCalculator.TripFare(mass, boarding.tripDistance)
                : 0;

            Command_Action setDest = new Command_Action
            {
                defaultLabel = boarding.HasDestination
                    ? "RimTaxi_SetDestinationChange".Translate(boarding.tripDistance, estFare)
                    : "RimTaxi_SetDestination".Translate(),
                defaultDesc = "RimTaxi_SetDestinationDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchShip", reportFailure: false)
                    ?? TexCommand.Install,
                Order = -50f,
                action = () => BeginSetDestinationCaravan(caravan, boarding)
            };
            yield return setDest;

            int passengers = TaxiCaravanUtility.PassengerCount(caravan);
            Command_Action depart = new Command_Action
            {
                defaultLabel = "RimTaxi_CaravanDepartSend".Translate(passengers, estFare),
                defaultDesc = "RimTaxi_CaravanDepartDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchShip", reportFailure: false)
                    ?? TexCommand.Attack,
                Order = -49f,
                action = () => TryDepartCaravan(caravan, boarding, auto: false)
            };

            if (!boarding.HasDestination)
            {
                depart.Disable("RimTaxi_NeedDestinationBeforeDepart".Translate());
            }
            else if (passengers <= 0)
            {
                depart.Disable("RimTaxi_DepartEmpty".Translate());
            }
            else if (!TaxiPayment.CanAfford(caravan, estFare))
            {
                depart.Disable("RimTaxi_NeedSilver".Translate(estFare, TaxiPayment.CountSilver(caravan)));
            }

            yield return depart;
        }

        /// <summary>
        /// Pay call fee then open world map to choose where to send the caravan by taxi.
        /// </summary>
        public static void BeginSendTaxiFromCaravan(Caravan caravan)
        {
            string blocked = GetBlockedReasonCaravan(caravan);
            if (blocked != null)
            {
                Messages.Message(blocked, caravan, MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            // Destination first (no charge yet). On confirm → pay call fee + queue dispatch.
            BeginCaravanWorldTarget(
                caravan,
                (GlobalTargetInfo t, int dist) =>
                {
                    if (!FinishCaravanSendAfterDestination(caravan, t.Tile, dist))
                    {
                        return false;
                    }

                    return true;
                },
                "RimTaxi_CaravanPickDestToSend".Translate());
        }

        private static bool FinishCaravanSendAfterDestination(Caravan caravan, PlanetTile dest, int dist)
        {
            string blocked = GetBlockedReasonCaravan(caravan);
            if (blocked != null)
            {
                Messages.Message(blocked, caravan, MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }

            TaxiGameComponent taxiComp = Comp;
            int callFee = CallFee;
            if (!TaxiPayment.TryPay(caravan, callFee))
            {
                Messages.Message(
                    "RimTaxi_NeedSilver".Translate(callFee, TaxiPayment.CountSilver(caravan)),
                    caravan,
                    MessageTypeDefOf.RejectInput,
                    historical: false);
                return false;
            }

            if (taxiComp == null)
            {
                TaxiPayment.RefundToCaravan(caravan, callFee);
                Messages.Message("RimTaxi_SpawnFailed".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }

            taxiComp.NotifyCalled();
            taxiComp.QueueCaravanDispatch(caravan, callFee, dest, dist);

            TaxiPendingDispatch pending = taxiComp.GetPendingDispatch(caravan);
            string eta = pending != null
                ? pending.TicksRemaining.ToStringTicksToPeriod()
                : "—";

            float mass = TaxiCaravanUtility.GetCaravanMass(caravan);
            int tripFare = TaxiFareCalculator.TripFare(mass, dist);

            Messages.Message(
                "RimTaxi_CaravanSendDispatched".Translate(callFee, dist, tripFare, eta),
                caravan,
                MessageTypeDefOf.PositiveEvent);

            Find.LetterStack.ReceiveLetter(
                "RimTaxi_LetterDispatchedLabel".Translate(),
                "RimTaxi_LetterCaravanSendText".Translate(callFee, dist, tripFare, eta),
                LetterDefOf.PositiveEvent,
                caravan);

            Log.Message($"[RimTaxi] Caravan SEND caravan#{caravan.ID} fee={callFee} dest={dest} dist={dist} eta={eta}");
            return true;
        }

        public static void TryCallFromCaravan(Caravan caravan)
        {
            // Same entry as send (destination required)
            BeginSendTaxiFromCaravan(caravan);
        }

        public static void BeginSetDestinationPendingCaravan(Caravan caravan, TaxiPendingDispatch pending)
        {
            if (caravan == null || pending == null)
            {
                return;
            }

            BeginCaravanWorldTarget(
                caravan,
                (GlobalTargetInfo t, int dist) =>
                {
                    Comp?.BookPendingCaravanDestination(caravan, t.Tile, dist);
                    float mass = TaxiCaravanUtility.GetCaravanMass(caravan);
                    int fare = TaxiFareCalculator.TripFare(mass, dist);
                    Messages.Message(
                        "RimTaxi_DestinationSet".Translate(dist, mass.ToString("0.0"), fare),
                        caravan,
                        MessageTypeDefOf.TaskCompletion,
                        historical: false);
                    return true;
                },
                "RimTaxi_CaravanPickDestToSend".Translate());
        }

        public static void BeginSetDestinationCaravan(Caravan caravan, TaxiCaravanBoarding boarding)
        {
            if (caravan == null || boarding == null)
            {
                return;
            }

            BeginCaravanWorldTarget(
                caravan,
                (GlobalTargetInfo t, int dist) =>
                {
                    boarding.Book(t.Tile, dist);
                    float mass = TaxiCaravanUtility.GetCaravanMass(caravan);
                    int fare = TaxiFareCalculator.TripFare(mass, dist);
                    Messages.Message(
                        "RimTaxi_DestinationSet".Translate(dist, mass.ToString("0.0"), fare),
                        caravan,
                        MessageTypeDefOf.TaskCompletion,
                        historical: false);
                    Log.Message($"[RimTaxi] Caravan dest set caravan#{caravan.ID} → {t.Tile} dist={dist} estFare={fare}");
                    return true;
                },
                "RimTaxi_CaravanPickDestToSend".Translate());
        }

        private delegate bool CaravanDestChosen(GlobalTargetInfo target, int distance);

        private static void BeginCaravanWorldTarget(Caravan caravan, CaravanDestChosen onChosen, string prompt)
        {
            if (caravan == null || onChosen == null)
            {
                return;
            }

            PlanetTile origin = caravan.Tile;
            int maxDist = MaxDistance;

            if (!string.IsNullOrEmpty(prompt))
            {
                Messages.Message(prompt, caravan, MessageTypeDefOf.NeutralEvent, historical: false);
            }

            CameraJumper.TryJump(CameraJumper.GetWorldTarget(new GlobalTargetInfo(origin)));
            Find.WorldSelector.ClearSelection();
            Find.WorldSelector.Select(caravan);

            Find.WorldTargeter.BeginTargeting(
                (GlobalTargetInfo t) =>
                {
                    if (!TryValidateCaravanDest(t, origin, maxDist, out int dist))
                    {
                        return false;
                    }

                    return onChosen(t, dist);
                },
                canTargetTiles: true,
                CompLaunchable.TargeterMouseAttachment,
                closeWorldTabWhenFinished: false,
                delegate
                {
                    GenDraw.DrawWorldRadiusRing(origin, maxDist);
                },
                (GlobalTargetInfo t) => SetDestinationLabelCaravan(t, origin, maxDist, caravan),
                null,
                origin,
                showCancelButton: true);
        }

        private static bool TryValidateCaravanDest(GlobalTargetInfo target, PlanetTile origin, int maxDist, out int dist)
        {
            dist = 0;
            if (!target.IsValid)
            {
                Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }

            if (target.HasWorldObject && !target.WorldObject.def.validLaunchTarget)
            {
                Messages.Message("MessageWorldObjectIsInvalid".Translate(target.WorldObject.Named("OBJECT")), MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }

            dist = Find.WorldGrid.TraversalDistanceBetween(origin, target.Tile, passImpassable: true, maxDist + 1, canTraverseLayers: true);
            if (dist < 0 || dist > maxDist)
            {
                Messages.Message("TransportPodDestinationBeyondMaximumRange".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }

            if (dist == 0)
            {
                Messages.Message("RimTaxi_SameTile".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }

            return true;
        }

        private static string SetDestinationLabelCaravan(GlobalTargetInfo target, PlanetTile origin, int maxDist, Caravan caravan)
        {
            if (!target.IsValid)
            {
                return null;
            }

            int dist = Find.WorldGrid.TraversalDistanceBetween(origin, target.Tile, passImpassable: true, maxDist + 1, canTraverseLayers: true);
            if (dist < 0 || dist > maxDist)
            {
                GUI.color = ColorLibrary.RedReadable;
                return "TransportPodDestinationBeyondMaximumRange".Translate();
            }

            float mass = TaxiCaravanUtility.GetCaravanMass(caravan);
            int fare = TaxiFareCalculator.TripFare(mass, dist);
            GUI.color = Color.white;
            return "RimTaxi_SetDestLabel".Translate(dist, mass.ToString("0.0"), fare);
        }

        /// <summary>
        /// Charge trip fare from caravan silver, board everyone, fly as TravelingRimTaxi.
        /// </summary>
        public static bool TryDepartCaravan(Caravan caravan, TaxiCaravanBoarding boarding, bool auto)
        {
            if (caravan == null || caravan.Destroyed || boarding == null)
            {
                return false;
            }

            if (!boarding.HasDestination)
            {
                if (!auto)
                {
                    Messages.Message("RimTaxi_NeedDestinationBeforeDepart".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                }

                return false;
            }

            if (TaxiCaravanUtility.PassengerCount(caravan) <= 0)
            {
                if (!auto)
                {
                    Messages.Message("RimTaxi_DepartEmpty".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                }

                return false;
            }

            float mass = TaxiCaravanUtility.GetCaravanMass(caravan);
            int fare = TaxiFareCalculator.TripFare(mass, boarding.tripDistance);
            if (!TaxiPayment.TryPay(caravan, fare))
            {
                if (!auto)
                {
                    Messages.Message(
                        "RimTaxi_NeedSilver".Translate(fare, TaxiPayment.CountSilver(caravan)),
                        caravan,
                        MessageTypeDefOf.RejectInput,
                        historical: false);
                }

                return false;
            }

            PlanetTile dest = boarding.destination;
            int dist = boarding.tripDistance;
            TransportersArrivalAction arrival = TaxiArrivalUtility.CreateArrivalAction(dest);

            // Clear boarding before launch (caravan is destroyed on success)
            Comp?.ClearBoarding(caravan);

            if (!TaxiCaravanUtility.LaunchCaravanAsTaxi(caravan, dest, arrival))
            {
                // Refund fare if launch failed (caravan still exists)
                if (!caravan.Destroyed)
                {
                    TaxiPayment.RefundToCaravan(caravan, fare);
                    // Re-attach boarding so player can retry
                    Comp?.StartCaravanBoarding(caravan, boarding.callFeePaid);
                    TaxiCaravanBoarding restored = Comp?.GetBoarding(caravan);
                    if (restored != null && dest.Valid)
                    {
                        restored.Book(dest, dist);
                    }
                }

                Messages.Message("RimTaxi_SpawnFailed".Translate(), MessageTypeDefOf.NegativeEvent);
                return false;
            }

            if (fare > 0)
            {
                Messages.Message(
                    "RimTaxi_DepartingPaid".Translate(fare, mass.ToString("0.0"), dist),
                    MessageTypeDefOf.TaskCompletion,
                    historical: false);
            }
            else
            {
                Messages.Message("RimTaxi_Departing".Translate(), MessageTypeDefOf.TaskCompletion, historical: false);
            }

            Log.Message($"[RimTaxi] Caravan depart → {dest} mass={mass:0.0} dist={dist} fare={fare} auto={auto}");
            return true;
        }
    }
}
