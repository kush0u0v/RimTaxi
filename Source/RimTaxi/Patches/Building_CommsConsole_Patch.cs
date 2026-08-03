using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RimTaxi.Patches
{
    /// <summary>
    /// Direct taxi call from the comms console (gizmo + right-click float menu).
    /// </summary>
    public static class Building_CommsConsole_Patch
    {
        /// <summary>
        /// Thing.GetGizmos is not overridden on Building_CommsConsole; filter by runtime type.
        /// </summary>
        [HarmonyPatch(typeof(Thing), nameof(Thing.GetGizmos))]
        public static class Thing_GetGizmos
        {
            [HarmonyPostfix]
            public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Thing __instance)
            {
                if (__result != null)
                {
                    foreach (Gizmo gizmo in __result)
                    {
                        yield return gizmo;
                    }
                }

                if (__instance is Building_CommsConsole console
                    && console.Spawned
                    && console.Faction == Faction.OfPlayer)
                {
                    yield return TaxiCallService.MakeCallGizmo(console);
                }
            }
        }

        [HarmonyPatch(typeof(Building_CommsConsole), nameof(Building_CommsConsole.GetFloatMenuOptions))]
        public static class GetFloatMenuOptions
        {
            [HarmonyPostfix]
            public static IEnumerable<FloatMenuOption> Postfix(
                IEnumerable<FloatMenuOption> __result,
                Building_CommsConsole __instance,
                Pawn myPawn)
            {
                if (__instance != null && __instance.Spawned && myPawn != null && myPawn.IsColonistPlayerControlled)
                {
                    yield return TaxiCallService.MakeCallFloatMenuOption(myPawn, __instance);
                }

                if (__result != null)
                {
                    foreach (FloatMenuOption option in __result)
                    {
                        yield return option;
                    }
                }
            }
        }
    }
}
