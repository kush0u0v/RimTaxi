using System.Collections.Generic;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace RimTaxi.Patches
{
    /// <summary>
    /// World caravan top bar: Call taxi, or Set destination / Depart when taxi is ready.
    /// </summary>
    [HarmonyPatch(typeof(Caravan), nameof(Caravan.GetGizmos))]
    public static class Caravan_Gizmos_Patch
    {
        [HarmonyPostfix]
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Caravan __instance)
        {
            if (__result != null)
            {
                foreach (Gizmo g in __result)
                {
                    yield return g;
                }
            }

            if (__instance == null || __instance.Destroyed || !__instance.IsPlayerControlled)
            {
                yield break;
            }

            TaxiGameComponent comp = TaxiGameComponent.Get();
            TaxiCaravanBoarding boarding = comp?.GetBoarding(__instance);
            if (boarding != null)
            {
                foreach (Gizmo g in TaxiCallService.MakeCaravanBoardingGizmos(__instance, boarding))
                {
                    yield return g;
                }

                yield break;
            }

            // En route: show disabled call with ETA, still list the gizmo for clarity
            yield return TaxiCallService.MakeCaravanCallGizmo(__instance);
        }
    }
}
