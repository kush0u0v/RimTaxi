using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RimTaxi.Patches
{
    /// <summary>
    /// Early depart: charge mass×distance, fly to destination stored on the shuttle Comp.
    /// </summary>
    [HarmonyPatch(typeof(ShipJob_Wait), nameof(ShipJob_Wait.GetJobGizmos))]
    public static class ShipJob_Wait_Gizmos_Patch
    {
        private static readonly Texture2D SendTex = CompLaunchable.LaunchCommandTex;
        private static readonly Texture2D DismissTex =
            ContentFinder<Texture2D>.Get("UI/Commands/DismissShuttle", reportFailure: false);

        [HarmonyPrefix]
        public static bool Prefix(ShipJob_Wait __instance, ref IEnumerable<Gizmo> __result)
        {
            TransportShip ship = __instance.transportShip;
            if (ship?.def == null || RimTaxiDefOf.Ship_RimTaxi == null || ship.def != RimTaxiDefOf.Ship_RimTaxi)
            {
                return true;
            }

            __result = RimTaxiWaitGizmos(ship);
            return false;
        }

        private static IEnumerable<Gizmo> RimTaxiWaitGizmos(TransportShip ship)
        {
            CompTransporter transporter = ship.TransporterComp;
            int passengerCount = CountPassengers(transporter);
            bool hasBoarded = passengerCount > 0 || (transporter?.innerContainer?.Any ?? false);

            bool hasTrip = TaxiTripLookup.TryGetTrip(ship, out PlanetTile dest, out int distance);
            float mass = TaxiTripBilling.GetCargoMass(ship);
            int tripFare = hasTrip ? TaxiFareCalculator.TripFare(mass, distance) : 0;
            Map map = ship.shipThing?.Map;

            Command_Action departNow = new Command_Action
            {
                defaultLabel = "RimTaxi_DepartNowFare".Translate(passengerCount, tripFare),
                defaultDesc = "RimTaxi_DepartNowDesc".Translate(),
                icon = SendTex,
                alsoClickIfOtherInGroupClicked = false,
                Order = -20f,
                action = () => DepartToBookedDestination(ship)
            };

            if (!hasBoarded)
            {
                departNow.Disable("RimTaxi_DepartEmpty".Translate());
            }
            else if (!hasTrip)
            {
                departNow.Disable("RimTaxi_DepartNoDest".Translate());
            }
            else if (map != null && !TaxiTripBilling.CanAffordTripFare(ship, map, distance, out int need, out _))
            {
                departNow.Disable("RimTaxi_NeedSilver".Translate(need, TaxiPayment.CountSilver(map)));
            }

            yield return departNow;

            Command_Action dismiss = new Command_Action
            {
                defaultLabel = "CommandShuttleDismiss".Translate(),
                defaultDesc = "CommandShuttleDismissDesc".Translate(),
                icon = DismissTex ?? SendTex,
                alsoClickIfOtherInGroupClicked = false,
                Order = -19f,
                action = delegate
                {
                    TaxiTripLookup.Clear(ship);
                    ship.ForceJob(ShipJobDefOf.Unload);
                    ship.AddJob(ShipJobMaker.MakeShipJob(ShipJobDefOf.FlyAway));
                }
            };
            yield return dismiss;
        }

        private static int CountPassengers(CompTransporter transporter)
        {
            if (transporter?.innerContainer == null)
            {
                return 0;
            }

            int n = 0;
            for (int i = 0; i < transporter.innerContainer.Count; i++)
            {
                if (transporter.innerContainer[i] is Pawn)
                {
                    n++;
                }
            }

            return n;
        }

        public static void DepartToBookedDestination(TransportShip ship)
        {
            if (ship == null)
            {
                return;
            }

            if (!TaxiTripLookup.TryGetTrip(ship, out PlanetTile dest, out int distance))
            {
                Messages.Message("RimTaxi_DepartNoDest".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                Log.Warning($"[RimTaxi] Depart failed: no trip on ship#{ship.loadID} thing={ship.shipThing} comp={TaxiTripLookup.GetComp(ship) != null}");
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
            fly.arrivalAction = new TransportersArrivalAction_RimTaxiWorldDrop("RimTaxi_ArrivedCaravan");

            ship.ForceJob(fly);
            // Clear only after we successfully issued the fly job
            TaxiTripLookup.Clear(ship);

            if (charged > 0)
            {
                Messages.Message("RimTaxi_DepartingPaid".Translate(charged, mass.ToString("0.0"), distance), ship.shipThing, MessageTypeDefOf.TaskCompletion, historical: false);
            }
            else
            {
                Messages.Message("RimTaxi_Departing".Translate(), ship.shipThing, MessageTypeDefOf.TaskCompletion, historical: false);
            }

            Log.Message($"[RimTaxi] Early depart ship#{ship.loadID} → {dest} mass={mass:0.0} dist={distance} fare={charged}");
        }
    }
}
