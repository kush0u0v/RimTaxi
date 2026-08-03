using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTaxi
{
    /// <summary>
    /// Where a taxi can be sent for pickup — player homes, other settlements,
    /// and any open map that already has free colonists (quest/raid/caravan maps).
    /// No need to found a camp/settlement on that map.
    /// </summary>
    public class TaxiPickupSite
    {
        public string label;
        public Map openMap;
        public MapParent mapParent;
        public PlanetTile tile;

        public bool HasOpenMap => openMap != null;

        public static List<TaxiPickupSite> GetAll(Map callMap = null)
        {
            var list = new List<TaxiPickupSite>();
            var seenTiles = new HashSet<int>();
            var seenMapIds = new HashSet<int>();

            // 1) Every open map that already has free colonists (home OR temp field map)
            for (int i = 0; i < Find.Maps.Count; i++)
            {
                Map map = Find.Maps[i];
                if (map == null)
                {
                    continue;
                }

                int freeColonists = map.mapPawns?.FreeColonistsCount ?? 0;
                // Player homes always listed; field/quest/raid maps only if colonists are there (no camp required).
                if (!map.IsPlayerHome && freeColonists <= 0)
                {
                    continue;
                }

                if (seenMapIds.Contains(map.uniqueID))
                {
                    continue;
                }

                seenMapIds.Add(map.uniqueID);
                int tileId = map.Tile.tileId;
                seenTiles.Add(tileId);

                string name = map.Parent?.LabelCap ?? map.info?.parent?.LabelCap ?? map.ToString();
                bool isHere = callMap != null && map == callMap;
                string label;
                if (isHere)
                {
                    label = "RimTaxi_PickupSiteHere".Translate(name);
                }
                else if (map.IsPlayerHome)
                {
                    label = "RimTaxi_PickupSiteOtherOpen".Translate(name);
                }
                else
                {
                    // Temp / quest / raid map — no settlement/camp founding needed
                    label = "RimTaxi_PickupSiteTempMap".Translate(name, freeColonists);
                }

                list.Add(new TaxiPickupSite
                {
                    label = label,
                    openMap = map,
                    mapParent = map.Parent,
                    tile = map.Tile
                });
            }

            // 2) Player settlements not already listed (map unloaded — no need to camp elsewhere)
            foreach (Settlement settlement in Find.WorldObjects.Settlements)
            {
                if (settlement?.Faction == null || !settlement.Faction.IsPlayer)
                {
                    continue;
                }

                int tileId = settlement.Tile.tileId;
                if (seenTiles.Contains(tileId))
                {
                    continue;
                }

                seenTiles.Add(tileId);
                list.Add(new TaxiPickupSite
                {
                    label = "RimTaxi_PickupSiteOtherClosed".Translate(settlement.LabelCap),
                    openMap = settlement.Map,
                    mapParent = settlement,
                    tile = settlement.Tile
                });
            }

            // Stable order: current map first, then homes, then temp, then closed
            list = list
                .OrderByDescending(s => callMap != null && s.openMap == callMap)
                .ThenByDescending(s => s.openMap != null && s.openMap.IsPlayerHome)
                .ThenBy(s => s.label)
                .ToList();

            return list;
        }
    }
}
