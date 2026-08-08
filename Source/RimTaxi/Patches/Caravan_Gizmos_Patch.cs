using System.Collections.Generic;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace RimTaxi.Patches
{
    /// <summary>
    /// World caravan top bar: taxi gizmos only while en route / boarding
    /// (set dest, depart, disembark). Call is comms-console only — no idle send.
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

            foreach (Gizmo g in TaxiCallService.MakeAllCaravanTaxiGizmos(__instance))
            {
                yield return g;
            }
        }
    }
}
