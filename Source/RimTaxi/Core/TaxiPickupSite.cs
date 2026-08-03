using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimTaxi
{
    public enum TaxiPickupKind
    {
        Map,
        Settlement,
        Caravan
    }

    /// <summary>
    /// Where a taxi can be sent for pickup — player homes, other settlements,
    /// open field maps with colonists, and player caravans (no camp required).
    /// </summary>
    public class TaxiPickupSite
    {
        public string label;
        public TaxiPickupKind kind;
        public Map openMap;
        public MapParent mapParent;
        public Caravan caravan;
        public PlanetTile tile;

        public bool HasOpenMap => openMap != null;
        public bool IsCaravan => kind == TaxiPickupKind.Caravan && caravan != null;

        public static List<TaxiPickupSite> GetAll(Map callMap = null)
        {
            var list = new List<TaxiPickupSite>();
            var seenMapIds = new HashSet<int>();
            var seenTileKeys = new HashSet<string>();
            var seenCaravanIds = new HashSet<int>();

            // 1) Every open map that has player-controlled free colonists (or is a player home)
            for (int i = 0; i < Find.Maps.Count; i++)
            {
                Map map = Find.Maps[i];
                if (map == null)
                {
                    continue;
                }

                int freeColonists = CountFreeColonists(map);
                // Player homes always listed; field/quest/raid maps if any free colonist is there
                if (!map.IsPlayerHome && freeColonists <= 0)
                {
                    continue;
                }

                if (seenMapIds.Contains(map.uniqueID))
                {
                    continue;
                }

                seenMapIds.Add(map.uniqueID);
                seenTileKeys.Add(TileKey(map.Tile));

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
                    label = "RimTaxi_PickupSiteTempMap".Translate(name, freeColonists);
                }

                list.Add(new TaxiPickupSite
                {
                    label = label,
                    kind = TaxiPickupKind.Map,
                    openMap = map,
                    mapParent = map.Parent,
                    tile = map.Tile
                });
            }

            // 2) Player settlements not already listed (map unloaded)
            List<Settlement> settlements = Find.WorldObjects?.Settlements;
            if (settlements != null)
            {
                for (int i = 0; i < settlements.Count; i++)
                {
                    Settlement settlement = settlements[i];
                    if (settlement?.Faction == null || !settlement.Faction.IsPlayer)
                    {
                        continue;
                    }

                    string key = TileKey(settlement.Tile);
                    if (seenTileKeys.Contains(key))
                    {
                        continue;
                    }

                    // If settlement map is open but was skipped (no colonists), still allow as closed-style entry
                    if (settlement.HasMap && seenMapIds.Contains(settlement.Map.uniqueID))
                    {
                        continue;
                    }

                    seenTileKeys.Add(key);
                    list.Add(new TaxiPickupSite
                    {
                        label = "RimTaxi_PickupSiteOtherClosed".Translate(settlement.LabelCap),
                        kind = TaxiPickupKind.Settlement,
                        openMap = settlement.HasMap ? settlement.Map : null,
                        mapParent = settlement,
                        tile = settlement.Tile
                    });
                }
            }

            // 3) Player caravans on the world map (colonists not on any map)
            List<Caravan> caravans = Find.WorldObjects?.Caravans;
            if (caravans != null)
            {
                for (int i = 0; i < caravans.Count; i++)
                {
                    Caravan caravan = caravans[i];
                    if (caravan == null || caravan.Destroyed || !caravan.IsPlayerControlled)
                    {
                        continue;
                    }

                    if (caravan.PawnsListForReading == null || caravan.PawnsListForReading.Count == 0)
                    {
                        continue;
                    }

                    if (seenCaravanIds.Contains(caravan.ID))
                    {
                        continue;
                    }

                    seenCaravanIds.Add(caravan.ID);
                    int pawns = caravan.PawnsListForReading.Count;
                    string name = caravan.Name ?? caravan.LabelCap;
                    list.Add(new TaxiPickupSite
                    {
                        label = "RimTaxi_PickupSiteCaravan".Translate(name, pawns),
                        kind = TaxiPickupKind.Caravan,
                        caravan = caravan,
                        tile = caravan.Tile
                    });
                }
            }

            // Stable order: current map first, then homes, then field maps, then caravans, then closed
            list = list
                .OrderByDescending(s => callMap != null && s.openMap == callMap)
                .ThenByDescending(s => s.openMap != null && s.openMap.IsPlayerHome)
                .ThenBy(s => s.kind == TaxiPickupKind.Caravan)
                .ThenBy(s => s.kind == TaxiPickupKind.Settlement)
                .ThenBy(s => s.label)
                .ToList();

            return list;
        }

        public static int CountFreeColonists(Map map)
        {
            if (map?.mapPawns == null)
            {
                return 0;
            }

            return map.mapPawns.FreeColonistsSpawnedCount;
        }

        public static string TileKey(PlanetTile tile)
        {
            if (!tile.Valid)
            {
                return "invalid";
            }

            // GetHashCode includes layer in 1.6 multi-layer worlds
            return tile.GetHashCode().ToString();
        }

        /// <summary>
        /// Resolve a world-map click into a pickup site (caravan / player settlement / open map parent).
        /// </summary>
        public static TaxiPickupSite FromWorldTarget(GlobalTargetInfo target)
        {
            if (!target.IsValid)
            {
                return null;
            }

            WorldObject wo = target.WorldObject;
            if (wo is Caravan caravan && caravan.IsPlayerControlled && !caravan.Destroyed)
            {
                int pawns = caravan.PawnsListForReading?.Count ?? 0;
                if (pawns <= 0)
                {
                    return null;
                }

                return new TaxiPickupSite
                {
                    label = "RimTaxi_PickupSiteCaravan".Translate(caravan.Name ?? caravan.LabelCap, pawns),
                    kind = TaxiPickupKind.Caravan,
                    caravan = caravan,
                    tile = caravan.Tile
                };
            }

            if (wo is Settlement settlement && settlement.Faction != null && settlement.Faction.IsPlayer)
            {
                return new TaxiPickupSite
                {
                    label = settlement.HasMap
                        ? "RimTaxi_PickupSiteOtherOpen".Translate(settlement.LabelCap)
                        : "RimTaxi_PickupSiteOtherClosed".Translate(settlement.LabelCap),
                    kind = settlement.HasMap ? TaxiPickupKind.Map : TaxiPickupKind.Settlement,
                    openMap = settlement.HasMap ? settlement.Map : null,
                    mapParent = settlement,
                    tile = settlement.Tile
                };
            }

            if (wo is MapParent mapParent && mapParent.HasMap)
            {
                Map map = mapParent.Map;
                int free = CountFreeColonists(map);
                if (map.IsPlayerHome || free > 0)
                {
                    return new TaxiPickupSite
                    {
                        label = map.IsPlayerHome
                            ? "RimTaxi_PickupSiteOtherOpen".Translate(mapParent.LabelCap)
                            : "RimTaxi_PickupSiteTempMap".Translate(mapParent.LabelCap, free),
                        kind = TaxiPickupKind.Map,
                        openMap = map,
                        mapParent = mapParent,
                        tile = mapParent.Tile
                    };
                }
            }

            // Bare tile: open map on that tile with colonists?
            if (target.Tile.Valid)
            {
                for (int i = 0; i < Find.Maps.Count; i++)
                {
                    Map map = Find.Maps[i];
                    if (map == null || map.Tile != target.Tile)
                    {
                        continue;
                    }

                    int free = CountFreeColonists(map);
                    if (map.IsPlayerHome || free > 0)
                    {
                        string name = map.Parent?.LabelCap ?? map.ToString();
                        return new TaxiPickupSite
                        {
                            label = map.IsPlayerHome
                                ? "RimTaxi_PickupSiteOtherOpen".Translate(name)
                                : "RimTaxi_PickupSiteTempMap".Translate(name, free),
                            kind = TaxiPickupKind.Map,
                            openMap = map,
                            mapParent = map.Parent,
                            tile = map.Tile
                        };
                    }
                }

                Settlement at = Find.WorldObjects.SettlementAt(target.Tile);
                if (at != null && at.Faction != null && at.Faction.IsPlayer)
                {
                    return FromWorldTarget(new GlobalTargetInfo(at));
                }

                Caravan carAt = Find.WorldObjects.PlayerControlledCaravanAt(target.Tile);
                if (carAt != null)
                {
                    return FromWorldTarget(new GlobalTargetInfo(carAt));
                }
            }

            return null;
        }
    }
}
