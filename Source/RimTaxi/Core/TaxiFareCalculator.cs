using UnityEngine;
using Verse;

namespace RimTaxi
{
    /// <summary>
    /// Call fee = baseFare.
    /// Trip surcharge at depart = massKg × distanceTiles × farePerKgPerTile (ceil).
    /// </summary>
    public static class TaxiFareCalculator
    {
        public static int CallFee => RimTaxiMod.Settings?.baseFare ?? 200;

        public static float FarePerKgPerTile => RimTaxiMod.Settings?.farePerKgPerTile ?? 0.1f;

        public static int TripFare(float massKg, int distanceTiles)
        {
            if (massKg < 0f)
            {
                massKg = 0f;
            }

            if (distanceTiles < 0)
            {
                distanceTiles = 0;
            }

            if (massKg <= 0.01f || distanceTiles <= 0)
            {
                return 0;
            }

            float rate = FarePerKgPerTile;
            if (rate < 0f)
            {
                rate = 0f;
            }

            long total = Mathf.CeilToInt(massKg * distanceTiles * rate);
            if (total > int.MaxValue)
            {
                return int.MaxValue;
            }

            if (total < 0)
            {
                return 0;
            }

            return (int)total;
        }

        public static string DescribeCallFee()
        {
            return "RimTaxi_CallFeeOnly".Translate(CallFee);
        }

        public static string DescribeTripEstimate(float massKg, int distanceTiles)
        {
            int fare = TripFare(massKg, distanceTiles);
            return "RimTaxi_TripFareBreakdown".Translate(
                massKg.ToString("0.0"),
                distanceTiles,
                FarePerKgPerTile.ToString("0.00"),
                fare);
        }
    }
}
