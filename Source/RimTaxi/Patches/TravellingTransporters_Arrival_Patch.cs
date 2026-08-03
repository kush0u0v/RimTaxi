using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTaxi.Patches
{
    /// <summary>
    /// Hard-force RimTaxi flights to use world-map caravan drop.
    /// Vanilla Arrived() replaces invalid/null arrivalAction with LandInSpecificCell
    /// when a map already exists on the destination tile (combat / pawn bugs).
    /// </summary>
    [HarmonyPatch]
    public static class TravellingTransporters_Arrival_Patch
    {
        private static readonly FieldInfo ArrivedField =
            AccessTools.Field(typeof(TravellingTransporters), "arrived");

        private static readonly FieldInfo TransportersField =
            AccessTools.Field(typeof(TravellingTransporters), "transporters");

        private static readonly MethodInfo DoArrivalActionMethod =
            AccessTools.Method(typeof(TravellingTransporters), "DoArrivalAction");

        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(TravellingTransporters), "Arrived");
        }

        /// <summary>
        /// Skip vanilla Arrived entirely for RimTaxi world objects.
        /// </summary>
        [HarmonyPrefix]
        public static bool Prefix(TravellingTransporters __instance)
        {
            if (__instance == null || RimTaxiDefOf.TravelingRimTaxi == null)
            {
                return true;
            }

            if (__instance.def != RimTaxiDefOf.TravelingRimTaxi)
            {
                return true;
            }

            if (ArrivedField != null && (bool)ArrivedField.GetValue(__instance))
            {
                return false;
            }

            ArrivedField?.SetValue(__instance, true);

            // Always overwrite — do not trust StillValid / save-loaded alternate actions.
            __instance.arrivalAction = new TransportersArrivalAction_RimTaxiWorldDrop("RimTaxi_ArrivedCaravan");

            Log.Message($"[RimTaxi] Forced WorldDrop arrival on tile {__instance.destinationTile} (def={__instance.def?.defName}).");

            if (DoArrivalActionMethod != null)
            {
                DoArrivalActionMethod.Invoke(__instance, null);
            }
            else
            {
                // Fallback: call action directly
                List<ActiveTransporterInfo> list = TransportersField?.GetValue(__instance) as List<ActiveTransporterInfo>;
                if (list != null && __instance.arrivalAction != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        list[i].savePawnsWithReferenceMode = false;
                        list[i].parent = null;
                    }

                    __instance.arrivalAction.Arrived(list, __instance.destinationTile);
                    __instance.arrivalAction = null;
                    list.Clear();
                    __instance.Destroy();
                }
            }

            return false; // skip vanilla Arrived
        }
    }
}
