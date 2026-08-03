using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTaxi
{
    /// <summary>
    /// Picks arrival style: taxi map landing on settlements/maps, else world caravan drop-off.
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
                    Log.Message($"[RimTaxi] Arrival action: MapLand on settlement {settlement.Label}");
                    return new TransportersArrivalAction_RimTaxiMapLand(settlement);
                }

                MapParent mapParent = Find.WorldObjects.MapParentAt(tile);
                if (mapParent != null && mapParent.Spawned)
                {
                    Log.Message($"[RimTaxi] Arrival action: MapLand on MapParent {mapParent.LabelCap}");
                    return new TransportersArrivalAction_RimTaxiMapLand(mapParent);
                }

                // Already-generated map for this tile (e.g. player base currently loaded)
                for (int i = 0; i < Find.Maps.Count; i++)
                {
                    Map map = Find.Maps[i];
                    if (map.Tile == tile && map.Parent != null)
                    {
                        Log.Message($"[RimTaxi] Arrival action: MapLand on open map {map}");
                        return new TransportersArrivalAction_RimTaxiMapLand(map.Parent);
                    }
                }
            }

            Log.Message($"[RimTaxi] Arrival action: WorldDrop caravan at {tile}");
            return new TransportersArrivalAction_RimTaxiWorldDrop("RimTaxi_ArrivedCaravan");
        }
    }
}
