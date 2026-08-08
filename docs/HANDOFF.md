# Handoff summary (current baseline)

**Date context:** 2026-08 — playable baseline; full GUI deferred.  
**GitHub:** https://github.com/kush0u0v/RimTaxi  
**packageId:** `kush.rimtaxi`

---

## What the mod is

Civilian **call taxi** for RimWorld 1.6:

- Dispatch from **comms console only** (radio **RimTaxi 배차센터** → request taxi → **pick where to send in the radio GUI**, including player caravans). Caravan top bar has no idle call/send.
- Taxi arrives after **1–3 hours** (not instant).
- Board at **pickup** (maps, field maps, caravan — no camp required).
- **Landing cell + no rotation**: (1) at **call**, (2) on departure before leaving when destination map is open.
- Caravan + taxi (en route or waiting): **world icon = taxi** (not yellow circle) until **하차** or **출발**.
- Destination after taxi ready → **mass × distance** trip fare → fly.
- Trip end: **player maps** land + layover wait; **foreign settlements** world caravan **on tile** (no combat entry) + layover.

Identity: **service / meter**, not player spaceship (not SRTS).

---

## Canonical 5 steps

| # | Step | Player action | Silver |
|---|------|---------------|--------|
| 1 | Call | **Comms only** (map or remote caravan pickup) | **Call fee (default 400)** prepaid |
| 2 | Dispatch | Wait ETA | — |
| 3 | Arrive | Map land or caravan taxi ready | — |
| 4 | Set destination | Gizmo / world map | Preview only |
| 5 | Depart | Gizmo **출발** | **mass×dist×rate** |

**Call fee:** paid **once per call** only (default 400). Not again on depart/layover/next leg. No refund on no-board/disembark.  
**Trip fare:** only at **Depart** (once per leg; no double-charge after manual depart fix).

---

## Silver payment (`TaxiPayment`)

| Context | Sources |
|---------|---------|
| Settlement **with** orbital trade beacon | Beacon radius + pawn-carried |
| Settlement **without** beacon | Stockpile/storage + carried |
| Field map | Carried only |
| **Caravan** trip fare (depart) | Open settlements’ **beacon** silver + caravan inventory (beacons first). Call fee still from **comms map**. |

---

## Caravan taxi UX

| State | UI | Movement | World icon |
|-------|-----|----------|------------|
| Idle | (no taxi gizmo — call from **comms**) | Free | Yellow caravan |
| En route | ETA + change dest | **Immobile** | **Taxi** |
| Waiting (boarding) | Dest / **출발** / **하차** | **Immobile** | **Taxi** |
| After 하차 or 출발 | — | Free | Normal / in-flight taxi |

---

## Map taxi UX (settlement / camp)

| Action | Notes |
|--------|--------|
| Choose landing | Cell at call |
| Wait | `waitTicks` (default 5h) after land |
| **착륙 위치 변경** | Open destination map에서 출발 전 착륙 칸 재설정 |
| Dest / Depart / Dismiss | Standard wait gizmos |

---

## Implementation map

| Concern | Files |
|---------|--------|
| Call / pickup / dispatch | `TaxiCallService`, `TaxiPickupSite`, `TaxiPendingDispatch`, `TaxiGameComponent` |
| Landing selection | `TaxiLandingUtility`, `ShipJob_Wait_Gizmos_Patch` |
| Caravan board / 하차 (after comms) | `TaxiCaravanUtility`, `TaxiCaravanBoarding`, `Caravan_Gizmos_Patch` |
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
| baseFare | **400** | Call fee (default cost model) |
| farePerKgPerTile | 0.18 | Trip rate |
| dispatchBaseTicks | 2500 | 1h |
| dispatchVarianceTicks | 5000 | +0–2h → **1–3h ETA** |
| dispatchTicksPerTripTile | 0 | Off |
| waitTicks | **5000 (2h)** | Board/layover after land (incl. after a ride) |
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
- Comms→caravan pickup + immobilize + taxi world icon + 하차. No caravan idle 「택시 보내기」.  
- Map 착륙 칸 미리 선택(출발 전), 도착 시 블록되면 자동 랜덤 대체.  
- Shadow tracks angled skyfaller.  

---

## For the next person / AI

1. Read `AGENTS.md`.  
2. Prefer bugfixes over new systems.  
3. GUI later = wrap same 5 steps.  
4. Build + smoke checklist in `docs/BUG_CHECKLIST.md`.  
