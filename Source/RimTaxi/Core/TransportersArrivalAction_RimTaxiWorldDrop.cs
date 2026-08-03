using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTaxi
{
    /// <summary>
    /// Always form a player caravan on the world map.
    /// StillValid must never fail — otherwise TravellingTransporters.Arrived nulls this action
    /// and falls back to LandInSpecificCell (map drop / combat) or pawn loss.
    /// </summary>
    public class TransportersArrivalAction_RimTaxiWorldDrop : TransportersArrivalAction
    {
        private string arrivalMessageKey = "RimTaxi_ArrivedCaravan";

        private static readonly List<Pawn> tmpPawns = new List<Pawn>();
        private static readonly List<Thing> tmpThings = new List<Thing>();

        public override bool GeneratesMap => false;

        public TransportersArrivalAction_RimTaxiWorldDrop()
        {
        }

        public TransportersArrivalAction_RimTaxiWorldDrop(string arrivalMessageKey)
        {
            this.arrivalMessageKey = arrivalMessageKey;
        }

        /// <summary>
        /// CRITICAL: must stay valid on settlement tiles / loaded maps.
        /// Returning false causes vanilla to replace us with map drop or destroy contents.
        /// </summary>
        public override FloatMenuAcceptanceReport StillValid(IEnumerable<IThingHolder> pods, PlanetTile destinationTile)
        {
            return destinationTile.Valid;
        }

        public override bool ShouldUseLongEvent(List<ActiveTransporterInfo> pods, PlanetTile tile)
        {
            return false;
        }

        public override void Arrived(List<ActiveTransporterInfo> transporters, PlanetTile tile)
        {
            Log.Message($"[RimTaxi] WorldDrop.Arrived tile={tile} transporters={transporters?.Count ?? 0}");

            CollectPawnsAndThings(transporters, tmpPawns, tmpThings);

            PlanetTile dropTile = ResolveDropTile(tile);

            if (tmpPawns.Count == 0)
            {
                Log.Warning("[RimTaxi] WorldDrop: no pawns found in transporters; attempting shuttle salvage only.");
                // Still try to place items as abandoned on tile if any — better than silent loss.
                if (tmpThings.Count > 0 && tmpThings.Exists(t => t is Pawn) == false)
                {
                    // Need at least one pawn for caravan; create temporary handling: pass things to world at tile
                    for (int i = 0; i < tmpThings.Count; i++)
                    {
                        Find.WorldPawns.PassToWorld(tmpThings[i] as Pawn); // only pawns
                    }
                }

                Messages.Message("RimTaxi_ArrivedNoPawns".Translate(), new GlobalTargetInfo(dropTile), MessageTypeDefOf.NegativeEvent);
                tmpPawns.Clear();
                tmpThings.Clear();
                return;
            }

            // Ensure pawns are world pawns before caravan
            for (int i = 0; i < tmpPawns.Count; i++)
            {
                Pawn p = tmpPawns[i];
                if (!p.IsWorldPawn())
                {
                    Find.WorldPawns.PassToWorld(p);
                }
            }

            Caravan caravan = CaravanMaker.MakeCaravan(tmpPawns, Faction.OfPlayer, dropTile, addToWorldPawnsIfNotAlready: true);

            for (int i = 0; i < tmpThings.Count; i++)
            {
                Thing thing = tmpThings[i];
                if (thing == null || thing.Destroyed)
                {
                    continue;
                }

                if (thing is Pawn)
                {
                    continue; // already in caravan
                }

                CaravanInventoryUtility.GiveThing(caravan, thing);
            }

            tmpPawns.Clear();
            tmpThings.Clear();

            Messages.Message(arrivalMessageKey.Translate(), caravan, MessageTypeDefOf.TaskCompletion);
            CameraJumper.TryJumpAndSelect(caravan);
            Log.Message($"[RimTaxi] WorldDrop caravan formed at {dropTile} with {caravan.PawnsListForReading.Count} pawns.");
        }

        /// <summary>
        /// Pull pawns/things from top-level containers and nested shuttle contents.
        /// </summary>
        public static void CollectPawnsAndThings(
            List<ActiveTransporterInfo> transporters,
            List<Pawn> pawnsOut,
            List<Thing> thingsOut)
        {
            pawnsOut.Clear();
            thingsOut.Clear();
            if (transporters == null)
            {
                return;
            }

            for (int i = 0; i < transporters.Count; i++)
            {
                ActiveTransporterInfo info = transporters[i];
                if (info == null)
                {
                    continue;
                }

                // Nested shuttle first (may hold extra state)
                Thing shuttle = info.GetShuttle();
                if (shuttle != null)
                {
                    CompTransporter shuttleTrans = shuttle.TryGetComp<CompTransporter>();
                    if (shuttleTrans != null)
                    {
                        DrainThingOwner(shuttleTrans.GetDirectlyHeldThings(), pawnsOut, thingsOut);
                    }
                }

                DrainThingOwner(info.innerContainer, pawnsOut, thingsOut);

                // Official shuttle extract after draining
                if (info.GetShuttle() != null)
                {
                    Thing removed = info.RemoveShuttle();
                    if (removed != null && !thingsOut.Contains(removed))
                    {
                        thingsOut.Add(removed);
                    }
                }
            }
        }

        private static void DrainThingOwner(ThingOwner owner, List<Pawn> pawnsOut, List<Thing> thingsOut)
        {
            if (owner == null || owner.Count == 0)
            {
                return;
            }

            List<Thing> snapshot = new List<Thing>(owner.Count);
            for (int i = 0; i < owner.Count; i++)
            {
                snapshot.Add(owner[i]);
            }

            for (int i = 0; i < snapshot.Count; i++)
            {
                Thing thing = snapshot[i];
                if (thing == null)
                {
                    continue;
                }

                owner.Remove(thing);
                if (thing is Pawn pawn)
                {
                    if (!pawnsOut.Contains(pawn))
                    {
                        pawnsOut.Add(pawn);
                    }
                }
                else if (!thingsOut.Contains(thing))
                {
                    thingsOut.Add(thing);
                }
            }
        }

        /// <summary>
        /// Drop beside settlements when possible so the caravan is not forced onto a base tile.
        /// </summary>
        public static PlanetTile ResolveDropTile(PlanetTile preferred)
        {
            if (preferred.Valid)
            {
                Settlement settlement = Find.WorldObjects.SettlementAt(preferred);
                MapParent mapParent = Find.WorldObjects.MapParentAt(preferred);
                if (settlement != null || mapParent != null)
                {
                    PlanetTile neighbor = FindAdjacentDropTile(preferred);
                    if (neighbor.Valid)
                    {
                        Log.Message($"[RimTaxi] Dropping beside settlement/map at {preferred} → {neighbor}");
                        return neighbor;
                    }
                }

                if (!Find.World.Impassable(preferred))
                {
                    return preferred;
                }
            }

            if (GenWorldClosest.TryFindClosestPassableTile(preferred, out PlanetTile found))
            {
                return found;
            }

            return preferred;
        }

        private static PlanetTile FindAdjacentDropTile(PlanetTile center)
        {
            List<PlanetTile> neighbors = new List<PlanetTile>();
            Find.WorldGrid.GetTileNeighbors(center, neighbors);
            for (int i = 0; i < neighbors.Count; i++)
            {
                PlanetTile n = neighbors[i];
                if (!n.Valid || Find.World.Impassable(n))
                {
                    continue;
                }

                if (Find.WorldObjects.SettlementAt(n) != null)
                {
                    continue;
                }

                if (Find.WorldObjects.MapParentAt(n) != null)
                {
                    continue;
                }

                return n;
            }

            return PlanetTile.Invalid;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref arrivalMessageKey, "arrivalMessageKey", "RimTaxi_ArrivedCaravan");
        }
    }
}
