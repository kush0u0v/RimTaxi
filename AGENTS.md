# AGENTS.md — Guidance for AI coding agents

This repository is **RimTaxi**, a RimWorld 1.6 mod. **Read this file before changing code.**

Repo: https://github.com/kush0u0v/RimTaxi  
Local (typical): `C:\Users\KUSH\RimWorldMods\RimTaxi` (often junctioned to `RimWorld/Mods/RimTaxi`)

---

## Project goal / identity

**RimTaxi is a civilian on-demand taxi service**, not a player-owned spaceship fleet.

| Is | Is not |
|----|--------|
| Call a ride, pay a meter, get dropped off | SRTS / player ship ownership |
| Dispatch from comms (network) | Odyssey passenger-shuttle clone |
| Service fantasy | “Build/upgrade your ship” |

Do **not** brand features as SRTS-like in UI, docs, or commits.

---

## Locked player flow (do not redesign unless asked)

```
1. Call          → pay CALL FEE (default 200 silver) from comms map stockpile
2. Dispatch      → ETA 1–3 in-game hours (no world map at call time)
3. Arrive        → taxi lands at chosen PICKUP map/cell
4. Set destination → world map; show mass×distance fare estimate (no charge yet)
5. Depart        → charge trip fare, then fly
```

### Pickup (step 1 UI)

- Float menu: **where to send the taxi** (not “which faction to call”).
- Options include:
  - Current map
  - Other **player** settlements (open or closed → open map)
  - **Field maps** that already have free colonists (quest/raid/caravan maps) — **no camp required**
- Call fee is paid from the **comms console map**, not necessarily the pickup map.

### After land (steps 4–5)

- Gizmo **「4. 목적지 설정」** — world map, `CompRimTaxiTrip.Book`
- Gizmo **「5. 출발」** — needs boarded cargo + destination; `TaxiTripBilling` then `FlyAway`
- Boarding wait default **5h**; empty taxi may leave after wait; loaded without destination re-waits

### Arrival at trip destination

- Prefer **map land** only on **player-owned** settlements/maps (`TransportersArrivalAction_RimTaxiMapLand` / custom `DropRimTaxi`)
- **Foreign faction settlements: always world caravan** beside the tile — never generate/enter their map (no auto-combat)
- Else **world caravan** (`TransportersArrivalAction_RimTaxiWorldDrop`)
- Setting: `landOnSettlementMaps` (default true) — affects player maps only; foreign bases always caravan
- Harmony forces RimTaxi arrival path so vanilla `StillValid` fallback cannot wipe/banish pawns

---

## Hard rules

1. Investigate existing `Source/RimTaxi` + Defs before editing; do not invent RimWorld APIs.
2. Verify APIs via decompile / local `Assembly-CSharp.dll`.
3. No drive-by refactors; no unrelated files.
4. Save compatibility: new Scribe fields need defaults.
5. Harmony only when needed; filter patches to RimTaxi defs/things.
6. DLC-safe: no Odyssey hard dep; own `Ship_RimTaxi` (do not rely on Royalty `Ship_Shuttle` alone).
7. **Hospitality permanently out of scope.**
8. **No full custom GUI yet** — gizmos/float menus/letters only until user asks.
9. After changes: `dotnet build -c Release` in `Source/RimTaxi`.

---

## Environment

| Item | Value |
|------|--------|
| Game | RimWorld **1.6** |
| packageId | `kush.rimtaxi` |
| Namespace | `RimTaxi` |
| TFM | `net472` |
| Required mod | Harmony (`brrainz.harmony`) |

---

## Key types

| Type | Role |
|------|------|
| `TaxiCallService` | Call / pickup / dispatch / set dest / depart entry points |
| `TaxiPickupSite` | Lists pickup maps/settlements/field maps |
| `TaxiPendingDispatch` + `TaxiGameComponent` | Dispatch ETA queue; trip dict backup; cooldown |
| `CompRimTaxiTrip` | **Authoritative** destination+distance on shuttle Thing |
| `TaxiTripLookup` | Comp + GameComponent resolve |
| `TaxiPayment` | Map silver count/spend (not orbital-beacon-only) |
| `TaxiFareCalculator` / `TaxiTripBilling` | Call fee vs mass×distance trip fare |
| `TaxiArrivalUtility` | Map land vs world drop chooser |
| `TransportersArrivalAction_RimTaxiMapLand` | Safe map drop (`DropRimTaxi`, not vanilla DropShuttle NRE) |
| `TransportersArrivalAction_RimTaxiWorldDrop` | World caravan drop-off |
| `Building_CommsConsole_Patch` | Call gizmo + right-click |
| `ShipJob_Wait_Gizmos_Patch` | Set destination / Depart / Dismiss |
| `ShipJob_FlyAway_Billing_Patch` | Auto path after wait (fare / re-wait / empty leave) |
| `TravellingTransporters_Arrival_Patch` | Force our arrival for `TravelingRimTaxi` |
| `TravellingTransporters_Speed_Patch` | `travelSpeedFactor` on RimTaxi world flights |

### Defs

| Def | Role |
|-----|------|
| `Ship_RimTaxi` | TransportShipDef (`shipThing` RimTaxiShuttle) |
| `TravelingRimTaxi` | WorldObject in flight |
| `RimTaxiShuttle` | Vehicle; **must** have `CompProperties_Shuttle.shipDef = Ship_RimTaxi` |
| `RimTaxiIncoming` / `RimTaxiLeaving` | Skyfallers + custom shadow |

---

## Economy (do not regress)

| When | Silver | Taken from |
|------|--------|------------|
| **1. Call** | `baseFare` (200) | Comms / call map stockpile |
| **5. Depart** | `ceil(massKg × tiles × farePerKgPerTile)` | Map where taxi currently is |
| Empty leave | Trip fare 0 | — |

Defaults:

| Setting | Default | Notes |
|---------|---------|--------|
| `baseFare` | 200 | Call fee |
| `farePerKgPerTile` | 0.1 | Trip rate |
| `dispatchBaseTicks` | 2500 | 1h base ETA |
| `dispatchVarianceTicks` | 5000 | +0–2h → **call arrival 1–3h** |
| `dispatchTicksPerTripTile` | 0 | Keep ETA in 1–3h band |
| `waitTicks` | 12500 | 5h boarding window after land |
| `cooldownTicks` | 2500 | 1h between calls |
| `maxLaunchDistance` | 70 | Trip range |
| `travelSpeedFactor` | 0.6 | Slower world flight |
| `landOnSettlementMaps` | true | Map land vs caravan |

---

## Known traps (see also `docs/KNOWN_ISSUES.md`)

- Vanilla `DropShuttle` NRE if `shipDef` missing — use `DropRimTaxi` + XML `shipDef`.
- Destination is **not** set at call; only at step 4. Do not reintroduce world map at call without asking.
- Caravan **without open map** still not a pickup target.
- Old in-world taxis before Comp booking may lack destination until re-called.

---

## Build

```powershell
cd Source\RimTaxi
dotnet build -c Release
# → ../../Assemblies/RimTaxi.dll
```

Or repo root: `.\build.ps1`

---

## In-game smoke checklist

1. Log: `[RimTaxi] Loaded` (or current load message).
2. Call: float menu pickup list; **no world map**; pay 200; letter ETA ~1–3h.
3. Remote: pick other settlement / field map with colonists.
4. After land: set destination (world map) → fare preview; depart charges trip fare.
5. Settlement arrival: map land without DropShuttle NRE; fallback caravan if needed.
6. Save/load during dispatch wait and boarding wait.

---

## Docs map

| File | Purpose |
|------|---------|
| `README.md` | Players + quick start |
| `AGENTS.md` | **AI entry (this file)** |
| `CONTRIBUTING.md` | PR / agent reporting |
| `docs/ARCHITECTURE.md` | Systems diagram + patches |
| `docs/KNOWN_ISSUES.md` | Bugs / UX traps |
| `docs/ROADMAP.md` | Baseline freeze + next work |
| `docs/HANDOFF.md` | Session handoff summary (human + AI) |

## Style

- Match existing C# style; explicit braces; Verse/RimWorld patterns.
- Player text: English + Korean Keyed.
- Prefer small diffs; report files changed, build result, test steps, limits.
