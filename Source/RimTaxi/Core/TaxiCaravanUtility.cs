using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RimTaxi
{
    /// <summary>
    /// Call / board / launch taxi from a world caravan (no map).
    /// </summary>
    public static class TaxiCaravanUtility
    {
        private static Texture2D cachedTaxiWorldIcon;
        private static Material cachedTaxiWorldMat;

        /// <summary>
        /// Show taxi world icon while boarding layover (until disembark/depart).
        /// </summary>
        public static bool ShouldShowTaxiWorldIcon(Caravan caravan)
        {
            if (caravan == null || caravan.Destroyed)
            {
                return false;
            }

            // Ground taxi with caravan: boarding wait. Also pending en-route for visibility.
            TaxiGameComponent gc = TaxiGameComponent.Get();
            return gc != null && (gc.HasBoarding(caravan) || gc.HasPendingDispatch(caravan));
        }

        public static Texture2D TaxiWorldIcon
        {
            get
            {
                if (cachedTaxiWorldIcon == null)
                {
                    // Prefer world icons; fall back to command / building art
                    cachedTaxiWorldIcon = ContentFinder<Texture2D>.Get("World/WorldObjects/Expanding/RimTaxi", reportFailure: false)
                        ?? ContentFinder<Texture2D>.Get("World/WorldObjects/RimTaxi", reportFailure: false)
                        ?? ContentFinder<Texture2D>.Get("UI/Commands/RimTaxiInstantSend", reportFailure: false)
                        ?? ContentFinder<Texture2D>.Get("UI/Commands/RimTaxiDisembark", reportFailure: false)
                        ?? ContentFinder<Texture2D>.Get("Things/Building/Misc/RimTaxiShuttle", reportFailure: false);
                    if (cachedTaxiWorldIcon == null)
                    {
                        Log.Warning("[RimTaxi] Taxi world icon texture missing — caravan will stay yellow circle.");
                    }
                }

                return cachedTaxiWorldIcon;
            }
        }

        public static Material TaxiWorldMaterial
        {
            get
            {
                if (cachedTaxiWorldMat == null && TaxiWorldIcon != null)
                {
                    // Same shader family as Caravan.Material (yellow disc uses WorldOverlayTransparentLit)
                    cachedTaxiWorldMat = MaterialPool.MatFrom(
                        TaxiWorldIcon,
                        ShaderDatabase.WorldOverlayTransparentLit,
                        Color.white,
                        3600);
                }

                return cachedTaxiWorldMat;
            }
        }

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
        /// True while a taxi is en route to this caravan, or waiting for board/depart (layover).
        /// Caravan must stay put so the taxi can meet them.
        /// </summary>
        public static bool IsImmobilizedForTaxi(Caravan caravan)
        {
            if (caravan == null || caravan.Destroyed)
            {
                return false;
            }

            TaxiGameComponent gc = TaxiGameComponent.Get();
            if (gc == null)
            {
                return false;
            }

            return gc.HasPendingDispatch(caravan) || gc.HasBoarding(caravan);
        }

        public static string ImmobilizedForTaxiReason(Caravan caravan)
        {
            if (caravan == null)
            {
                return null;
            }

            TaxiGameComponent gc = TaxiGameComponent.Get();
            if (gc == null)
            {
                return null;
            }

            if (gc.HasPendingDispatch(caravan))
            {
                TaxiPendingDispatch p = gc.GetPendingDispatch(caravan);
                string eta = p != null ? p.TicksRemaining.ToStringTicksToPeriod() : "";
                return "RimTaxi_CaravanImmobileEnRoute".Translate(eta);
            }

            if (gc.HasBoarding(caravan))
            {
                TaxiCaravanBoarding b = gc.GetBoarding(caravan);
                string left = b != null ? b.WaitTicksRemaining.ToStringTicksToPeriod() : "";
                return "RimTaxi_CaravanImmobileWaiting".Translate(left);
            }

            return null;
        }

        /// <summary>
        /// Stop world path so the caravan holds position for the taxi.
        /// </summary>
        public static void StopMovementForTaxi(Caravan caravan)
        {
            if (caravan?.pather == null)
            {
                return;
            }

            // Always StopDead — clears path even if not currently moving
            try
            {
                if (caravan.pather.Moving)
                {
                    caravan.pather.StopDead();
                    Log.Message($"[RimTaxi] Caravan#{caravan.ID} stopped for taxi hold.");
                }
            }
            catch (System.Exception e)
            {
                Log.Warning("[RimTaxi] StopMovementForTaxi: " + e.Message);
            }
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

