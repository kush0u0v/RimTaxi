using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RimTaxi.Patches
{
    /// <summary>
    /// RimTaxi appears as a radio contact (like factions), plus optional console gizmo shortcut.
    /// </summary>
    public static class Building_CommsConsole_Patch
    {
        /// <summary>
        /// Add taxi company to the same contact list used when a colonist uses the comms console.
        /// </summary>
        [HarmonyPatch(typeof(Building_CommsConsole), nameof(Building_CommsConsole.GetCommTargets))]
        public static class GetCommTargets
        {
            [HarmonyPostfix]
            public static IEnumerable<ICommunicable> Postfix(IEnumerable<ICommunicable> __result)
            {
                // Taxi first so it is easy to find among factions
                yield return TaxiCommsContact.Instance;

                if (__result != null)
                {
                    foreach (ICommunicable c in __result)
                    {
                        yield return c;
                    }
                }
            }
        }

        /// <summary>
        /// Optional gizmo: open the same radio dialog without walking to the console
        /// (still requires power/comms usable when starting a paid call).
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
                    yield return MakeOpenCommsGizmo(console);
                }
            }
        }

        private static Command_Action MakeOpenCommsGizmo(Building_CommsConsole console)
        {
            Command_Action cmd = new Command_Action
            {
                defaultLabel = "RimTaxi_CallOptionWithFee".Translate(TaxiCallService.CallFee),
                defaultDesc = "RimTaxi_CommsGizmoDesc".Translate(
                    TaxiCallService.CallFee,
                    TaxiFareCalculator.FarePerKgPerTile.ToString("0.00")),
                icon = ContentFinder<UnityEngine.Texture2D>.Get("UI/Commands/CallShuttle", reportFailure: false)
                    ?? TexCommand.Attack,
                action = () =>
                {
                    // Prefer a free colonist on the map as "negotiator" for the radio UI
                    Pawn negotiator = console.Map?.mapPawns?.FreeColonistsSpawned?.FirstOrFallback();
                    if (negotiator == null)
                    {
                        negotiator = console.Map?.mapPawns?.FreeColonists?.FirstOrFallback();
                    }

                    if (negotiator == null)
                    {
                        // Still allow pickup flow without dialog if no colonist exists
                        string blocked = TaxiCallService.GetBlockedReason(console.Map, console);
                        if (blocked != null)
                        {
                            Messages.Message(blocked, console, MessageTypeDefOf.RejectInput, historical: false);
                            return;
                        }

                        TaxiCallService.ShowPickupSiteMenu(console.Map, null);
                        return;
                    }

                    TaxiCommsContact.Instance.TryOpenComms(negotiator);
                }
            };

            if (!console.CanUseCommsNow)
            {
                string reason = TaxiCallService.GetBlockedReason(console.Map, console)
                    ?? "RimTaxi_CommsUnavailable".Translate();
                cmd.Disable(reason);
            }

            return cmd;
        }
    }
}
