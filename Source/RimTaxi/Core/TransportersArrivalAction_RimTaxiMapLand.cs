using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTaxi
{
    /// <summary>
    /// Land the taxi on a settlement/map. Uses our own drop path (not vanilla DropShuttle),
    /// because DropShuttle requires CompShuttle.Props.shipDef / Royalty Ship_Shuttle and NREs otherwise.
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
            return destinationTile.Valid;
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
                Log.Warning("[RimTaxi] MapLand: no MapParent; world caravan fallback.");
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
                Log.Error("[RimTaxi] MapLand: map get/generate failed: " + e);
                new TransportersArrivalAction_RimTaxiWorldDrop("RimTaxi_ArrivedCaravan").Arrived(transporters, tile);
                return;
            }

            if (map == null)
            {
                Log.Warning("[RimTaxi] MapLand: map null; world caravan fallback.");
                new TransportersArrivalAction_RimTaxiWorldDrop("RimTaxi_ArrivedCaravan").Arrived(transporters, tile);
                return;
            }

            // Map is open/visible: player picks landing cell + Q/E (same as call-time placement).
            // Hold transporters and open targeter after this frame.
            var held = new List<ActiveTransporterInfo>(transporters);
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                TaxiCallService.BeginMapLandingPick(map, held);
            });
            Log.Message($"[RimTaxi] MapLand: waiting for player landing pick on {map}");
        }

        /// <summary>
        /// Safe equivalent of TransportersArrivalActionUtility.DropShuttle for RimTaxi.
        /// </summary>
        public static Thing DropRimTaxi(
            ActiveTransporterInfo transporter,
            Map map,
            IntVec3 near,
            Rot4 rotation,
            TransportShipDef shipDef,
            ThingDef shuttleDef)
        {
            if (transporter == null || map == null)
            {
                throw new System.ArgumentNullException("transporter/map");
            }

            // World pawns must re-enter the map
            TransportersArrivalActionUtility.RemovePawnsFromWorldPawns(Gen.YieldSingle(transporter));

            Thing shuttle = transporter.RemoveShuttle();
            if (shuttle == null)
            {
                if (shuttleDef == null)
                {
                    throw new System.InvalidOperationException("No shuttle def and no shuttle in transporter.");
                }

                shuttle = ThingMaker.MakeThing(shuttleDef);
            }

            CompShuttle compShuttle = shuttle.TryGetComp<CompShuttle>();
            CompTransporter compTransporter = shuttle.TryGetComp<CompTransporter>();
            if (compTransporter == null)
            {
                throw new System.InvalidOperationException("RimTaxi shuttle missing CompTransporter.");
            }

            if (compShuttle != null)
            {
                compShuttle.permitShuttle = true;
                compShuttle.acceptChildren = true;
                compShuttle.acceptColonists = true;
                compShuttle.acceptColonyPrisoners = true;
            }

            shuttle.Rotation = rotation;
            if (shuttle.Faction == null)
            {
                shuttle.SetFaction(Faction.OfPlayer);
            }

            // Move remaining contents into the shuttle transporter
            if (transporter.innerContainer != null && transporter.innerContainer.Count > 0)
            {
                compTransporter.innerContainer.TryAddRangeOrTransfer(
                    transporter.innerContainer,
                    canMergeWithExistingStacks: true,
                    destroyLeftover: true);
            }

            // Ensure TransportShip parent (DropShuttle NRE'd when shipDef was null)
            TransportShip transportShip = compShuttle?.shipParent;
            if (transportShip == null)
            {
                if (shipDef == null)
                {
                    // Last resort: still spawn the building so pawns aren't lost
                    if (!near.IsValid)
                    {
                        near = DropCellFinder.TradeDropSpot(map);
                    }

                    GenSpawn.Spawn(shuttle, near, map, rotation, WipeMode.VanishOrMoveAside);
                    UnloadToMap(compTransporter, map, near);
                    return shuttle;
                }

                transportShip = TransportShipMaker.MakeTransportShip(shipDef, null, shuttle);
            }

            if (!near.IsValid)
            {
                near = DropCellFinder.GetBestShuttleLandingSpot(map, Faction.OfPlayer);
            }

            // Land via ship pipeline (skyfaller arrive)
            transportShip.ArriveAt(near, map.Parent);

            // Clear the leg just completed so re-boarding requires a new destination (no auto re-fly).
            TaxiTripLookup.Clear(transportShip);
            shuttle.TryGetComp<CompRimTaxiTrip>()?.Clear();

            // Unload passengers for trade/explore, then WAIT so they can reboard and go elsewhere.
            ShipJob_Unload unload = (ShipJob_Unload)ShipJobMaker.MakeShipJob(ShipJobDefOf.Unload);
            unload.dropMode = TransportShipDropMode.All;
            transportShip.AddJob(unload);

            int wait = RimTaxiMod.Settings?.waitTicks ?? 12500;
            if (wait < 2500)
            {
                wait = 2500;
            }

            ShipJob_WaitTime waitJob = (ShipJob_WaitTime)ShipJobMaker.MakeShipJob(ShipJobDefOf.WaitTime);
            waitJob.duration = wait;
            waitJob.showGizmos = true;
            transportShip.AddJob(waitJob);

            // After wait: empty leave (billing patch); loaded without dest re-waits; loaded+dest charges and flies.
            transportShip.AddJob(ShipJobMaker.MakeShipJob(ShipJobDefOf.FlyAway));

            return shuttle;
        }

        private static void UnloadToMap(CompTransporter comp, Map map, IntVec3 near)
        {
            if (comp?.innerContainer == null)
            {
                return;
            }

            List<Thing> items = new List<Thing>();
            items.AddRange(comp.innerContainer);
            for (int i = 0; i < items.Count; i++)
            {
                Thing t = items[i];
                comp.innerContainer.Remove(t);
                GenPlace.TryPlaceThing(t, near, map, ThingPlaceMode.Near);
            }
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
