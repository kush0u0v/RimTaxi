using System;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimTaxi
{
    /// <summary>
    /// Call (200 silver) → land → wait 5h / early depart → trip fare = mass × distance → fly.
    /// </summary>
    public static class TaxiCallService
    {
        public static TaxiGameComponent Comp => Current.Game?.GetComponent<TaxiGameComponent>();

        public static int CallFee => TaxiFareCalculator.CallFee;

        public static int MaxDistance => RimTaxiMod.Settings?.maxLaunchDistance ?? 70;

        public static string GetBlockedReason(Map map, Building_CommsConsole console = null, int? requiredSilver = null)
        {
            if (map == null || !map.IsPlayerHome)
            {
                return "RimTaxi_NotPlayerHome".Translate();
            }

            if (map.generatorDef != null && map.generatorDef.isUnderground)
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
            if (!TaxiPayment.CanAfford(map, need))
            {
                return "RimTaxi_NeedSilver".Translate(need, TaxiPayment.CountSilver(map));
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

        public static void BeginCallFromConsole(Building_CommsConsole console, Pawn caller)
        {
            if (console == null || !console.Spawned)
            {
                return;
            }

            Map map = console.Map;
            string blocked = GetBlockedReason(map, console);
            if (blocked != null)
            {
                Messages.Message(blocked, console, MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            BeginDestinationTargeting(caller, map);
        }

        public static void BeginDestinationTargeting(Pawn caller, Map map)
        {
            if (map == null)
            {
                return;
            }

            PlanetTile origin = map.Tile;
            int maxDist = MaxDistance;

            CameraJumper.TryJump(CameraJumper.GetWorldTarget(new GlobalTargetInfo(origin)));
            Find.WorldSelector.ClearSelection();

            Find.WorldTargeter.BeginTargeting(
                (GlobalTargetInfo t) => ChoseWorldDestination(t, origin, map, caller, maxDist),
                canTargetTiles: true,
                CompLaunchable.TargeterMouseAttachment,
                closeWorldTabWhenFinished: false,
                delegate
                {
                    GenDraw.DrawWorldRadiusRing(origin, maxDist);
                },
                (GlobalTargetInfo t) => DestinationLabel(t, origin, maxDist, map),
                null,
                origin,
                showCancelButton: true);
        }

        private static string DestinationLabel(GlobalTargetInfo target, PlanetTile origin, int maxDist, Map map)
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

            // Call fee only now; trip fare depends on mass at depart.
            GUI.color = Color.white;
            return "RimTaxi_DestLabel".Translate(CallFee, dist, TaxiFareCalculator.FarePerKgPerTile.ToString("0.00"));
        }

        private static bool ChoseWorldDestination(GlobalTargetInfo target, PlanetTile origin, Map map, Pawn caller, int maxDist)
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

            // Only need call fee to book.
            string blocked = GetBlockedReason(map, null, CallFee);
            if (blocked != null)
            {
                Messages.Message(blocked, MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }

            PlanetTile destTile = target.Tile;
            CameraJumper.TryHideWorld();
            BeginLandingTargeting(caller, map, dist, destTile);
            return true;
        }

        public static void BeginLandingTargeting(Pawn negotiator, Map map, int distance, PlanetTile destTile)
        {
            if (map == null)
            {
                return;
            }

            Messages.Message(
                "RimTaxi_ChooseLanding".Translate(TaxiFareCalculator.DescribeCallFee()),
                MessageTypeDefOf.NeutralEvent,
                historical: false);

            CameraJumper.TryJump(map.Center, map);

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
                    if (!t.IsValid || t.Map != map)
                    {
                        return false;
                    }

                    return TaxiLandingUtility.CanLandHere(t.Cell, map).Accepted;
                }
            };

            Find.Targeter.BeginTargeting(
                parms,
                (LocalTargetInfo target) => TryCompleteCall(negotiator, map, target.Cell, distance, destTile),
                (LocalTargetInfo target) => TaxiLandingUtility.DrawGhost(target, map),
                null,
                null);
        }

        public static void TryCompleteCall(Pawn negotiator, Map map, IntVec3 cell, int distance, PlanetTile destTile)
        {
            AcceptanceReport landReport = TaxiLandingUtility.CanLandHere(cell, map);
            if (!landReport.Accepted)
            {
                Messages.Message(landReport.Reason, new LookTargets(cell, map), MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            TaxiGameComponent taxiComp = Comp;
            if (taxiComp != null && taxiComp.OnCooldown)
            {
                Messages.Message("RimTaxi_OnCooldown".Translate(taxiComp.CooldownTicksRemaining.ToStringTicksToPeriod()), MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            int callFee = CallFee;
            if (!TaxiPayment.TryPay(map, callFee))
            {
                Messages.Message("RimTaxi_NeedSilver".Translate(callFee, TaxiPayment.CountSilver(map)), MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            if (!SpawnTaxi(map, cell, destTile, distance))
            {
                if (callFee > 0)
                {
                    Thing refund = ThingMaker.MakeThing(ThingDefOf.Silver);
                    refund.stackCount = callFee;
                    if (!GenPlace.TryPlaceThing(refund, cell, map, ThingPlaceMode.Near))
                    {
                        refund.Destroy();
                        Log.Warning("[RimTaxi] Paid call fee but failed to spawn taxi and could not refund.");
                    }
                }

                Messages.Message("RimTaxi_SpawnFailed".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            taxiComp?.NotifyCalled();

            Messages.Message(
                "RimTaxi_CallSuccessTrip".Translate(callFee, distance),
                new LookTargets(cell, map),
                MessageTypeDefOf.PositiveEvent);

            Log.Message($"[RimTaxi] Called by {negotiator?.LabelShort ?? "console"} cell={cell} dest={destTile} dist={distance} callFee={callFee}");
        }

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
                // Store on shuttle Comp + GameComponent (load/unload must not erase booking)
                TaxiTripLookup.Book(transportShip, destTile, distance);

                int wait = RimTaxiMod.Settings?.waitTicks ?? 12500;
                if (wait < 2500)
                {
                    wait = 2500;
                }

                ShipJob_WaitTime waitJob = (ShipJob_WaitTime)ShipJobMaker.MakeShipJob(ShipJobDefOf.WaitTime);
                waitJob.duration = wait;
                waitJob.showGizmos = true;
                transportShip.AddJob(waitJob);

                ShipJob_FlyAway flyJob = (ShipJob_FlyAway)ShipJobMaker.MakeShipJob(ShipJobDefOf.FlyAway);
                flyJob.destinationTile = destTile;
                flyJob.dropMode = TransportShipDropMode.None;
                flyJob.arrivalAction = TaxiArrivalUtility.CreateArrivalAction(destTile);
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
    }
}
