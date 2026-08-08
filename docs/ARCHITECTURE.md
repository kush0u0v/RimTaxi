# Architecture

## High-level flow

```
Comms (ICommunicable "RimTaxi Dispatch" + DiaNode; optional gizmo)
  └─ ShowPickupSiteMenu
       ├─ maps / settlements / field / caravans / world pick
      ├─ map landing: cell (fixed rotation) → QueueDispatch
       └─ caravan pickup: QueueCaravanDispatch
            └─ tick → SpawnTaxi (map) or TaxiCaravanBoarding (caravan)

Player caravan top bar
  ├─ idle: no call gizmo (comms only)
  ├─ after comms call to caravan: pending en-route gizmos
  ├─ en route: ETA + change dest (CantMove; taxi world icon)
  └─ boarding: dest / 출발 / 하차 (CantMove; taxi world icon)
       └─ 출발 → LaunchCaravanAsTaxi → TravelingRimTaxi

Map TransportShip (Ship_RimTaxi)
  ├─ WaitTime gizmos: dest / landing(필요 시) / depart / dismiss
  └─ FlyAway (billing patch)

TravellingTransporters (TravelingRimTaxi) — taxi world texture
  └─ Arrival patch → TaxiArrivalUtility
       ├─ MapLand (player only) → DropRimTaxi → unload + wait layover
       └─ WorldDrop → caravan on tile + boarding layover
```

## Layers

| Layer | Responsibility |
|-------|----------------|
| UI entry | Comms radio; caravan gizmos; wait-job gizmos |
| Dispatch | `TaxiPendingDispatch` (+ landingRot for maps) |
| Caravan hold | Immobile + taxi world icon while pending/boarding |
| Billing | Call fee at call; trip fare at depart (`TaxiPayment` rules) |
| Booking | `CompRimTaxiTrip` + GC backup |
| Ship | TransportShip Wait / Unload / FlyAway |
| Arrival | MapLand / WorldDrop + force patch |

## Harmony patches

| Patch | Target | Intent |
|-------|--------|--------|
| `Building_CommsConsole_Patch` | `GetCommTargets`, `Thing.GetGizmos` | Radio contact + gizmo |
| `Caravan_Gizmos_Patch` | `Caravan.GetGizmos` | Send / en route / board / 하차 |
| `Caravan_Movement_Patch` | `CantMove`, `StartPath` | Immobilize for taxi |
| `Caravan_WorldIcon_Patch` | `ExpandingIcon`, `Material`, color | Taxi icon vs yellow circle |
| `ShipJob_Wait_Gizmos_Patch` | `GetJobGizmos` | Dest / set landing (open map) / depart / dismiss |
| `ShipJob_FlyAway_Billing_Patch` | `TryStart` | Fare / re-wait / empty leave |
| `TravellingTransporters_Arrival_Patch` | `Arrived` | Force our arrival |
| `TravellingTransporters_Speed_Patch` | speed getter | Slower flights |

## Key types (short)

| Type | Role |
|------|------|
| `TaxiLandingUtility` | Land checks, ghost, fixed rotation |
| `TaxiPayment` | Settlement beacon/stockpile/carried; caravan = beacons + inv |
| `TaxiCaravanUtility` | Launch, immobilize, taxi world icon textures |
| `TaxiCommsContact` / `TaxiDialogMaker` | Faction-style radio |

## Defs / textures

| Asset | Purpose |
|-------|---------|
| `Ship_RimTaxi` | TransportShipDef |
| `TravelingRimTaxi` | In-flight WO; taxi expanding icon |
| `RimTaxiShuttle` | Vehicle; `shipDef = Ship_RimTaxi` required |
| `Textures/World/WorldObjects(/Expanding)/RimTaxi.png` | Flight + caravan-hold icon |
| `Textures/UI/Commands/RimTaxiDisembark.png` | 하차 gizmo |

## Settings defaults

| Field | Default | Notes |
|-------|---------|--------|
| baseFare | **400** | Call fee (default prepaid cost) |
| farePerKgPerTile | 0.18 | Trip |
| dispatchBaseTicks | 2500 | 1h |
| dispatchVarianceTicks | 5000 | → **1–3h ETA** |
| waitTicks | 12500 | 5h board/layover |
| cooldownTicks | 2500 | 1h |
| landOnSettlementMaps | true | Player map land |
| travelSpeedFactor | 0.6 | Slower flight |
| maxLaunchDistance | 70 | |

## Build

`Source/RimTaxi/RimTaxi.csproj` → `Assemblies/RimTaxi.dll`  
`.\build.ps1` or `dotnet build -c Release`
