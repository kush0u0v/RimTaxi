# Roadmap

## Done — playable baseline (feature freeze for systems)

Canonical loop:

1. **Call** — 200 silver (comms map); pick pickup site + landing cell; **no world map**  
2. **Dispatch** — ETA **1–3 hours**  
3. **Arrive** — land at pickup  
4. **Set destination** — world map; mass×distance **preview**  
5. **Depart** — charge trip fare; fly; map land or caravan  

Also delivered:

- Remote pickup (other player settlements + open field maps; no camp)
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

1. Call-fee refund / warning on empty no-show leave  
2. World **caravan** pickup without open map  
3. Trip fare always payable from home map (optional setting)  
4. Graphics polish  
5. **GUI overhaul**

## Explicit non-goals

- Hospitality  
- Player ship fleet / SRTS-style progression or branding  
- Odyssey hard dependency  
- Redesigning the 5-step economy without a clear request  
