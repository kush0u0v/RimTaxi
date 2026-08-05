# Known issues & UX traps

## Call / pickup

- **Only one player settlement** → menu still shows, but only “here” options (expected).
- **Field map pickup** (from comms list) requires the map **already open** with free colonists.
- Comms menu also lists **player caravans** and has **「월드맵에서 픽업 위치 선택…」**.
- Comms call fee is taken from the **comms map**, even if pickup is a remote map or caravan.
- **World caravans** can also call/board from the caravan top bar (fee from caravan silver).
- **Pending dispatch** blocks another taxi to the **same** pickup map until it lands or fails.
- Caravan **cannot move** while taxi is en route or waiting (`CantMove` + `StartPath`).
- Caravan + taxi (pending/boarding): **world icon is taxi**, not yellow circle, until **하차** or **출발**.
- Landing cell + **Q/E** only at **call** or when **map is visible for arrival landing** (not mid-wait reposition).

## Destination / depart

- **Map call:** destination after land (design). **Caravan send:** destination chosen at call, changeable while en route / boarding.
- Manual **Depart** clears trip booking **before** `ForceJob(FlyAway)` so billing patch does not double-charge trip fare.
- Loaded taxi + wait expired + **no destination** → re-wait + message (does not fly loaded blind).
- Empty taxi after wait may leave with no trip fare.
- **Old taxis** from before `CompRimTaxiTrip` may lack booking; re-call.

## Arrival

- Map land uses **custom DropRimTaxi** (vanilla DropShuttle NRE if `shipDef` missing).
- **Player colonies only** for map land. On land: unload + **layover wait** (reboard → new dest → depart). Empty taxi leaves after wait.
- **Foreign faction settlements never open the map** — world caravan **on that tile** + layover boarding gizmos. Entering the settlement destroys the caravan (boarding ends); call again after reforming if needed.
- If map land fails, fallback is **world caravan** drop-off.
- World drop only moves to a neighbor if the destination tile is **impassable**.

## Economy

- **Call fee is the default cost:** prepaid at call (`baseFare` default 200). No refund on no-board, disembark, or empty leave after wait. Trip fare only on Depart.
- Silver rules: **settlement + trade beacon** = beacon radius + carried; **settlement no beacon** = stockpile + carried; **field** = carried only; **caravan taxi** = all open settlements’ beacon silver + caravan inventory.
- Trip fare from taxi’s current map / caravan — remote depart needs silver under those rules there.
- Large mass × long distance can dwarf the 200 call fee.

## Graphics

- `drawSize (7.2, 5.8)` vs collision `size (5,3)`.
- Skyfaller shadow tracks craft XZ (`RimTaxiIncoming` / `RimTaxiLeaving`) so angled approach does not look like a vertical drop pad-only blob.
- In-flight WO: `Textures/World/WorldObjects(/Expanding)/RimTaxi.png`.
- Caravan-hold taxi icon uses same expanding texture via `Caravan_WorldIcon_Patch`.
- 하차 gizmo icon: `Textures/UI/Commands/RimTaxiDisembark.png`.
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
