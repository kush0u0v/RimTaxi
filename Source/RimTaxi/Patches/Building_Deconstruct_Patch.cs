using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RimTaxi.Patches
{
    /// <summary>
    /// Hide deconstruct gizmo on RimTaxi shuttle (service craft, not a colony building).
    /// </summary>
    [HarmonyPatch(typeof(Building), nameof(Building.GetGizmos))]
    public static class Building_Deconstruct_Patch
    {
        [HarmonyPostfix]
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Building __instance)
        {
            if (__result == null)
            {
                yield break;
            }

            bool isTaxi = __instance != null
                && RimTaxiDefOf.RimTaxiShuttle != null
                && __instance.def == RimTaxiDefOf.RimTaxiShuttle;

            foreach (Gizmo g in __result)
            {
                if (isTaxi && IsDeconstructGizmo(g))
                {
                    continue;
                }

                yield return g;
            }
        }

        private static bool IsDeconstructGizmo(Gizmo g)
        {
            if (g is Designator_Deconstruct)
            {
                return true;
            }

            if (g is Command_Action cmd)
            {
                string label = cmd.defaultLabel ?? "";
                // English "Deconstruct" / Korean "해체"
                if (label.IndexOf("Deconstruct", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || label.Contains("해체"))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
