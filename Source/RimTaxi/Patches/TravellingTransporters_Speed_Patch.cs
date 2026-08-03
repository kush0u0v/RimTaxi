using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace RimTaxi.Patches
{
    /// <summary>
    /// Slow down RimTaxi world flights via settings.travelSpeedFactor.
    /// </summary>
    [HarmonyPatch(typeof(TravellingTransporters), "get_TraveledPctStepPerTick")]
    public static class TravellingTransporters_Speed_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(TravellingTransporters __instance, ref float __result)
        {
            if (__instance == null || __result <= 0f)
            {
                return;
            }

            if (RimTaxiDefOf.TravelingRimTaxi == null || __instance.def != RimTaxiDefOf.TravelingRimTaxi)
            {
                return;
            }

            float factor = RimTaxiMod.Settings?.travelSpeedFactor ?? 0.6f;
            if (factor < 0.05f)
            {
                factor = 0.05f;
            }

            __result *= factor;
        }
    }
}
