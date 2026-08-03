using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTaxi.Patches
{
    /// <summary>
    /// While a taxi is dispatched to / waiting at a caravan, the caravan cannot move.
    /// </summary>
    public static class Caravan_Movement_Patch
    {
        private static readonly FieldInfo PatherCaravanField =
            AccessTools.Field(typeof(Caravan_PathFollower), "caravan");

        [HarmonyPatch(typeof(Caravan), nameof(Caravan.CantMove), MethodType.Getter)]
        public static class CantMove
        {
            [HarmonyPostfix]
            public static void Postfix(Caravan __instance, ref bool __result)
            {
                if (__result || __instance == null)
                {
                    return;
                }

                if (TaxiCaravanUtility.IsImmobilizedForTaxi(__instance))
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(Caravan_PathFollower), nameof(Caravan_PathFollower.StartPath))]
        public static class StartPath
        {
            [HarmonyPrefix]
            public static bool Prefix(Caravan_PathFollower __instance, ref bool __result)
            {
                Caravan caravan = PatherCaravanField?.GetValue(__instance) as Caravan;
                if (caravan == null || !TaxiCaravanUtility.IsImmobilizedForTaxi(caravan))
                {
                    return true;
                }

                string reason = TaxiCaravanUtility.ImmobilizedForTaxiReason(caravan)
                    ?? "RimTaxi_CaravanImmobileGeneric".Translate();
                Messages.Message(reason, caravan, MessageTypeDefOf.RejectInput, historical: false);
                __result = false;
                return false;
            }
        }
    }
}
