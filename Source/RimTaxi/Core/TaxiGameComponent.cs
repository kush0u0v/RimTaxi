using System.Collections.Generic;
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
    /// Cooldown + pending trips (dest + distance) keyed by TransportShip.loadID.
    /// </summary>
    public class TaxiGameComponent : GameComponent
    {
        private int lastCallTick = -999999;
        private Dictionary<int, TaxiTripInfo> trips = new Dictionary<int, TaxiTripInfo>();

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
            Log.Message($"[RimTaxi] Registered trip ship#{transportShipLoadId} → {destination} dist={distance}");
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

        public override void ExposeData()
        {
            Scribe_Values.Look(ref lastCallTick, "rimTaxiLastCallTick", -999999);
            Scribe_Collections.Look(ref trips, "rimTaxiTrips", LookMode.Value, LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && trips == null)
            {
                trips = new Dictionary<int, TaxiTripInfo>();
            }

            base.ExposeData();
        }
    }
}
