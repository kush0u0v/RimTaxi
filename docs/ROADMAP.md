# Roadmap

## Done (current playable baseline — freeze feature creep)

Core loop (keep working; polish later via GUI, not new systems unless bugfix):

1. **Call** — 200 silver from comms map  
2. **Dispatch** — ETA delay (no world map at call)  
3. **Arrive** — land at chosen pickup (other settlements / field maps OK)  
4. **Set destination** — world map; fare preview = mass × distance  
5. **Depart** — pay trip fare, fly; map land or caravan drop-off  

Also in: remote pickup list, delayed dispatch, trip booking on Comp, map landing NRE fix, dual fare model.

## Deferred: dedicated GUI

Do **not** build a full custom control panel yet. Current gizmos/float menus/letters are enough.

Later GUI pass (when requested) should make the same steps intuitive:

- One taxi desk / panel: call fee, pickup, ETA, load, destination, fare meter, depart  
- Live fare estimate, clearer state (“en route / boarding / ready”)  
- Fewer nested float menus  

Until then: bugfixes and small UX text only.

## Next candidates (after baseline stable)

1. Empty auto-leave warning / optional call-fee refund  
2. Caravan-on-world-map pickup (no open map yet)  
3. Graphics polish  
4. **GUI overhaul** (above)

## Explicit non-goals

- Hospitality  
- Player-owned spaceship fleet / SRTS-style progression  
- Treating SRTS as identity reference in UI/docs  
- Odyssey required integration  
