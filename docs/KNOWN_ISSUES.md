# Known issues & UX traps

## Booking / depart

- **Old taxis** spawned before `CompRimTaxiTrip` may show “no booked destination”. Fix: call a new taxi.
- Unloading all passengers does **not** clear booking (by design after fix). Empty auto-leave after wait clears booking.
- If trip fare cannot be paid at auto-depart, wait is extended (~3h) with a message.

## Arrival

- Drop is **world caravan**, often on a **neighbor tile** of a settlement — not auto map entry.
- Hostile tile caravans can still get world incidents after landing (vanilla).

## Economy

- Call fee is paid even if the player never boards (wait expires empty).
- Trip cost scales with mass × distance; large hauls can exceed call fee a lot.

## Graphics

- `drawSize` is larger than collision `size (5,3)` so the sprite fills the box; shadow is intentionally small.
- Replace `Textures/Things/Building/Misc/RimTaxiShuttle.png` and regenerate skyfaller shadow if art changes.

## Compatibility

- Not tested thoroughly with Odyssey orbital layers.
- Other shuttle/TransportShip mods may conflict.
- Prisoners-only loads may fail caravan owner checks on world drop.

## Save/load

- Prefer testing booking after load on a waiting taxi.
- In-flight `arrivalAction` is custom type in RimTaxi assembly — mod must stay enabled.

## Reporting bugs

Include: RimWorld version, Harmony, steps, Player.log lines with `[RimTaxi]`.
