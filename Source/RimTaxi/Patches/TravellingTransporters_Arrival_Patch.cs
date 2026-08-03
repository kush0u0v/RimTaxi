using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTaxi.Patches
{
    /// <summary>
    /// RimTaxi flights: run our arrival action (map land or world caravan).
    /// Skips vanilla StillValid fallback that can LandInSpecificCell incorrectly.
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

            // Prefer action already set on the flight; otherwise pick from destination tile.
            TransportersArrivalAction action = __instance.arrivalAction;
            if (action == null
                || action is TransportersArrivalAction_RimTaxiWorldDrop
                || action is TransportersArrivalAction_RimTaxiMapLand)
            {
                // Re-pick so setting landOnSettlementMaps is honored at arrival time.
                action = TaxiArrivalUtility.CreateArrivalAction(__instance.destinationTile);
            }
            else
            {
                // Unknown action type — still force our chooser for safety.
                action = TaxiArrivalUtility.CreateArrivalAction(__instance.destinationTile);
            }

            __instance.arrivalAction = action;

            Log.Message($"[RimTaxi] Arrival via {action.GetType().Name} on tile {__instance.destinationTile}");

            if (DoArrivalActionMethod != null)
            {
                DoArrivalActionMethod.Invoke(__instance, null);
            }
            else
            {
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

            return false;
        }
    }
}
