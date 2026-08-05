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
1. Call          → pay CALL FEE (default 200) — prepaid default cost
2. Dispatch      → ETA 1–3 in-game hours
3. Arrive        → map land (cell+Q/E) OR caravan “taxi ready” (world icon = taxi)
4. Set destination → world map; mass×distance preview (no charge yet)
5. Depart        → trip fare, then fly
```

### Pickup (step 1 UI)

**From comms (radio Dialog_Negotiation GUI):**

1. Request taxi  
2. **In the same radio dialog:** choose where to send (pickup sites + world map pick)  
3. Map landing: **cell + Q/E**  
- Call fee from **comms map** (even if remote pickup)

**From player caravan (top bar):**

- **택시 보내기** → dest → call fee → ETA → **출발** / **하차**
- While en route or boarding: caravan **cannot move**; **world icon = taxi** (not yellow circle)
- Silver: open settlements’ **trade-beacon** silver + caravan inventory
- **하차**: dismiss taxi layover, free movement; call fee not refunded

### Landing position (only these times)

1. **Call** — map is shown → pick cell + **Q/E**
2. **Arrival on player map** — map is shown → pick cell + **Q/E**, then drop + wait layover

No mid-wait “move pad anywhere” gizmo.

### After land on map (steps 4–5)

- Gizmos: set dest / depart / dismiss
- Wait default **2h**; empty leave free; loaded no dest → re-wait

### Arrival at trip destination

- **Player maps:** MapLand → **player picks landing** → unload + wait layover
- **Foreign settlements:** WorldDrop **on settlement tile** + caravan boarding (no map combat)
- `landOnSettlementMaps` (default true) affects player maps only

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
| `TaxiCallService` | Call / pickup / dispatch / caravan send / reposition / depart |
| `TaxiLandingUtility` | Land checks, ghost, **Q/E** placement rotation |
| `TaxiPickupSite` | Pickup maps / settlements / field / caravans |
| `TaxiPendingDispatch` + `TaxiGameComponent` | ETA queue (+ `landingRot`); caravan boarding; cooldown |
| `TaxiCaravanBoarding` / `TaxiCaravanUtility` | Layover, launch, immobilize, **taxi world icon** |
| `Caravan_Gizmos_Patch` | Send / en route / board / **하차** |
| `Caravan_Movement_Patch` | Immobilize while taxi en route or waiting |
| `Caravan_WorldIcon_Patch` | Taxi icon instead of yellow caravan circle |
| `CompRimTaxiTrip` / `TaxiTripLookup` | Booked destination on shuttle |
| `TaxiPayment` | Beacon / stockpile / carried / caravan dual pool |
| `TaxiFareCalculator` / `TaxiTripBilling` | Call vs trip fare |
| `TaxiArrivalUtility` + MapLand / WorldDrop | Arrival chooser |
| `TaxiCommsContact` / `TaxiDialogMaker` | Radio UI |
| `ShipJob_Wait_Gizmos_Patch` | Dest / depart / **reposition** / dismiss |
| `ShipJob_FlyAway_Billing_Patch` | Auto fare / re-wait / empty leave |
| Arrival / speed patches | Force arrival; slower flight |

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
| **1. Call** | `baseFare` (**default 200**) | Call map / caravan via `TaxiPayment` |
| **5. Depart** | `ceil(massKg × tiles × farePerKgPerTile)` | Taxi map / caravan via `TaxiPayment` |
| Empty leave / no-board / disembark / wait expire | **Trip fare 0** | — |

### Call fee is the default cost (locked)

- **`baseFare` default = 200 silver** — settings may change amount, but the *model* is fixed: **dispatch is prepaid**.
- Call fee is charged when the call is accepted (comms or caravan send).
- **No refund by default** if the player never boards, cancels by waiting out, disembarks on world map, or the taxi leaves empty after wait.
- **Only exception-style refunds** already in code for hard failures (e.g. spawn/landing failed) — not for player no-show.
- Trip fare is separate and only at **Depart**.

**Silver sources (`TaxiPayment`) — trade beacon = player settlement orbital trade beacon:**

- **Player settlement with trade beacon(s):** silver in beacon radius + carried by player pawns
- **Player settlement without trade beacon:** stockpile/storage + carried
- **Field / temp maps:** carried by player pawns only
- **Caravan taxi (call / board / depart):** silver in trade-beacon radius on **any open player settlement** + silver the caravan carries (spend settlement beacons first, then caravan inv)
- Spend order on maps: ground/storage/beacon first, then pawn inventory

Defaults:

| Setting | Default | Notes |
|---------|---------|--------|
| `baseFare` | 200 | Call fee |
| `farePerKgPerTile` | 0.1 | Trip rate |
| `dispatchBaseTicks` | 2500 | 1h base ETA |
| `dispatchVarianceTicks` | 5000 | +0–2h → **call arrival 1–3h** |
| `dispatchTicksPerTripTile` | 0 | Keep ETA in 1–3h band |
| `waitTicks` | **5000** | **2h** boarding/layover after land (after a ride) |
| `cooldownTicks` | 2500 | 1h between calls |
| `maxLaunchDistance` | 70 | Trip range |
| `travelSpeedFactor` | 0.6 | Slower world flight |
| `landOnSettlementMaps` | true | Map land vs caravan |

---

## Known traps (see also `docs/KNOWN_ISSUES.md`)

- Vanilla `DropShuttle` NRE if `shipDef` missing — use `DropRimTaxi` + XML `shipDef`.
- Destination is **not** set at call; only at step 4. Do not reintroduce world map at call without asking.
- Caravan can call/board taxi from world map (no open map required).
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
