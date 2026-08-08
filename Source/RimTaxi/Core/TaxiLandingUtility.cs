using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTaxi
{
    /// <summary>
    /// Landing checks and ghost draw. Fixed rotation (no Q/E).
    /// </summary>
    public static class TaxiLandingUtility
    {
        public static ThingDef ShuttleDef => RimTaxiDefOf.RimTaxiShuttle ?? ThingDefOf.Shuttle;

        /// <summary>Fixed placement rotation (default facing). Q/E rotation removed.</summary>
        public static Rot4 DefaultRot
        {
            get
            {
                ThingDef def = ShuttleDef;
                return def != null ? def.defaultPlacingRot : Rot4.North;
            }
        }

        /// <summary>Alias for call sites that still say PlacementRot.</summary>
        public static Rot4 PlacementRot => DefaultRot;

        public static void ResetPlacementRot()
        {
            // No-op: rotation is fixed.
        }

        public static AcceptanceReport CanLandHere(LocalTargetInfo target, Map map)
        {
            return CanLandHere(target, map, DefaultRot);
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
            DrawGhost(target, map, DefaultRot);
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

        /// <summary>
        /// Preferred cell if still valid; else random valid cell; else Invalid.
        /// </summary>
        public static IntVec3 ResolveLandingCell(Map map, IntVec3 preferred)
        {
            Rot4 rot = DefaultRot;
            if (map == null)
            {
                return IntVec3.Invalid;
            }

            if (preferred.IsValid && CanLandHere(preferred, map, rot).Accepted)
            {
                return preferred;
            }

            return FindRandomLandingCell(map, rot);
        }

        public static IntVec3 FindRandomLandingCell(Map map, Rot4 rot = default)
        {
            if (map == null)
            {
                return IntVec3.Invalid;
            }

            if (rot == default)
            {
                rot = DefaultRot;
            }

            ThingDef shuttleDef = ShuttleDef;
            IntVec3 landing = DropCellFinder.GetBestShuttleLandingSpot(map, Faction.OfPlayer);
            if (shuttleDef != null && CanLandHere(landing, map, rot).Accepted)
            {
                return landing;
            }

            if (CellFinder.TryFindRandomCell(
                    map,
                    c => CanLandHere(c, map, rot).Accepted,
                    out landing)
                && landing.IsValid)
            {
                return landing;
            }

            // Last ditch: trade drop spot if shuttle can land there
            landing = DropCellFinder.TradeDropSpot(map);
            if (landing.IsValid && CanLandHere(landing, map, rot).Accepted)
            {
                return landing;
            }

            return IntVec3.Invalid;
        }

        /// <summary>
        /// True if this world tile will map-land on an already-open map (player can pre-pick cell).
        /// </summary>
        public static bool TryGetOpenMapForLanding(PlanetTile tile, out Map map)
        {
            map = null;
            if (!tile.Valid)
            {
                return false;
            }

            TransportersArrivalAction action = TaxiArrivalUtility.CreateArrivalAction(tile);
            if (!(action is TransportersArrivalAction_RimTaxiMapLand mapLand))
            {
                return false;
            }

            // Resolve open map without generating a new one
            Settlement settlement = Find.WorldObjects.SettlementAt(tile);
            if (settlement != null && settlement.HasMap)
            {
                map = settlement.Map;
                return map != null;
            }

            MapParent parent = Find.WorldObjects.MapParentAt(tile);
            if (parent != null && parent.HasMap)
            {
                map = parent.Map;
                return map != null;
            }

            for (int i = 0; i < Find.Maps.Count; i++)
            {
                Map m = Find.Maps[i];
                if (m != null && m.Tile == tile)
                {
                    map = m;
                    return true;
                }
            }

            return false;
        }
    }
}
