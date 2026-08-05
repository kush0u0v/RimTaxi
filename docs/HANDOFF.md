# Handoff summary (current baseline)

**Date context:** 2026-08 — playable baseline; full GUI deferred.  
**GitHub:** https://github.com/kush0u0v/RimTaxi  
**packageId:** `kush.rimtaxi`

---

## What the mod is

Civilian **call taxi** for RimWorld 1.6:

- Dispatch from **comms console** (radio contact **RimTaxi 배차센터**) or **player caravan** top bar (**택시 보내기**).
- Taxi arrives after **1–3 hours** (not instant).
- Board at **pickup** (maps, field maps, caravan — no camp required).
- Map landing: **cell pick + Q/E rotate**; waiting taxi can **reposition** pad.
- Caravan + taxi (en route or waiting): **world icon = taxi** (not yellow circle) until **하차** or **출발**.
- Destination after taxi ready → **mass × distance** trip fare → fly.
- Trip end: **player maps** land + layover wait; **foreign settlements** world caravan **on tile** (no combat entry) + layover.

Identity: **service / meter**, not player spaceship (not SRTS).

---

## Canonical 5 steps

| # | Step | Player action | Silver |
|---|------|---------------|--------|
| 1 | Call | Comms or caravan **보내기** | **Call fee (default 200)** prepaid |
| 2 | Dispatch | Wait ETA | — |
| 3 | Arrive | Map land or caravan taxi ready | — |
| 4 | Set destination | Gizmo / world map | Preview only |
| 5 | Depart | Gizmo **출발** | **mass×dist×rate** |

**Call fee is the default cost:** no refund on no-board / disembark / empty leave. Trip fare only at depart.

---

## Silver payment (`TaxiPayment`)

| Context | Sources |
|---------|---------|
| Settlement **with** orbital trade beacon | Beacon radius + pawn-carried |
| Settlement **without** beacon | Stockpile/storage + carried |
| Field map | Carried only |
| **Caravan** call/depart | Open settlements’ **beacon** silver + caravan inventory (beacons first) |

---

## Caravan taxi UX

| State | UI | Movement | World icon |
|-------|-----|----------|------------|
| Idle | **택시 보내기** (fee → pick dest) | Free | Yellow caravan |
| En route | ETA + change dest | **Immobile** | **Taxi** |
| Waiting (boarding) | Dest / **출발** / **하차** | **Immobile** | **Taxi** |
| After 하차 or 출발 | — | Free | Normal / in-flight taxi |

---

## Map taxi UX (settlement / camp)

| Action | Notes |
|--------|--------|
| Choose landing | Cell + **Q/E** rotation at call |
| Wait | `waitTicks` (default 5h) after land |
| **착륙 위치 변경** | Gizmo while waiting; Q/E again |
| Dest / Depart / Dismiss | Standard wait gizmos |

---

## Implementation map

| Concern | Files |
|---------|--------|
| Call / pickup / dispatch | `TaxiCallService`, `TaxiPickupSite`, `TaxiPendingDispatch`, `TaxiGameComponent` |
| Landing Q/E + reposition | `TaxiLandingUtility`, `ShipJob_Wait_Gizmos_Patch` |
| Caravan send / board / 하차 | `TaxiCaravanUtility`, `TaxiCaravanBoarding`, `Caravan_Gizmos_Patch` |
| Caravan immobile | `Caravan_Movement_Patch` |
| Caravan taxi world icon | `Caravan_WorldIcon_Patch` |
| Fare / silver | `TaxiPayment`, `TaxiFareCalculator`, `TaxiTripBilling` |
| Arrival | `TaxiArrivalUtility`, MapLand / WorldDrop, arrival patch |
| Comms radio | `TaxiCommsContact`, `TaxiDialogMaker`, `Building_CommsConsole_Patch` |
| Flight icon | `TravelingRimTaxi` + `Textures/World/WorldObjects/.../RimTaxi.png` |
| Disembark icon | `Textures/UI/Commands/RimTaxiDisembark.png` |

---

## Defaults (settings)

| Key | Default | Meaning |
|-----|---------|---------|
| baseFare | **200** | Call fee (default cost model) |
| farePerKgPerTile | 0.1 | Trip rate |
| dispatchBaseTicks | 2500 | 1h |
| dispatchVarianceTicks | 5000 | +0–2h → **1–3h ETA** |
| dispatchTicksPerTripTile | 0 | Off |
| waitTicks | 12500 | 5h board/layover window |
| cooldownTicks | 2500 | 1h between calls |
| landOnSettlementMaps | true | Player map land vs always caravan |

---

## Deferred / next P0 gaps

- Full custom GUI panel  
- Hospitality  
- **Settlement enter/exit without losing taxi layover** (map enter still breaks caravan boarding)  
- Explicit cancel while en route (still no call-fee refund by default)  

---

## Recent fixes worth knowing

- Call fee prepaid default; no no-show refund.  
- Foreign settlement drop **on tile** (not adjacent).  
- Caravan send + immobilize + taxi world icon + 하차.  
- Map Q/E landing + reposition gizmo.  
- Shadow tracks angled skyfaller.  

---

## For the next person / AI

1. Read `AGENTS.md`.  
2. Prefer bugfixes over new systems.  
3. GUI later = wrap same 5 steps.  
4. Build + smoke checklist in `docs/BUG_CHECKLIST.md`.  
