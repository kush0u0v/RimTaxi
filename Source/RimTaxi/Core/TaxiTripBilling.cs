using RimWorld;
using Verse;

namespace RimTaxi
{
    /// <summary>
    /// Charge mass×distance trip fare at depart time. Call fee is separate (paid at call).
    /// </summary>
    public static class TaxiTripBilling
    {
        public static float GetCargoMass(TransportShip ship)
        {
            CompTransporter t = ship?.TransporterComp;
            if (t == null)
            {
                return 0f;
            }

            return t.MassUsage;
        }

        /// <summary>
        /// Try charge trip surcharge. Empty taxi (no mass) = 0 silver.
        /// Returns false if not enough silver (nothing charged).
        /// </summary>
        public static bool TryChargeTripFare(TransportShip ship, Map map, int distance, out int charged, out float mass)
        {
            charged = 0;
            mass = GetCargoMass(ship);
            if (map == null)
            {
                return false;
            }

            charged = TaxiFareCalculator.TripFare(mass, distance);
            if (charged <= 0)
            {
                return true;
            }

            if (!TaxiPayment.TryPay(map, charged))
            {
                return false;
            }

            return true;
        }

        public static bool CanAffordTripFare(TransportShip ship, Map map, int distance, out int fare, out float mass)
        {
            mass = GetCargoMass(ship);
            fare = TaxiFareCalculator.TripFare(mass, distance);
            if (fare <= 0)
            {
                return true;
            }

            return TaxiPayment.CanAfford(map, fare);
        }
    }
}
