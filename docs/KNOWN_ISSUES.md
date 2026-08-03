# Known issues & UX traps

## Call / pickup

- **Only one player settlement** → menu still shows, but only “here” options (expected).
- **Field map pickup** requires the map **already open** with free colonists. Pure world caravans (no map) are **not** pickup targets yet — enter the tile first (no permanent camp required).
- Call fee is always taken from the **comms map**, even if pickup is remote.
- **Pending dispatch** blocks another taxi to the **same** pickup map until it lands or fails.

## Destination / depart

- Destination is **not** chosen at call (by design). Use gizmo **Set destination** after land.
- Loaded taxi + wait expired + **no destination** → re-wait + message (does not fly loaded blind).
- Empty taxi after wait may leave with no trip fare.
- **Old taxis** from before `CompRimTaxiTrip` may lack booking; re-call.

## Arrival

- Map land uses **custom DropRimTaxi** (vanilla DropShuttle NRE if `shipDef` missing).
- Hostile settlements: generating/landing on map can still cause combat/relations (vanilla-like).
- If map land fails, fallback is **world caravan** drop-off.
- World drop may place caravan on a **neighbor tile** beside a settlement.

## Economy

- Call fee spent even if player never boards (dispatch no-show cost).
- Trip fare on **taxi’s current map** silver — remote pickup needs silver there to depart (or haul silver first).
- Large mass × long distance can dwarf the 200 call fee.

## Graphics

- `drawSize (7.2, 5.8)` vs collision `size (5,3)`.
- Shadow intentionally small; replace PNGs under `Textures/` if art updates.

## Compatibility

- Odyssey orbit layers lightly tested.
- Other shuttle/TransportShip mods may conflict.
- Prisoners-only loads may fail caravan formation on world drop.

## Save/load

- Pending dispatches and trips are Scribed on `TaxiGameComponent` / comps.
- Keep mod enabled when taxis are in flight (custom arrival action types).

## Reporting

Include: RimWorld version, Harmony, steps, `Player.log` lines with `[RimTaxi]`.
