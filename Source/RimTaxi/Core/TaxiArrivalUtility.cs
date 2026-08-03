using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTaxi
{
    /// <summary>
    /// Picks arrival style: map land only on player-owned content; foreign settlements always world caravan.
    /// </summary>
    public static class TaxiArrivalUtility
    {
        public static bool PreferMapLanding => RimTaxiMod.Settings == null || RimTaxiMod.Settings.landOnSettlementMaps;

        public static TransportersArrivalAction CreateArrivalAction(PlanetTile tile)
        {
            if (!tile.Valid)
            {
                return new TransportersArrivalAction_RimTaxiWorldDrop("RimTaxi_ArrivedCaravan");
            }

            if (PreferMapLanding)
            {
                Settlement settlement = Find.WorldObjects.SettlementAt(tile);
                if (settlement != null && settlement.Spawned)
                {
                    // Other factions: never open the base map (instant raid/combat). Caravan beside tile.
                    if (!IsPlayerOwned(settlement))
                    {
                        Log.Message($"[RimTaxi] Arrival action: WorldDrop beside foreign settlement {settlement.Label} ({settlement.Faction?.Name})");
                        return new TransportersArrivalAction_RimTaxiWorldDrop("RimTaxi_ArrivedCaravan");
                    }

                    Log.Message($"[RimTaxi] Arrival action: MapLand on player settlement {settlement.Label}");
                    return new TransportersArrivalAction_RimTaxiMapLand(settlement);
                }

                MapParent mapParent = Find.WorldObjects.MapParentAt(tile);
                if (mapParent != null && mapParent.Spawned)
                {
                    if (IsPlayerOwned(mapParent))
                    {
                        Log.Message($"[RimTaxi] Arrival action: MapLand on player MapParent {mapParent.LabelCap}");
                        return new TransportersArrivalAction_RimTaxiMapLand(mapParent);
                    }

                    // Already-open non-settlement map (quest/raid site player is on) — land is fine.
                    // Unopened foreign bases/sites: stay on world map as caravan.
                    if (mapParent.HasMap)
                    {
                        Log.Message($"[RimTaxi] Arrival action: MapLand on open map {mapParent.LabelCap}");
                        return new TransportersArrivalAction_RimTaxiMapLand(mapParent);
                    }

                    Log.Message($"[RimTaxi] Arrival action: WorldDrop at closed non-player MapParent {mapParent.LabelCap}");
                    return new TransportersArrivalAction_RimTaxiWorldDrop("RimTaxi_ArrivedCaravan");
                }

                // Player home map already loaded for this tile
                for (int i = 0; i < Find.Maps.Count; i++)
                {
                    Map map = Find.Maps[i];
                    if (map.Tile != tile || map.Parent == null)
                    {
                        continue;
                    }

                    if (map.IsPlayerHome || IsPlayerOwned(map.Parent))
                    {
                        Log.Message($"[RimTaxi] Arrival action: MapLand on open player map {map}");
                        return new TransportersArrivalAction_RimTaxiMapLand(map.Parent);
                    }
                }
            }

            Log.Message($"[RimTaxi] Arrival action: WorldDrop caravan at {tile}");
            return new TransportersArrivalAction_RimTaxiWorldDrop("RimTaxi_ArrivedCaravan");
        }

        /// <summary>
        /// Player colony / player settlement only — not allied or hostile foreign bases.
        /// </summary>
        public static bool IsPlayerOwned(MapParent parent)
        {
            if (parent == null)
            {
                return false;
            }

            if (parent.Faction != null && parent.Faction.IsPlayer)
            {
                return true;
            }

            if (parent.HasMap && parent.Map.IsPlayerHome)
            {
                return true;
            }

            return false;
        }
    }
}
