using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTaxi.Patches
{
    /// <summary>
    /// After boarding wait expires:
    /// - empty + no dest → leave
    /// - cargo + no dest → re-wait, ask to set destination
    /// - cargo + dest → charge mass×distance and fly
    /// </summary>
    [HarmonyPatch(typeof(ShipJob_FlyAway), nameof(ShipJob_FlyAway.TryStart))]
    public static class ShipJob_FlyAway_Billing_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(ShipJob_FlyAway __instance, ref bool __result)
        {
            TransportShip ship = __instance.transportShip;
            if (ship?.def == null || RimTaxiDefOf.Ship_RimTaxi == null || ship.def != RimTaxiDefOf.Ship_RimTaxi)
            {
                return true;
            }

            bool hasTrip = TaxiTripLookup.TryGetTrip(ship, out PlanetTile dest, out int distance);
            bool hasContents = ship.TransporterComp?.innerContainer != null && ship.TransporterComp.innerContainer.Any;
            float mass = TaxiTripBilling.GetCargoMass(ship);
            Map map = ship.shipThing?.Map;

            // No booking: dismiss / empty leave
            if (!hasTrip)
            {
                if (__instance.destinationTile.Valid)
                {
                    __instance.arrivalAction = TaxiArrivalUtility.CreateArrivalAction(__instance.destinationTile);
                    __instance.dropMode = TransportShipDropMode.None;
                    return true;
                }

                if (hasContents && mass > 0.01f)
                {
                    Messages.Message(
                        "RimTaxi_NeedDestinationBeforeDepart".Translate(),
                        ship.shipThing,
                        MessageTypeDefOf.RejectInput,
                        historical: false);

                    ReWait(ship, null);
                    __result = false;
                    return false;
                }

                // Empty leave
                return true;
            }

            if (!__instance.destinationTile.Valid)
            {
                __instance.destinationTile = dest;
            }

            __instance.arrivalAction = TaxiArrivalUtility.CreateArrivalAction(dest);
            __instance.dropMode = TransportShipDropMode.None;

            if (!hasContents || mass <= 0.01f)
            {
                TaxiTripLookup.Clear(ship);
                Log.Message($"[RimTaxi] Auto leave empty ship#{ship.loadID}");
                return true;
            }

            if (map == null)
            {
                return true;
            }

            if (!TaxiTripBilling.TryChargeTripFare(ship, map, distance, out int charged, out mass))
            {
                int need = TaxiFareCalculator.TripFare(mass, distance);
                Messages.Message(
                    "RimTaxi_AutoDepartNeedSilver".Translate(need, TaxiPayment.CountSilver(map)),
                    ship.shipThing,
                    MessageTypeDefOf.RejectInput,
                    historical: false);

                ReWait(ship, dest);
                __result = false;
                return false;
            }

            if (charged > 0)
            {
                Messages.Message(
                    "RimTaxi_DepartingPaid".Translate(charged, mass.ToString("0.0"), distance),
                    ship.shipThing,
                    MessageTypeDefOf.TaskCompletion,
                    historical: false);
            }

            TaxiTripLookup.Clear(ship);
            Log.Message($"[RimTaxi] Auto depart ship#{ship.loadID} mass={mass:0.0} dist={distance} fare={charged}");
            return true;
        }

        private static void ReWait(TransportShip ship, PlanetTile? bookedDest)
        {
            ShipJob_WaitTime wait = (ShipJob_WaitTime)ShipJobMaker.MakeShipJob(ShipJobDefOf.WaitTime);
            wait.duration = 7500;
            wait.showGizmos = true;
            ship.ForceJob(wait);

            ShipJob_FlyAway fly = (ShipJob_FlyAway)ShipJobMaker.MakeShipJob(ShipJobDefOf.FlyAway);
            if (bookedDest.HasValue && bookedDest.Value.Valid)
            {
                fly.destinationTile = bookedDest.Value;
                fly.dropMode = TransportShipDropMode.None;
                fly.arrivalAction = TaxiArrivalUtility.CreateArrivalAction(bookedDest.Value);
            }

            ship.AddJob(fly);
        }
    }
}
