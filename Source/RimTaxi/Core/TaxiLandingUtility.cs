using RimWorld;
using UnityEngine;
using Verse;

namespace RimTaxi
{
    /// <summary>
    /// Landing checks, ghost draw, and Q/E rotation while targeting.
    /// </summary>
    public static class TaxiLandingUtility
    {
        public static ThingDef ShuttleDef => RimTaxiDefOf.RimTaxiShuttle ?? ThingDefOf.Shuttle;

        /// <summary>Rotation used during active landing targeter (Q/E).</summary>
        public static Rot4 PlacementRot = Rot4.North;

        public static void ResetPlacementRot()
        {
            ThingDef def = ShuttleDef;
            PlacementRot = def != null ? def.defaultPlacingRot : Rot4.North;
        }

        /// <summary>
        /// Poll designator rotate bindings while targeting is active.
        /// </summary>
        public static void ProcessPlacementRotationInput()
        {
            if (KeyBindingDefOf.Designator_RotateLeft.KeyDownEvent)
            {
                PlacementRot = PlacementRot.Rotated(RotationDirection.Counterclockwise);
            }

            if (KeyBindingDefOf.Designator_RotateRight.KeyDownEvent)
            {
                PlacementRot = PlacementRot.Rotated(RotationDirection.Clockwise);
            }
        }

        public static AcceptanceReport CanLandHere(LocalTargetInfo target, Map map)
        {
            return CanLandHere(target, map, PlacementRot);
        }

        public static AcceptanceReport CanLandHere(LocalTargetInfo target, Map map, Rot4 rot)
        {
            ThingDef def = ShuttleDef;
            if (def == null || map == null)
            {
                return "RimTaxi_DefsMissing".Translate();
            }

            return RoyalTitlePermitWorker_CallShuttle.ShuttleCanLandHere(target, map, def, rot);
        }

        public static void DrawGhost(LocalTargetInfo target, Map map)
        {
            ProcessPlacementRotationInput();
            DrawGhost(target, map, PlacementRot);
        }

        public static void DrawGhost(LocalTargetInfo target, Map map, Rot4 rot)
        {
            ThingDef def = ShuttleDef;
            if (def == null || map == null || !target.IsValid)
            {
                return;
            }

            RoyalTitlePermitWorker_CallShuttle.DrawShuttleGhost(target, map, def, rot);
        }
    }
}
