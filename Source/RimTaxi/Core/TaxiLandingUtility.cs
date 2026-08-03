using RimWorld;
using UnityEngine;
using Verse;

namespace RimTaxi
{
    /// <summary>
    /// Landing checks and ghost draw — delegates to vanilla shuttle helpers.
    /// </summary>
    public static class TaxiLandingUtility
    {
        public static ThingDef ShuttleDef => RimTaxiDefOf.RimTaxiShuttle ?? ThingDefOf.Shuttle;

        public static AcceptanceReport CanLandHere(LocalTargetInfo target, Map map)
        {
            ThingDef def = ShuttleDef;
            return RoyalTitlePermitWorker_CallShuttle.ShuttleCanLandHere(
                target,
                map,
                def,
                def.defaultPlacingRot);
        }

        public static void DrawGhost(LocalTargetInfo target, Map map)
        {
            ThingDef def = ShuttleDef;
            RoyalTitlePermitWorker_CallShuttle.DrawShuttleGhost(
                target,
                map,
                def,
                def.defaultPlacingRot);
        }
    }
}
