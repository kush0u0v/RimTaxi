using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTaxi
{
    public class CompProperties_RimTaxiTrip : CompProperties
    {
        public CompProperties_RimTaxiTrip()
        {
            compClass = typeof(CompRimTaxiTrip);
        }
    }

    /// <summary>
    /// Booked destination + optional pre-depart map landing cell on the shuttle Thing.
    /// Also handles emergency evacuate when hull HP falls to 25% or below from damage.
    /// </summary>
    public class CompRimTaxiTrip : ThingComp
    {
        public const float EmergencyEvacHpFraction = 0.25f;

        public PlanetTile destination = PlanetTile.Invalid;
        public int distance;
        public bool booked;

        /// <summary>Pre-selected landing on destination map (before depart).</summary>
        public IntVec3 destLandingCell = IntVec3.Invalid;
        public int destLandingMapId = -1;
        public bool hasDestLanding;

        public bool emergencyEvacTriggered;

        public void Book(PlanetTile dest, int dist)
        {
            destination = dest;
            distance = dist < 0 ? 0 : dist;
            booked = dest.Valid;
            // New destination invalidates prior landing pick
            ClearLanding();
            Log.Message($"[RimTaxi] CompRimTaxiTrip booked dest={dest} dist={distance} on {parent}");
        }

        public void BookLanding(Map map, IntVec3 cell)
        {
            if (map == null || !cell.IsValid)
            {
                ClearLanding();
                return;
            }

            destLandingMapId = map.uniqueID;
            destLandingCell = cell;
            hasDestLanding = true;
            Log.Message($"[RimTaxi] CompRimTaxiTrip landing booked map#{destLandingMapId} cell={cell}");
        }

        public void ClearLanding()
        {
            hasDestLanding = false;
            destLandingCell = IntVec3.Invalid;
            destLandingMapId = -1;
        }

        public void Clear()
        {
            booked = false;
            destination = PlanetTile.Invalid;
            distance = 0;
            ClearLanding();
        }

        public bool TryGet(out PlanetTile dest, out int dist)
        {
            dest = destination;
            dist = distance;
            return booked && destination.Valid;
        }

        public bool TryGetLandingForMap(Map map, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (!hasDestLanding || map == null || destLandingMapId != map.uniqueID || !destLandingCell.IsValid)
            {
                return false;
            }

            cell = destLandingCell;
            return true;
        }

        public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostPostApplyDamage(dinfo, totalDamageDealt);
            TryEmergencyEvacuate();
        }

        /// <summary>
        /// Hull at or below 25% max HP from damage → dump contents and fly away empty.
        /// </summary>
        public void TryEmergencyEvacuate()
        {
            if (emergencyEvacTriggered || parent == null || parent.Destroyed || !parent.Spawned)
            {
                return;
            }

            int max = parent.MaxHitPoints;
            if (max <= 0)
            {
                return;
            }

            if (parent.HitPoints > max * EmergencyEvacHpFraction)
            {
                return;
            }

            emergencyEvacTriggered = true;
            Map map = parent.Map;
            IntVec3 near = parent.Position;

            Log.Warning($"[RimTaxi] Emergency evacuate: HP {parent.HitPoints}/{max} at {near} on {map}");

            // Dump passengers / cargo onto the map
            CompTransporter transporter = parent.TryGetComp<CompTransporter>();
            if (transporter?.innerContainer != null && transporter.innerContainer.Count > 0)
            {
                List<Thing> items = new List<Thing>();
                items.AddRange(transporter.innerContainer);
                for (int i = 0; i < items.Count; i++)
                {
                    Thing t = items[i];
                    if (t == null)
                    {
                        continue;
                    }

                    transporter.innerContainer.Remove(t);
                    if (!t.Destroyed)
                    {
                        GenPlace.TryPlaceThing(t, near, map, ThingPlaceMode.Near);
                    }
                }
            }

            CompShuttle compShuttle = parent.TryGetComp<CompShuttle>();
            TransportShip ship = compShuttle?.shipParent;
            if (ship != null)
            {
                TaxiTripLookup.Clear(ship);
                ShipJob_FlyAway fly = (ShipJob_FlyAway)ShipJobMaker.MakeShipJob(ShipJobDefOf.FlyAway);
                fly.dropMode = TransportShipDropMode.None;
                ship.ForceJob(fly);
            }
            else
            {
                // No ship pipeline — just destroy empty hull so it is gone
                if (!parent.Destroyed)
                {
                    parent.Destroy(DestroyMode.Vanish);
                }
            }

            Messages.Message(
                "RimTaxi_EmergencyEvacuate".Translate(),
                new LookTargets(near, map),
                MessageTypeDefOf.NegativeEvent);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref destination, "rimTaxiDestination");
            Scribe_Values.Look(ref distance, "rimTaxiDistance", 0);
            Scribe_Values.Look(ref booked, "rimTaxiBooked", defaultValue: false);
            Scribe_Values.Look(ref destLandingCell, "rimTaxiDestLandingCell", IntVec3.Invalid);
            Scribe_Values.Look(ref destLandingMapId, "rimTaxiDestLandingMapId", -1);
            Scribe_Values.Look(ref hasDestLanding, "rimTaxiHasDestLanding", defaultValue: false);
            Scribe_Values.Look(ref emergencyEvacTriggered, "rimTaxiEmergencyEvac", defaultValue: false);
        }

        public override string CompInspectStringExtra()
        {
            if (!booked || !destination.Valid)
            {
                return null;
            }

            string s = "RimTaxi_InspectBooked".Translate(distance);
            if (hasDestLanding && destLandingCell.IsValid)
            {
                s += "\n" + "RimTaxi_InspectLanding".Translate(destLandingCell.ToString());
            }

            return s;
        }
    }
}
