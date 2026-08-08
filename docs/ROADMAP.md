# Roadmap

## Done — playable baseline (feature freeze for systems)

Canonical loop:

1. **Call** — 400 silver default (comms map); pick pickup site + landing cell; **no world map**  
2. **Dispatch** — ETA **1–3 hours**  
3. **Arrive** — land at pickup  
4. **Set destination** — world map; mass×distance **preview**  
5. **Depart** — charge trip fare; fly; map land or caravan  

Also delivered:

- Remote pickup (other player settlements + open field maps; no camp)
- **World caravan** pickup via **comms**; set dest / board / depart on caravan top bar (no idle call)
- Dual billing (call vs trip)
- Trip booking on vehicle Comp
- Map landing without vanilla DropShuttle NRE
- Slower world flight
- Docs for AI handoff (`AGENTS.md`, `docs/HANDOFF.md`, …)

## Deferred — dedicated GUI

Do **not** build a full taxi control panel unless the user asks.

Later GUI should re-skin the **same five steps**:

- Single panel: fee, pickup, ETA, load, destination, meter, depart  
- Clear states: en route / boarding / dest set / ready  
- Fewer nested menus  

Until then: **bugfixes and small string/UX only**.

## Next candidates (after baseline)

1. Explicit **cancel** UX while en route (still **no call-fee refund by default**)  
2. Settlement enter/exit without losing taxi layover session  
3. Trip fare always payable from home map (optional setting)  
4. **GUI overhaul**  

**Done recently:** caravan taxi world icon while waiting; map landing pre-depart selection (fixed rotation) and fallback handling.

**Locked economy:** call fee (`baseFare` default 400) is prepaid default; no-show/disembark does not refund call fee unless a future optional setting is requested.

## Explicit non-goals

- Hospitality  
- Player ship fleet / SRTS-style progression or branding  
- Odyssey hard dependency  
- Redesigning the 5-step economy without a clear request  
