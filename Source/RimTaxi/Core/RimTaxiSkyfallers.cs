using RimWorld;
using UnityEngine;
using Verse;

namespace RimTaxi
{
    /// <summary>
    /// Incoming taxi: drop-spot shadow tracks under the craft along the approach path.
    /// Vanilla always paints the shadow on the pad only, so angled landings look like a vertical drop.
    /// </summary>
    public class RimTaxiIncoming : ShuttleIncoming
    {
        protected override void DrawDropSpotShadow()
        {
            Material mat = ShadowMaterial;
            if (mat == null || def?.skyfaller == null)
            {
                return;
            }

            Vector3 shadowLoc = DrawPos;
            GetDrawPositionAndRotation(ref shadowLoc, out float _);
            DrawDropSpotShadow(shadowLoc, Rotation, mat, def.skyfaller.shadowSize, ticksToImpact);
        }
    }

    /// <summary>
    /// Leaving taxi: same shadow tracking on takeoff.
    /// </summary>
    public class RimTaxiLeaving : FlyShipLeaving
    {
        protected override void DrawDropSpotShadow()
        {
            Material mat = ShadowMaterial;
            if (mat == null || def?.skyfaller == null)
            {
                return;
            }

            Vector3 shadowLoc = DrawPos;
            GetDrawPositionAndRotation(ref shadowLoc, out float _);
            DrawDropSpotShadow(shadowLoc, Rotation, mat, def.skyfaller.shadowSize, ticksToImpact);
        }
    }
}
