using HarmonyLib;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RimTaxi.Patches
{
    /// <summary>
    /// While a taxi is en route or waiting at a caravan, show taxi icon instead of yellow caravan circle.
    /// </summary>
    public static class Caravan_WorldIcon_Patch
    {
        [HarmonyPatch(typeof(WorldObject), nameof(WorldObject.ExpandingIcon), MethodType.Getter)]
        public static class ExpandingIcon
        {
            [HarmonyPostfix]
            public static void Postfix(WorldObject __instance, ref Texture2D __result)
            {
                if (__instance is Caravan caravan && TaxiCaravanUtility.ShouldShowTaxiWorldIcon(caravan))
                {
                    Texture2D taxi = TaxiCaravanUtility.TaxiWorldIcon;
                    if (taxi != null)
                    {
                        __result = taxi;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(WorldObject), nameof(WorldObject.Material), MethodType.Getter)]
        public static class GetMaterial
        {
            [HarmonyPostfix]
            public static void Postfix(WorldObject __instance, ref UnityEngine.Material __result)
            {
                if (__instance is Caravan caravan && TaxiCaravanUtility.ShouldShowTaxiWorldIcon(caravan))
                {
                    UnityEngine.Material mat = TaxiCaravanUtility.TaxiWorldMaterial;
                    if (mat != null)
                    {
                        __result = mat;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(WorldObject), nameof(WorldObject.ExpandingIconColor), MethodType.Getter)]
        public static class ExpandingIconColor
        {
            [HarmonyPostfix]
            public static void Postfix(WorldObject __instance, ref Color __result)
            {
                // Keep taxi art colors (no yellow caravan tint)
                if (__instance is Caravan caravan && TaxiCaravanUtility.ShouldShowTaxiWorldIcon(caravan))
                {
                    __result = Color.white;
                }
            }
        }
    }
}
