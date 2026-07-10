# Afterburn — Tracking System Index
## Veratus Games · Project ALTERNATE · letter **A**, slot 7

> **The rule: every loose thread lives in exactly one of these files.** If it's not written
> here, it will be forgotten — write it down the moment it appears, not at session end.

| File | What goes in it | When to update |
|---|---|---|
| `Afterburn_BUGS.md` | Confirmed defects — open + fixed archive, with repro and severity | The moment a bug is found; move to Fixed with the commit hash |
| `Afterburn_REVISIT.md` | Parked decisions, deferred work, "flagged for later", tuning verdicts pending | The moment anything is deferred; check when its trigger phase starts |
| `Afterburn_STATE.md` | The living snapshot: what exists, what's wired, verification status | End of every working session |
| `Afterburn_SWEEPLOG.md` | Code sweeps/audits/reviews already performed — scope + findings | After every sweep; CHECK BEFORE starting one (no repeats) |
| `Afterburn_CHANGELOG.md` | Session-by-session change narrative (the code change log) | End of every working session |
| `Afterburn_MILESTONES.md` | Phase gates (U1–U7) + kill-gate status + session log | When a gate's status changes |
| `Afterburn_DesignReview.md` | Locked decisions D1–D15.x, the economy, Game Center spec | When Seni locks a decision |
| `Afterburn_AssetBacklog.md` | Visual/audio work packages (WP-01…12) for design sessions | When a WP ships or changes status |
| `Afterburn_BUILD.md` | The build contract (§2 law, frozen values, module specs) | Amendment blockquotes only — never rewrite history |
| `Afterburn_PortSpec.md` | Prototype extraction + divergences | Frozen — new divergences append to the list |

**Bug vs Revisit:** a BUG is broken behaviour (it does the wrong thing). A REVISIT is a
decision or improvement deliberately postponed (it does the intended thing, for now).

**Session-end checklist** (also in root `CLAUDE.md`):
1. Tests green (or the gap logged in STATE) → 2. CHANGELOG entry → 3. STATE refreshed →
4. New bugs/deferrals filed → 5. commit + push → 6. backup zip at milestone points
(vendor packs excluded from zips — R-17).
