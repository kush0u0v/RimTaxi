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

        /// <summary>Boarding/layover wait after taxi lands (incl. after a ride). Default 2 in-game hours.</summary>
        public int waitTicks = 5000;

        /// <summary>Base time until taxi arrives after call. Default 1 hour.</summary>
        public int dispatchBaseTicks = 2500;

        /// <summary>Extra random dispatch delay (0..this). Default up to 2 hours → total ETA 1–3 hours.</summary>
        public int dispatchVarianceTicks = 5000;

        /// <summary>Optional extra ticks per trip tile. Default 0 so call ETA stays in 1–3h band.</summary>
        public int dispatchTicksPerTripTile = 0;

        public int maxLaunchDistance = 70;

        public float travelSpeedFactor = 0.6f;

        /// <summary>
        /// If true (default), land the taxi on settlement/map tiles.
        /// If false, always form a world caravan beside the tile.
        /// </summary>
        public bool landOnSettlementMaps = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref baseFare, "baseFare", 200);
            Scribe_Values.Look(ref farePerKgPerTile, "farePerKgPerTile", 0.1f);
            int legacyPerTile = 15;
            Scribe_Values.Look(ref legacyPerTile, "farePerTile", 15);

            Scribe_Values.Look(ref maxPassengers, "maxPassengers", 4);
            Scribe_Values.Look(ref cooldownTicks, "cooldownTicks", 2500);
            Scribe_Values.Look(ref waitTicks, "waitTicks", 5000);
            Scribe_Values.Look(ref dispatchBaseTicks, "dispatchBaseTicks", 2500);
            Scribe_Values.Look(ref dispatchVarianceTicks, "dispatchVarianceTicks", 5000);
            Scribe_Values.Look(ref dispatchTicksPerTripTile, "dispatchTicksPerTripTile", 0);
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
            dispatchBaseTicks = 2500;
            dispatchVarianceTicks = 5000;
            dispatchTicksPerTripTile = 0;
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

            listing.Label("RimTaxi_Settings_DispatchBase".Translate((dispatchBaseTicks / 2500f).ToString("0.0")));
            dispatchBaseTicks = (int)listing.Slider(dispatchBaseTicks, 0f, 60000f);

            listing.Label("RimTaxi_Settings_DispatchVariance".Translate((dispatchVarianceTicks / 2500f).ToString("0.0")));
            dispatchVarianceTicks = (int)listing.Slider(dispatchVarianceTicks, 0f, 30000f);

            listing.Label("RimTaxi_Settings_DispatchPerTile".Translate(dispatchTicksPerTripTile));
            dispatchTicksPerTripTile = (int)listing.Slider(dispatchTicksPerTripTile, 0f, 250f);

            listing.Label("RimTaxi_Settings_Wait".Translate((waitTicks / 2500f).ToString("0.0")));
            waitTicks = (int)listing.Slider(waitTicks, 2500f, 180000f);
            // Snap near 2h default
            if (waitTicks > 4000 && waitTicks < 6000)
            {
                waitTicks = 5000;
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
