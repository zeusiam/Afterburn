# Afterburn — Bug Register
## Veratus Games · Project ALTERNATE · A7

> Confirmed defects only (broken behaviour). Deliberate deferrals go to `Afterburn_REVISIT.md`.
> Format: `BUG-NNN · severity (S1 blocker / S2 major / S3 minor / S4 cosmetic) · status`.
> On fix: move to the archive with the commit hash. Never delete entries.

## Open

| ID | Sev | Reported | Symptom / repro | Notes |
|---|---|---|---|---|
| BUG-013 | S4 | 2026-07-10 | First Play after import/shader-change flashes cyan while "Compiling shader" runs (editor Simulator only) | Editor async shader warm-up; not present in builds. Watch: if it persists after warm-up, escalate |
| BUG-014 | S3 | 2026-07-10 | Ships built from vendor prefabs (USC/StarSparrow) show their own static exhaust — boost state doesn't read on the ship (HUD chips only) | Known gap until WP-02 thruster tiers anchor to prefab tails |
| BUG-015 | S3 | 2026-07-10 | D15.1 gate lane + v2 reseed not yet verified by the headless suite (editor lock since S11) | Run full suite on next editor close; expected 76 tests |

## Fixed (archive)

| ID | Sev | Fixed | Symptom | Fix / commit |
|---|---|---|---|---|
| BUG-001 | S2 | 07-09 | U1 gate torus rendered as flat halo instead of vertical ring | Removed compounding rotation; adversarial review finding · in `ba1b034` |
| BUG-002 | S3 | 07-09 | HeavyWall slab lost the prototype's ~8.5° downward tilt | LookAt(mid.Pos) with true y · `ba1b034` |
| BUG-003 | S3 | 07-09 | Starfield omitted from greybox undocumented | StarfieldView, deterministic seed · `ba1b034` |
| BUG-004 | S2 | 07-10 | `cam` used before declaration in SceneBuilder (CS0841) | Flow wiring moved after camera creation · `2d3d6c7`-era |
| BUG-005 | S1 | 07-10 | ALL HUD text invisible — TMP default font not resolving; warning spam | UIFactory font-fallback chain + inactive-construction · Session 9 |
| BUG-006 | S2 | 07-10 | Touch wheel floated mid-screen (pivot anchored corner where centre intended) | Pivot 0.5 + aligned ability button · S9 |
| BUG-007 | S1 | 07-10 | FIRE/SHIELD touch zones dead — SafeArea inset desynced touch centre from visuals (~141 px) | Wheel centre read from actual screen rect · S9 |
| BUG-008 | S3 | 07-10 | ★ glyph missing from font atlas (□ in bounty tag) | Glyph removed · S9 |
| BUG-009 | S2 | 07-10 | Start warp gate swallowed the grid (station-scale bounds mis-fit); cyan flood at spawn | Extent clamp + 22 u forward park · `a807fef` |
| BUG-010 | S3 | 07-10 | Steer-zone tint read as permanent blue overlay | Idle 0.008 alpha, brightens on touch · `a807fef` |
| BUG-011 | S3 | 07-10 | 276 BoxCollider negative-scale warnings from Ebal mirrored modules | Pre-activation collider strip (`ViewPrefabs`) · `0848e90` |
| BUG-012 | S3 | 07-10 | `Stats_CountShots` failed after D14 (grid rams idle player, energy assumption broken) | Test lets the grid clear first · S9 |
