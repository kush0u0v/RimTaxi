# Contributing

## For humans

1. Fork / branch from `main`.
2. Keep changes scoped to one feature or fix.
3. Build: `dotnet build -c Release` in `Source/RimTaxi`.
4. Smoke-test in RimWorld 1.6 with Harmony.
5. Open a PR with: summary, test steps, known risks.

## For AI agents

1. Read **`AGENTS.md`** first (mandatory).
2. Skim `docs/ARCHITECTURE.md` and `docs/KNOWN_ISSUES.md`.
3. Prefer smallest diff that fixes the issue.
4. Do not add Hospitality or Odyssey hard deps.
5. Report: changed files, build result, in-game test steps, remaining limits.

## Commit messages

Use clear present tense, e.g.:

- `Fix trip booking lost on load/unload`
- `Charge trip fare from mass and distance at depart`
- `Force world caravan drop on settlement tiles`

## Code review focus

- Arrival still forced to world caravan?
- Call fee vs trip fee still split correctly?
- Harmony patches null-safe and def-filtered to RimTaxi only?
