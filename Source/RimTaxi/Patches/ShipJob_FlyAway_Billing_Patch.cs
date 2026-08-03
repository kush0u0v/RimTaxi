using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTaxi.Patches
{
    /// <summary>
    /// Auto-depart after wait: charge mass×distance when loaded; empty leaves free.
    /// Trip booking is read from shuttle Comp (not cleared by load/unload).
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

            // Dismiss path: no booking → plain leave
            if (!TaxiTripLookup.TryGetTrip(ship, out PlanetTile dest, out int distance))
            {
                return true;
            }

            if (!__instance.destinationTile.Valid)
            {
                __instance.destinationTile = dest;
            }

            // Always re-resolve so settlement map land works even if old WorldDrop was queued.
            __instance.arrivalAction = TaxiArrivalUtility.CreateArrivalAction(dest);
            __instance.dropMode = TransportShipDropMode.None;

            Map map = ship.shipThing?.Map;
            bool hasContents = ship.TransporterComp?.innerContainer != null && ship.TransporterComp.innerContainer.Any;
            float mass = TaxiTripBilling.GetCargoMass(ship);

            // Empty after wait: leave, then clear booking
            if (!hasContents || mass <= 0.01f)
            {
                Log.Message($"[RimTaxi] Auto leave empty ship#{ship.loadID}");
                // Clear after allowing fly — use postfix pattern: clear here is OK (leaving empty)
                TaxiTripLookup.Clear(ship);
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

                // Keep booking; re-wait so player can get silver
                ShipJob_WaitTime wait = (ShipJob_WaitTime)ShipJobMaker.MakeShipJob(ShipJobDefOf.WaitTime);
                wait.duration = 7500;
                wait.showGizmos = true;
                ship.ForceJob(wait);

                ShipJob_FlyAway fly = (ShipJob_FlyAway)ShipJobMaker.MakeShipJob(ShipJobDefOf.FlyAway);
                fly.destinationTile = dest;
                fly.dropMode = TransportShipDropMode.None;
                fly.arrivalAction = TaxiArrivalUtility.CreateArrivalAction(dest);
                ship.AddJob(fly);

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
    }
}
