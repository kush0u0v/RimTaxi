using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTaxi
{
    /// <summary>
    /// Open/generate the destination map and land the taxi vehicle on it (door-to-door drop-off).
    /// StillValid always stays true so vanilla cannot replace this with a bad fallback.
    /// </summary>
    public class TransportersArrivalAction_RimTaxiMapLand : TransportersArrivalAction
    {
        private MapParent mapParent;

        public override bool GeneratesMap => mapParent != null && !mapParent.HasMap;

        public TransportersArrivalAction_RimTaxiMapLand()
        {
        }

        public TransportersArrivalAction_RimTaxiMapLand(MapParent mapParent)
        {
            this.mapParent = mapParent;
        }

        public override FloatMenuAcceptanceReport StillValid(IEnumerable<IThingHolder> pods, PlanetTile destinationTile)
        {
            if (!destinationTile.Valid)
            {
                return false;
            }

            // Keep valid even if mapParent despawned — Arrived re-resolves parent by tile.
            return true;
        }

        public override bool ShouldUseLongEvent(List<ActiveTransporterInfo> pods, PlanetTile tile)
        {
            MapParent parent = ResolveParent(tile);
            return parent != null && !parent.HasMap;
        }

        public override void Arrived(List<ActiveTransporterInfo> transporters, PlanetTile tile)
        {
            Log.Message($"[RimTaxi] MapLand.Arrived tile={tile} transporters={transporters?.Count ?? 0}");

            if (transporters == null || transporters.Count == 0)
            {
                Log.Warning("[RimTaxi] MapLand: no transporters.");
                return;
            }

            MapParent parent = ResolveParent(tile);
            if (parent == null)
            {
                Log.Warning("[RimTaxi] MapLand: no MapParent; falling back to world caravan drop.");
                new TransportersArrivalAction_RimTaxiWorldDrop("RimTaxi_ArrivedCaravan").Arrived(transporters, tile);
                return;
            }

            Map map;
            try
            {
                map = parent.HasMap
                    ? parent.Map
                    : GetOrGenerateMapUtility.GetOrGenerateMap(parent.Tile, null);
            }
            catch (System.Exception e)
            {
                Log.Error("[RimTaxi] MapLand: failed to get/generate map: " + e);
                new TransportersArrivalAction_RimTaxiWorldDrop("RimTaxi_ArrivedCaravan").Arrived(transporters, tile);
                return;
            }

            if (map == null)
            {
                Log.Warning("[RimTaxi] MapLand: map null; world drop fallback.");
                new TransportersArrivalAction_RimTaxiWorldDrop("RimTaxi_ArrivedCaravan").Arrived(transporters, tile);
                return;
            }

            // Prefer a good shuttle pad; fall back to random valid landing cell.
            IntVec3 landing = DropCellFinder.GetBestShuttleLandingSpot(map, Faction.OfPlayer);
            ThingDef shuttleDef = RimTaxiDefOf.RimTaxiShuttle ?? ThingDefOf.Shuttle;
            Rot4 rot = shuttleDef.defaultPlacingRot;

            if (!RoyalTitlePermitWorker_CallShuttle.ShuttleCanLandHere(landing, map, shuttleDef, rot).Accepted)
            {
                if (!CellFinder.TryFindRandomCell(map, c =>
                        RoyalTitlePermitWorker_CallShuttle.ShuttleCanLandHere(c, map, shuttleDef, rot).Accepted,
                    out landing))
                {
                    landing = DropCellFinder.TradeDropSpot(map);
                }
            }

            // One shuttle group only
            ActiveTransporterInfo info = transporters[0];
            Thing look = TransportersArrivalActionUtility.GetLookTarget(transporters);

            try
            {
                TransportersArrivalActionUtility.DropShuttle(info, map, landing, rot, Faction.OfPlayer);
            }
            catch (System.Exception e)
            {
                Log.Error("[RimTaxi] DropShuttle failed: " + e + " — world drop fallback.");
                new TransportersArrivalAction_RimTaxiWorldDrop("RimTaxi_ArrivedCaravan").Arrived(transporters, tile);
                return;
            }

            // Extra pods if any (shouldn't happen for taxi)
            for (int i = 1; i < transporters.Count; i++)
            {
                try
                {
                    TransportersArrivalActionUtility.DropTravellingDropPods(
                        new List<ActiveTransporterInfo> { transporters[i] },
                        landing,
                        map);
                }
                catch (System.Exception e)
                {
                    Log.Warning("[RimTaxi] Extra pod drop failed: " + e);
                }
            }

            Messages.Message("RimTaxi_ArrivedOnMap".Translate(map.Parent?.LabelCap ?? map.ToString()), look, MessageTypeDefOf.TaskCompletion);
            CameraJumper.TryJump(landing, map);
            Log.Message($"[RimTaxi] MapLand dropped shuttle at {landing} on {map}");
        }

        private MapParent ResolveParent(PlanetTile tile)
        {
            if (mapParent != null && mapParent.Spawned)
            {
                return mapParent;
            }

            Settlement settlement = Find.WorldObjects.SettlementAt(tile);
            if (settlement != null)
            {
                return settlement;
            }

            return Find.WorldObjects.MapParentAt(tile);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref mapParent, "mapParent");
        }
    }
}
