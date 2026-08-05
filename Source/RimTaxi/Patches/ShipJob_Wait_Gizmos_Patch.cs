using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RimTaxi.Patches
{
    /// <summary>
    /// Step 4: Set destination. Step 5: Depart (pay mass×distance + fly).
    /// </summary>
    [HarmonyPatch(typeof(ShipJob_Wait), nameof(ShipJob_Wait.GetJobGizmos))]
    public static class ShipJob_Wait_Gizmos_Patch
    {
        private static readonly Texture2D SendTex = CompLaunchable.LaunchCommandTex;
        private static readonly Texture2D WorldTex =
            ContentFinder<Texture2D>.Get("UI/Commands/LaunchShip", reportFailure: false)
            ?? CompLaunchable.LaunchCommandTex;
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

            // ─── Step 4: Set destination ───
            Command_Action setDest = new Command_Action
            {
                defaultLabel = hasTrip
                    ? "RimTaxi_SetDestinationChange".Translate(distance, tripFare)
                    : "RimTaxi_SetDestination".Translate(),
                defaultDesc = "RimTaxi_SetDestinationDesc".Translate(),
                icon = WorldTex,
                alsoClickIfOtherInGroupClicked = false,
                Order = -21f,
                action = () => TaxiCallService.BeginSetDestination(ship)
            };
            yield return setDest;

            // ─── Step 5: Depart ───
            Command_Action depart = new Command_Action
            {
                defaultLabel = "RimTaxi_DepartStep".Translate(passengerCount, tripFare),
                defaultDesc = "RimTaxi_DepartStepDesc".Translate(),
                icon = SendTex,
                alsoClickIfOtherInGroupClicked = false,
                Order = -20f,
                action = () => TaxiCallService.Depart(ship)
            };

            if (!hasBoarded)
            {
                depart.Disable("RimTaxi_DepartEmpty".Translate());
            }
            else if (!hasTrip)
            {
                depart.Disable("RimTaxi_NeedDestinationBeforeDepart".Translate());
            }
            else if (map != null && !TaxiTripBilling.CanAffordTripFare(ship, map, distance, out int need, out _))
            {
                depart.Disable("RimTaxi_NeedSilver".Translate(need, TaxiPayment.CountSilver(map)));
            }

            yield return depart;

            // Reposition landing pad (settlement / camp maps) — Q/E rotate while targeting
            if (map != null && ship.shipThing != null && ship.shipThing.Spawned)
            {
                Command_Action reposition = new Command_Action
                {
                    defaultLabel = "RimTaxi_RepositionLanding".Translate(),
                    defaultDesc = "RimTaxi_RepositionLandingDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/RimTaxiDisembark", reportFailure: false)
                        ?? WorldTex,
                    alsoClickIfOtherInGroupClicked = false,
                    Order = -19.5f,
                    action = () => TaxiCallService.BeginRepositionTaxiOnMap(ship)
                };
                yield return reposition;
            }

            // Dismiss / leave without trip
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
    }
}
