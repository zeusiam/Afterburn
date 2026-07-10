# Afterburn — Revisit List
## Veratus Games · Project ALTERNATE · A7

> Everything deliberately parked, deferred, or awaiting a verdict — consolidated from every
> doc and session so nothing evaporates. Each item: **trigger** (when to pick it up) and
> **source** (where it was decided). Resolve = strike through with date + outcome, never delete.

## Design & tuning verdicts (Seni's calls)

| ID | Item | Trigger | Source |
|---|---|---|---|
| R-01 | **Ghost skill-band re-tune** — rulings #4/#5 fixed both prototype bugs (ghosts fair now), AND ghosts are exempt from the gate lane → once the player masters gates, races may skew easy. Band 0.80–0.90 is the lever | After gate-lane tuning settles / U4 gate | DesignReview §5, D15 |
| R-02 | **Formal confirmation of prototype bug rulings** #1 (countdown), #3 (wall reset), #4, #5, #7 (siphon cap), #16 (cone) — implemented as recommended, need Seni's explicit nod | Next feel pass | DesignReview §5 |
| R-03 | **Synthetic ghosts vs the gate lane** — v1 exemption (rail-locked). Options: apply speed gates when their rail crosses a field; or band compensation (R-01) | With R-01 | D15 |
| R-04 | **Perfect-launch countdown bonus** — designed as energy grant, but the pool starts FULL (no-op). Redesign: speed impulse? barrier charge? | U5 tuning pass | DesignReview U2 row |
| R-05 | **D14 contact tuning verdict** — wall 6/s, ship base 10, mass-scaled: spicier or softer after clean-race playtesting? | Seni play verdict | S8 |
| R-06 | **Gate combo chains + mutually exclusive gate routes** — proposed differentiators, undecided | Seni verdict | S12 reply |
| R-07 | **Battle Race spec** (D9 update 1): kill scoring, in-match pickups, ghost lateral-dodge (mines prerequisite), live-contact physicality question | Post-launch u1 planning | D9, D14 note |
| R-08 | **Bounty hysteresis + heat** (+2/10 s held, cap ≤ fireCost) — designed, not implemented | U4 polish | DesignReview U4 matrix |
| R-09 | **Slipstream draft** (speed-only, ×1.08–1.12 in wake) — designed, not implemented | U3/U4 polish | DesignReview U3 matrix |
| R-10 | **Aim assist ≤5° + full telegraph/counterplay pass** — designed; EMP windup etc. land with WP-07 VFX | WP-07 | DesignReview U3 matrix |
| R-26 | **D10 module slots** (engine/wing/hardpoint sidegrades, teaching challenges) — locked design, unbuilt; USC modular parts are the visual supply | Post-launch per matrix (or Seni pulls forward) | D10, §6.3 |

## Tech debt & engineering

| ID | Item | Trigger | Source |
|---|---|---|---|
| R-11 | **Mobile texture pass** — all Ebal packs ship 4K; downres 1–2K ASTC, share sets, LODs. The ≤48 MB device budget is broken until this runs | Before ANY device build (U7) | D13 |
| R-12 | **GreyboxMaterials `Shader.Find` → serialized .mat assets** (device shader stripping = magenta) | Before U7 device builds | GreyboxMaterials header |
| R-13 | **TMP Essentials proper import** (Window → TextMeshPro → Import Essential Resources) — fallback chain carries us meanwhile | Any time, 1 click (Seni) | BUG-005 |
| R-14 | **Track-space combat conversion** — world-space is prototype-parity on flat; convert when 3D arenas land or the combat economy silently re-tunes on slopes | First elevation arena (Arena02 tech) | DesignReview U3 matrix |
| R-15 | **StarSparrow retirement** — superseded by USC; 91 MB on disk, gitignored. Keep as fallback or delete? | After USC look confirmed | S10 |
| R-16 | **Generated asset pack reconciliation** — palette drift (Night Base `#0A0E1F` vs void `#05070F`); "drachma" icon → Salvage rename | On icon/audio wiring | S10 assessment |
| R-17 | **Backup zips: exclude vendor packs** going forward (~1.7 GB otherwise); one vendor snapshot zip is enough | Next backup | S10 |
| R-18 | **Parity trace fields** — D14/D15 tunables are zeroed in the trace test (divergence-by-ruling). Any NEW ShipFeel/gate field must be added to that zero-list or the trace breaks | Every new Core tunable | ParityTraceTests |
| R-27 | **Recorded-ghost roster integration** — GhostReplayer exists + proven; not yet spawnable in the race roster; no ghost persistence/store | U4 polish / leaderboards | GhostRecording |
| R-28 | **Ghost color identity on prefab ships** — roster tints only affect greybox cones; USC ghosts of one class look identical. Foundation = livery param block on Colorize | With livery system (U6) or WP pass | S10 note |

## Queued work (accepted, ordered)

| ID | Item | Trigger | Source |
|---|---|---|---|
| R-19 | **Icons + audio wiring** from `assets/afterburn_assets` (43 icons, 33 WAVs) | Next build slot (Seni picks vs R-20) | S10 |
| R-20 | **WP-01 TrackRibbon shader** (the neon road) | Next art slot | AssetBacklog |
| R-21 | **WP-06 speed-feel package** (FOV kick, lines, shake) — U2 signed off, toggle may enable | After WP-01/02 | AssetBacklog |
| R-22 | **U6: Salvage economy scaffolding + Unity Purchasing + store-catalog lint acceptance tests** | Seni's go | DesignReview §6 |
| R-23 | **Game Center scaffolding** (D8: counters U5, boards live U6, Play Games U7) | With U6 | D8 |
| R-24 | **Minimap elevation tint** | Arenas 02/03 | UIEnvSpec |
| R-25 | **Lineup/podium render ghost-header cosmetics** (billboard completion) | With livery system | UIEnvSpec 18b |
| R-29 | **Galactic Leopard purchase** (capital-ship backdrop) | Anchor Station work begins | S10 |
