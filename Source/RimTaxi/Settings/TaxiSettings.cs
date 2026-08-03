using UnityEngine;
using Verse;

namespace RimTaxi
{
    public class TaxiSettings : ModSettings
    {
        /// <summary>Silver charged when calling the taxi (dispatch).</summary>
        public int baseFare = 200;

        /// <summary>Trip surcharge: massKg × distanceTiles × this rate (rounded up).</summary>
        public float farePerKgPerTile = 0.1f;

        public int maxPassengers = 4;

        /// <summary>Reuse cooldown. Default 1 in-game hour.</summary>
        public int cooldownTicks = 2500;

        /// <summary>Boarding wait. Default 5 in-game hours — then leaves (empty or with whoever boarded).</summary>
        public int waitTicks = 12500;

        public int maxLaunchDistance = 70;

        public float travelSpeedFactor = 0.6f;

        /// <summary>
        /// If true (default), land the shuttle on settlement/map tiles (SRTS-like).
        /// If false, always form a world caravan beside the tile.
        /// </summary>
        public bool landOnSettlementMaps = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref baseFare, "baseFare", 200);
            Scribe_Values.Look(ref farePerKgPerTile, "farePerKgPerTile", 0.1f);
            // Migrate old saves that only had farePerTile
            int legacyPerTile = 15;
            Scribe_Values.Look(ref legacyPerTile, "farePerTile", 15);

            Scribe_Values.Look(ref maxPassengers, "maxPassengers", 4);
            Scribe_Values.Look(ref cooldownTicks, "cooldownTicks", 2500);
            Scribe_Values.Look(ref waitTicks, "waitTicks", 12500);
            Scribe_Values.Look(ref maxLaunchDistance, "maxLaunchDistance", 70);
            Scribe_Values.Look(ref travelSpeedFactor, "travelSpeedFactor", 0.6f);
            Scribe_Values.Look(ref landOnSettlementMaps, "landOnSettlementMaps", true);
            base.ExposeData();
        }

        public void ResetToDefaults()
        {
            baseFare = 200;
            farePerKgPerTile = 0.1f;
            maxPassengers = 4;
            cooldownTicks = 2500;
            waitTicks = 12500;
            maxLaunchDistance = 70;
            travelSpeedFactor = 0.6f;
            landOnSettlementMaps = true;
        }

        public void DoWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("RimTaxi_Settings_CallFee".Translate(baseFare));
            baseFare = (int)listing.Slider(baseFare, 0f, 2000f);

            listing.Label("RimTaxi_Settings_FarePerKgPerTile".Translate(farePerKgPerTile.ToString("0.00")));
            farePerKgPerTile = listing.Slider(farePerKgPerTile, 0.01f, 2f);

            // Example: 60kg colonist × 10 tiles
            int ex1 = TaxiFareCalculator.TripFare(60f, 10);
            int ex2 = TaxiFareCalculator.TripFare(200f, 30);
            listing.Label("RimTaxi_Settings_ExampleTripFare".Translate(ex1, ex2));

            listing.Label("RimTaxi_Settings_MaxPassengers".Translate(maxPassengers));
            maxPassengers = (int)listing.Slider(maxPassengers, 1f, 12f);

            listing.Label("RimTaxi_Settings_Cooldown".Translate((cooldownTicks / 2500f).ToString("0.0")));
            cooldownTicks = (int)listing.Slider(cooldownTicks, 0f, 120000f);
            if (cooldownTicks > 2000 && cooldownTicks < 3000)
            {
                cooldownTicks = 2500;
            }

            listing.Label("RimTaxi_Settings_Wait".Translate((waitTicks / 2500f).ToString("0.0")));
            waitTicks = (int)listing.Slider(waitTicks, 2500f, 180000f);
            // Snap near 5h
            if (waitTicks > 11000 && waitTicks < 14000)
            {
                waitTicks = 12500;
            }

            listing.Label("RimTaxi_Settings_MaxLaunch".Translate(maxLaunchDistance));
            maxLaunchDistance = (int)listing.Slider(maxLaunchDistance, 10f, 100f);

            listing.Label("RimTaxi_Settings_TravelSpeed".Translate(travelSpeedFactor.ToString("0.00")));
            travelSpeedFactor = listing.Slider(travelSpeedFactor, 0.25f, 1.5f);

            listing.CheckboxLabeled(
                "RimTaxi_Settings_LandOnMaps".Translate(),
                ref landOnSettlementMaps,
                "RimTaxi_Settings_LandOnMapsTip".Translate());

            if (listing.ButtonText("RimTaxi_Settings_Reset".Translate()))
            {
                ResetToDefaults();
            }

            listing.End();
        }
    }
}
