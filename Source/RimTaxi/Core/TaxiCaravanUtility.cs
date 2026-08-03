using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTaxi
{
    /// <summary>
    /// Call / board / launch taxi from a world caravan (no map).
    /// </summary>
    public static class TaxiCaravanUtility
    {
        public static Caravan FindCaravanById(int caravanId)
        {
            if (caravanId < 0 || Find.WorldObjects == null)
            {
                return null;
            }

            List<Caravan> caravans = Find.WorldObjects.Caravans;
            for (int i = 0; i < caravans.Count; i++)
            {
                Caravan c = caravans[i];
                if (c != null && !c.Destroyed && c.ID == caravanId)
                {
                    return c;
                }
            }

            return null;
        }

        public static float GetCaravanMass(Caravan caravan)
        {
            return caravan == null ? 0f : caravan.MassUsage;
        }

        public static int PassengerCount(Caravan caravan)
        {
            return caravan?.PawnsListForReading?.Count ?? 0;
        }

        /// <summary>
        /// Load entire caravan into TravellingTransporters (TravelingRimTaxi) and destroy the caravan.
        /// </summary>
        public static bool LaunchCaravanAsTaxi(Caravan caravan, PlanetTile destinationTile, TransportersArrivalAction arrivalAction)
        {
            if (caravan == null || caravan.Destroyed || !destinationTile.Valid)
            {
                return false;
            }

            ThingDef shuttleDef = RimTaxiDefOf.RimTaxiShuttle;
            WorldObjectDef worldDef = RimTaxiDefOf.TravelingRimTaxi;
            if (shuttleDef == null || worldDef == null)
            {
                Log.Error("[RimTaxi] LaunchCaravanAsTaxi: missing defs.");
                return false;
            }

            try
            {
                Thing shuttle = ThingMaker.MakeThing(shuttleDef);
                CompShuttle compShuttle = shuttle.TryGetComp<CompShuttle>();
                if (compShuttle != null)
                {
                    compShuttle.permitShuttle = true;
                    compShuttle.acceptChildren = true;
                    compShuttle.acceptColonists = true;
                    compShuttle.acceptColonyPrisoners = true;
                }

                if (shuttle.Faction == null)
                {
                    shuttle.SetFaction(Faction.OfPlayer);
                }

                ActiveTransporterInfo info = new ActiveTransporterInfo();
                info.sentTransporterDef = shuttleDef;

                // Inventory off pawns first, then pawns themselves (vanilla caravan-shuttle order)
                LoadCaravanInventoryInto(caravan, info.innerContainer);
                info.innerContainer.TryAddRangeOrTransfer(caravan.pawns, canMergeWithExistingStacks: true, destroyLeftover: false);
                info.SetShuttle(shuttle);

                TravellingTransporters traveling = (TravellingTransporters)WorldObjectMaker.MakeWorldObject(worldDef);
                traveling.Tile = caravan.Tile;
                traveling.SetFaction(Faction.OfPlayer);
                traveling.destinationTile = destinationTile;
                traveling.arrivalAction = arrivalAction ?? TaxiArrivalUtility.CreateArrivalAction(destinationTile);
                traveling.AddTransporter(info, true);
                Find.WorldObjects.Add(traveling);

                caravan.Destroy();

                CameraJumper.TryJump(traveling);
                Log.Message($"[RimTaxi] Caravan boarded taxi → tile {destinationTile}");
                return true;
            }
            catch (System.Exception e)
            {
                Log.Error("[RimTaxi] LaunchCaravanAsTaxi failed: " + e);
                return false;
            }
        }

        private static void LoadCaravanInventoryInto(Caravan caravan, ThingOwner container)
        {
            if (caravan == null || container == null)
            {
                return;
            }

            List<Pawn> pawns = new List<Pawn>(caravan.PawnsListForReading);
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn?.inventory?.innerContainer == null)
                {
                    continue;
                }

                // Guard against infinite loops
                for (int guard = 0; guard < 1000; guard++)
                {
                    if (pawn.inventory.innerContainer.Count == 0)
                    {
                        break;
                    }

                    Thing item = pawn.inventory.innerContainer[0];
                    if (item == null)
                    {
                        break;
                    }

                    if (pawn.inventory.innerContainer.TryTransferToContainer(item, container, item.stackCount) <= 0)
                    {
                        break;
                    }
                }
            }
        }
    }
}

