# RimTaxi bug checklist (1.6)

**How to use:** run each row in a dev/test colony. Mark Pass / Fail. On fail, note steps + `Player.log` lines with `[RimTaxi]`.

**Prep:** Harmony + RimTaxi loaded. Mod options → **Reset to defaults** once. Comms powered. ~1000+ silver available under payment rules.

---

## A. Smoke — happy path (must pass)

| # | Test | Expected | ☐ |
|---|------|----------|---|
| A1 | Comms use → contact list has **RimTaxi 배차센터** | Same style as faction radio | |
| A2 | Radio → **택시 요청** → pickup menu | Header + world pick + sites | |
| A3 | Pickup **here** → landing cell **(Q/E rotate)** → pay **200** | Letter + ETA **1–3h** (not instant) | |
| A4 | After ETA, taxi lands | No NRE; message/letter | |
| A5 | Load pawn → **4. 목적지** → **5. 출발** | Fare charged; flies | |
| A6 | Arrive player base | Map land; unload; **waits** (not instant leave) | |
| A6b | After flight to own map: pick landing + Q/E | Lands where chosen, then waits | |
| A7 | Reboard → new dest → depart | Second leg works | |

---

## B. Comms / call UI

| # | Test | Expected | ☐ |
|---|------|----------|---|
| B1 | Console gizmo opens radio (or call flow) | Usable shortcut | |
| B2 | Power off / solar flare | Call blocked with reason | |
| B3 | Not enough silver | Blocked; silver not partial-spent | |
| B4 | Call twice quickly | Cooldown **1h** after call | |
| B5 | Second call while first en route to same map | Blocked “already en route” | |
| B6 | **월드맵에서 픽업 위치 선택…** | Can pick caravan / other colony / open map with colonists | |
| B7 | Invalid world click | Reject message, no fee | |

---

## C. Remote pickup

| # | Test | Expected | ☐ |
|---|------|----------|---|
| C1 | 2nd player settlement (closed) | Listed; map opens; land cell; fee from **comms** map | |
| C2 | Field map with free colonists (no camp) | Listed as field pickup; taxi goes there | |
| C3 | Field map empty (no colonists) | Not listed (or not valid) | |
| C4 | Player caravan in list / world pick | Dispatch to caravan; fee from comms map | |

---

## D. Caravan taxi

| # | Test | Expected | ☐ |
|---|------|----------|---|
| D0 | **택시 보내기** → pick dest | Call fee; ETA; caravan **immobile**; **taxi world icon** | |
| D0b | En route: try move caravan | Blocked + message | |
| D0c | Arrive ready → **하차** | Taxi dismissed; can move; yellow circle returns | |
| D0d | Arrive ready → **출발** | Flight; trip fare | |
|---|------|----------|---|
| D1 | Select caravan → **택시 호출** | Fee from caravan inv; ETA letter | |
| D2 | After ETA | Set dest + Depart on caravan bar | |
| D3 | Depart | Whole caravan boards; world flight | |
| D4 | No silver on caravan | Call/depart disabled or reject | |

---

## E. Destination / boarding / wait

| # | Test | Expected | ☐ |
|---|------|----------|---|
| E1 | Depart with no destination | Disabled / message | |
| E2 | Depart empty | Disabled | |
| E3 | Set dest → unload all → still booked? | Booking kept on shuttle (inspect string) | |
| E4 | Wait expires empty | Taxi leaves free | |
| E5 | Wait expires loaded, no dest | Re-wait + message (no blind fly) | |
| E6 | Wait expires loaded + dest, enough silver | Auto charge + fly | |
| E7 | Auto-depart, not enough silver | Re-wait + need silver message | |
| E8 | Dismiss gizmo | Unload + leave | |
| E9 | Max range (default 70) | Beyond range rejected | |
| E10 | Same tile dest | Rejected | |

---

## F. Arrival

| # | Test | Expected | ☐ |
|---|------|----------|---|
| F1 | Dest = own colony | **Map land**, unload, layover wait | |
| F2 | Dest = foreign settlement | **No map open / no combat**; caravan **on settlement tile** (not neighbor) | |
| F3 | After foreign drop | Caravan has Set dest / Depart (layover) | |
| F4 | Enter foreign base then reform caravan | Boarding may end; can call taxi again | |
| F5 | Dest = empty world tile | Caravan drop OK | |
| F6 | `landOnSettlementMaps` OFF | Always caravan (even player base) | |

---

## G. Silver payment rules

| # | Test | Expected | ☐ |
|---|------|----------|---|
| G1 | Settlement **with** orbital trade beacon | Pays from beacon-radius silver + carried | |
| G2 | Settlement **without** beacon | Pays from **stockpile/storage** + carried | |
| G3 | Silver only outside beacon (beacon exists) | Call fails if not enough in radius+carried | |
| G4 | Field map depart | Only **carried** silver counts (not random piles) | |
| G5 | Caravan taxi: silver only at home (beacon) | Pays from settlement beacon; caravan inv optional | |
| G5b | Caravan taxi: silver only on caravan | Pays from caravan inv | |
| G5c | Caravan: combine beacon + caravan silver | Can split across both pools | |
| G6 | Fare math | ~`ceil(massKg × tiles × 0.1)` | |

---

## H. Graphics / world marker

| # | Test | Expected | ☐ |
|---|------|----------|---|
| H1 | Landing approach | Craft from angle; **shadow under craft** | |
| H2 | In-flight world map | Marker is **taxi image** | |
| H3 | Caravan taxi waiting | **Taxi icon** (not yellow circle) until 하차/출발 | |
| H4 | Landed shuttle size | Covers 5×3 footprint reasonably | |

---

## I. Save / load / multi-map

| # | Test | Expected | ☐ |
|---|------|----------|---|
| I1 | Save while taxi **en route** (dispatch) | Load → still arrives | |
| I2 | Save while **waiting** boarded with dest | Dest kept; depart OK | |
| I3 | Save while **in flight** (TravelingRimTaxi) | Load → arrives correctly (map or caravan) | |
| I4 | Save during caravan layover boarding | Load → gizmos still work | |
| I5 | Disable mod mid-flight then re-enable | May lose custom arrival — note behavior | |

---

## J. Edge cases / known traps

| # | Test | Expected / known | ☐ |
|---|------|------------------|---|
| J1 | Call fee paid, never board | Fee kept (no-show cost) | |
| J2 | Prisoners only in taxi → world drop | May fail caravan form — log | |
| J3 | Odyssey orbit tiles | No hard crash; note odd pathing | |
| J4 | Other shuttle mods | Note conflicts | |
| J5 | Dev quick-test: set dispatch wait low | Still no dest-at-call regression | |

---

## Log filter

```
[RimTaxi]
```

Useful lines: dispatch queued, MapLand OK, WorldDrop, CompRimTaxiTrip booked, Arrival via …

---

## Minimal 10-minute suite

If short on time, only run: **A1–A7, F2, G1–G2, H2, I3**.
