# Afterburn — Changelog
## Veratus Games · Project ALTERNATE · letter **A**, slot 7

**Git repo:** `https://github.com/zeusiam/Afterburn`
**Manual backup location:** `/Volumes/Veratus Group/manual Backups/Afterburn`

**Format:** newest first. Every entry = date · type · what changed · why.
**Types:** `DESIGN` · `PROTOTYPE` · `UNITY` · `TUNING` · `SCOPE` · `FIX` · `SWEEP`

---

## 2026-07-10 — Session 9 (U5 front-end + HUD + touch · StarSparrow gate 0)

| Type | Change | Rationale |
|---|---|---|
| `UNITY` | **U5 built:** SIGNAL VOID token system (`AfterburnPalette/Type/Motion`) · `UIFactory` runtime component library (canvas 2796×1290, SafeArea roots, robust font-fallback chain) · **Race HUD** (frame-true hero energy bar ∝ maxEnergy, no-tween law + loss-ghost trail, mode colors, ticks per 20, state chips, P/lap/tabular timer, km/h ×3, BOUNTY tag, trailer-only Intel bar, baked-outline minimap + leader ring, toasts 1.1 s, countdown) · **touch layout** (steer zone + energy-action wheel with input-layer exclusion PROVEN by exhaustive-sweep test, slide-to-switch, ability button) · **flow** (Grid hold → lineup sweep → countdown → racing → podium orbit → Summary with the three hooks + Rematch-same-seed) · Boot/MainMenu-loadout/Settings screens · lap-time capture | UIEnvSpec §4 executed; the game is playable end-to-end: Boot → loadout → race → summary → one more run |
| `UNITY` | **D13 gate 0 running:** Ebal **StarSparrow** (free kit) imported and validated — ShaderGraph materials = URP-native (zero conversion), shared 2048² texture set ≈ 8 MB for ALL ships, single-renderer prefabs = 1 draw call/ship. `HullDefinition.shipPrefab` + auto-fit (5 u sim length, +z) + `shipVisualScale` presence knob (default 1.4); assign menu `Veratus/Afterburn/Setup/Assign StarSparrow Hull Visuals` (idempotent) | Renders perfectly in-game. **Key finding: the Colorize shader (shared textures + per-material color params) IS the livery architecture the economy spec designed.** Hi-Rez €87 purchase decision awaits Seni |
| `FIX` | Wheel touch-classification now reads the wheel's ACTUAL screen rect (SafeArea notch inset had desynced touch from visuals — boost worked by luck, fire/shield fell in dead sectors) · TMP labels construct inactive with an explicit font (kills warning spam; dynamic-font last resort) · `Stats_CountShots` updated for D14 (the passing grid rams an idle player — the test now lets the grid clear) | Playtest findings from Seni's sessions, all root-caused |
| `SWEEP` | **69/69 edit-mode tests, zero compile errors** (headless, editor closed) | Full-stack verification: U2 parity + U3 combat + U4 ghosts/replay + U5 wheel/director |

**⏭ Next:** Seni's calls — Hi-Rez buy/pass (gate 0 look test) · contact-damage tuning verdict (6/s wall, 10 base ship) · then U6 (Salvage/IAP scaffolding + Game Center counters) or the U3-art pass (TrackRibbon shader, SDF gates) per preference.

---

## 2026-07-09 — Session 8 (U2 feel sign-off + D14 tangible ships)

| Type | Change | Rationale |
|---|---|---|
| `DESIGN` | **U2 FEEL GATE PASSED — Seni: "feels like a match"** (side-by-side vs the HTML at frozen tuning) | The kill-gate re-validation the whole port aimed at |
| `DESIGN` | **D14 locked (owner ruling):** ships are TANGIBLE — wall grind drains 6 energy/s (atop the ×0.92 scrape); ship↔ship contact = mutual **mass-ratio** damage (base 10), player pushout + scrape, 0.5 s pair cooldown, Phase immune. `hull.mass` gains its first gameplay consumer. Ghost holograms reverted to solid (tangible things look solid); `LitTransparent` reserved for Sora's Phase | Overrides the intangible-ghost recommendation after the feel pass; §2-safe (contact only drains). Parity trace zeroes the D14 fields — divergence-by-ruling |
| `UNITY` | `ShipContactSystem` (Core) + wall-damage in `ShipController.ResolveWalls` + 6 contact tests; new GameTuning block (`wallContactDamagePerSec`, `shipContactDamage`, `shipContactCooldown`, `shipContactSpeedMult`) | All tunable; zero = prototype-parity mode |
| `SCOPE` | Balance watch flagged: mass is now a live sidegrade axis (±10% accounting); rail-locked ghosts never wall-grind — skill band compensation review at the U4 re-tune | Keep the sidegrade doctrine honest |

**⏭ Next:** Seni closes Unity or runs the in-editor test suite (project lock blocks batchmode); then **U5: HUD + loadout + lineup/podium + touch** per UIEnvSpec.

---

## 2026-07-09 — Session 7 (headless build: U2 → U4 game logic)

| Type | Change | Rationale |
|---|---|---|
| `UNITY` | **U1 signed off by Seni** (Race.unity verified in Simulator); headless directive issued: build game logic through U4, UI after | Owner's call — gates verified by automated evidence, feel sign-off at the playable build |
| `UNITY` | **U2 built:** `EnergyCore` (single spend authority), `TrackSystem` (frames + windowed progress-coherent nearest + shortcut gating + per-race wall state), `ShipController` (prototype updatePlayer step-for-step, **fixed 60 Hz tick, track-frame math per D6** — flat = degenerate case), `RaceRunner` (accumulator + interpolation, prototype keyboard) | The kill-gate loop, 3D-ready from day one |
| `SWEEP` | **Parity instrument built and passing:** Node harness executes the ACTUAL prototype JS (verbatim slices, r128 shim, double precision) → 3600-frame reference trace; C# replays the schedule. **Proven: frame-0 + pre-contact arithmetic exact (<0.02u); wall containment all 60 s; energy economy ≤0.55 whole-run; fire 3/3, boost ticks 221/221, shield 76/76 exact.** Post-contact positional lockstep documented as chaos-bound (measured, report-only) | "Feels like the prototype" is now falsifiable; cross-validated — both sims independently produce the same cruise peak (58.19) and grind equilibrium (~9.36) |
| `UNITY` | **U3 built:** `IRacer` contract, `CombatSystem` (prototype-exact 40-bullet pool, shield ×0.40, bounty 8/16, **siphon capped at victim's pool — ruling #7**), `PilotAbilitySystem` (EMP/Phase/Siphon/Decoy, cooldown-gated, zero energy), Heavy wall break + **per-race reset (ruling #3)**, CombatView greybox | Combat economy exactly as frozen, with the two combat rulings applied |
| `UNITY` | **U4 built:** `GhostRacer` (rail-locked curve follower, **rulings #4 single-skill + #5 laps=−1**, seeded RNG), `RaceDirector` (Countdown→Racing→Finished, prototype grid, ±0.5 lap wrap + anti-cheese, every-tick bounty leader, standings, stats), **`GhostRecording` format** (fixed-tick input stream + cosmetic header + wall-break tick; **bitwise replay determinism proven incl. through serialization**), full roster in Race.unity + dev overlay | The race is complete: 3 tinted ghosts, combat, bounty, finish — playable in greybox |
| `SWEEP` | **59/59 edit-mode tests green**, zero compile errors across all batchmode runs. Notable: same-seed race determinism; boost dry-tank self-cancel; wall-grind equilibrium cross-validated against the prototype's own measured behaviour | U2–U4 acceptance evidence |

**⏭ Next:** Seni's feel pass on the playable greybox (side-by-side vs the HTML at frozen tuning — the formal U2 criterion), skill-band re-tune check (U4 gate item), then **U5: HUD + front-end + touch** per UIEnvSpec.

---

## 2026-07-09 — Session 6 (design pass 3 — battle-racer identity + audited economy)

| Type | Change | Rationale |
|---|---|---|
| `DESIGN` | **D9–D13 locked.** Identity = "3D space battle-racer"; mode roadmap RACE (launch) → BATTLE RACE (update 1, kill scoring + world-spawn pickups, never ranked/never sold) → BATTLE MODE LMS (update 2); **module slots** as earn-only sidegrades with teaching challenges; **XP unlocks content, never stats**; D13 provisional: hulls from the Ebal Hi-Rez PBR modular kit (free-sample device validation before purchase), world/UI stay code-built | Seni's original vision reconciled with the validated loop — the one self-defeating element (upgrades as raw power) re-engineered into visible ship-building |
| `DESIGN` | **Economy audited and rewritten** (DesignReview §6): 3 adversarial auditors (P2W leak-hunter / economy pacing / revenue realist) found 2 catalog breaks — **season-pass pilot timing** (was a 3–4-week paid ranked head start; now same tier both tracks) and the **week-2 spender wall** (fixed via collections $12.99–14.99, supporter pack $9.99, announcer packs) — plus leaks now closed: reactive-livery gutter owner-only, slot housings with legality contract, bounty-hit Salvage capped 3/race, Cold Blood requires pre-final-lap dry-out, hull-equal style bonuses, pilots fixed at 3000 Salvage, pass never expires, FTUE colorway token | "Nothing you can buy makes you faster" survives line-item audit; sellable shelf now feeds the $30 fan past week 2 |
| `UI` | **Mandatory cosmetic billboard:** pre-race Lineup + post-race Podium screens added to UIEnvSpec (deliverable 18b); in-race HUD themes rejected permanently; frame-true contract extended to all paid VFX | Cosmetics' audience by construction — the async-ghost status economy depends on it |

**⏭ Next:** U1 gate sign-off still open · validate the Hi-Rez free sample on device (D13 gate 0) · then U2 under the D6 directive.

---

## 2026-07-09 — Session 5 (design pass 2)

| Type | Change | Rationale |
|---|---|---|
| `DESIGN` | **Design review & assessment** against Seni's expanded vision (full 3D world, combat strategy, racing excitement, challenge, visual impressiveness) — 4-lens design panel (track/combat/monetisation/art) + synthesis → **`Afterburn_DesignReview.md`** | The validated energy loop stands; the gaps are the flat track, thin feel layer, no challenge ladder, and no owned art direction |
| `DESIGN` | **New locked decisions D5–D8:** D5 launch = 1 arena + 2 full-3D fast-follow · D6 **U2 physics is 3D-track-frame from day one** (flat = degenerate case, parity trace-diff mandatory) · D7 **no ads, ever** · D8 Game Center first (Nibwell GameKit pattern), Play Games at U7 | Seni's calls 2026-07-09; BUILD.md §1/§7.2/§7.6/§10 amended to match |
| `DESIGN` | **IAP catalog defined** (direct-priced cosmetics: liveries incl. energy-reactive tier, boost-visible trails, nameplates, founder pack, pilots; no hard currency/gacha/timers/run-gating) + **ghost records must carry cosmetic loadout** — the status billboard | Cosmetics need an audience in an async-ghost game; competitive integrity guardrails untouched |
| `DESIGN` | **Game Center spec:** per-arena race/lap boards (classic + weekly), career boards, ~20 mechanics-tied achievements | Studio GameKit precedent (Nibwell) makes this low-risk |
| `UNITY` | **`Afterburn_UIEnvSpec.md` written** — the requirements spec for the Claude design/build agent: emissive-first art direction, environment modular kit, 3 arena themes, VFX list, full UI system (landscape 2796×1290), URP mobile budgets, phased deliverables with acceptance criteria | Purpose-built for a code-only builder: procedural meshes, shaders, SDF, uGUI — no hand-painted assets required |
| `SCOPE` | Prototype bug rulings recommended (fix ghost free-lap #5, ghost skill² #4, broken-wall-for-all #3, Siphon minting #7; keep straightened cone #16; add countdown) — **await Seni confirmation at U2 gate** | PortSpec divergences need explicit rulings before the parity build |

**⏭ Next:** U1 gate sign-off still open (open `Race.unity`, press Play). Then U2 with the D6 3D-ready directive.

---

## 2026-07-09 — Session 4

| Type | Change | Rationale |
|---|---|---|
| `DESIGN` | **All BUILD §12 open inputs resolved by Seni:** repo `https://github.com/zeusiam/Afterburn` · backup `/Volumes/Veratus Group/manual Backups/Afterburn` · Unity **6000.4.4f1** · **LANDSCAPE** orientation · ghosts = synthetic day-one + recorded over time | Unblocks U1; nothing gating the Unity build |
| `UNITY` | Unity version chosen to match **all four sibling Veratus projects** (Rune Rouge, Eclipse, Nibwell, Triagles — all 6000.4.4f1, installed) | Studio-convention alignment; shared editor knowledge across the portfolio |
| `DESIGN` | **Orientation: LANDSCAPE** — deliberate deviation from the portrait studio convention; Canvas reference becomes 2796×1290 | Seni's call for the chase-cam racer: classic framing, wider view of the track. Core unaffected |
| `UNITY` | **U1 started:** git repo initialised, UnityProject scaffolded (6000.4.4f1 + URP), assembly split + folders per BUILD §4 | Milestone-gated build begins; stop at U1 gate for sign-off |
| `UNITY` | **U1 built:** 4 asmdefs (`Afterburn.Core/View/UI/Editor` + EditMode tests, nullable on), SO data model (`HullDefinition`/`PilotDefinition`/`GameTuning`+`ShipFeel`/`TrackDefinition`), idempotent seeder `Veratus/Afterburn/Create or Update SOs`, studio `ObjectPool`+`SafeArea` copied per convention, PrimeTween 1.4.8 + TMP Essentials, `Afterburn.inputactions` (prototype keys 1:1), `CatmullRomSpline` (three.js r128-parity), greybox `TrackView`/`ShipGreybox`/`StarfieldView`, `ChaseCamera`, Boot/MainMenu/Race scenes | Everything in BUILD §9's U1 rows, module boundaries per §3 |
| `PROTOTYPE` | **Wrote `Afterburn_PortSpec.md`** — line-by-line extraction of the prototype (track gen, camera, physics order, ghost AI, combat, dressing) + **16 documented divergences** between prototype code and BUILD doc (ghost free-lap bug, broken-wall persistence, Siphon minting energy, cone Euler skew…) | Prototype is the spec of feel; ambiguities now have receipts. Divergence rulings due at the U2 gate |
| `SWEEP` | **U1 verification:** batchmode compile zero errors ×2; seeder produced all 9 data assets with frozen values, second run `0 created / 9 kept` (idempotency proven); **15/15 edit-mode tests pass** incl. spline parity (lap ≈ 1823 u, min corner radius ≈ 58 u); adversarial review workflow (3 reviewers + refuters, 10 agents) found 6 real issues — all fixed (gate torus pose, slab tilt, starfield omission, test dead code, shader-stripping note, cone-skew ruling logged) | Evidence for the U1 acceptance gate before asking Seni to sign off |

**⏭ Next:** **U1 GATE — awaiting Seni's sign-off.** Open `UnityProject` in Unity 6000.4.4f1, open `Race.unity`, press Play: ship spawns at the start line, chase camera follows. On sign-off → U2 (EnergyCore + ShipController + mutual exclusion, the kill-gate re-validation).

---

## 2026-07-09 — Session 3

| Type | Change | Rationale |
|---|---|---|
| `DESIGN` | **P2 kill gate PASSED** — Seni approved the concept after playing the prototype | The shared-energy trade-off is fun; the project earns its Unity build |
| `SCOPE` | Design lock closed: D1 race-mode-first, D2 async ghosts, D3 three hulls, D4 tuning frozen — all ✅ | Stage 2 complete; no open design decisions blocking the build |
| `PROTOTYPE` | **Wrote the gated Unity handoff `Afterburn_BUILD.md` (milestone U0 ✅)** | Single source of truth for a Claude Code Unity build agent to build out the assets, phase-gated U1→U7 |
| `TUNING` | Froze all 12 prototype tuning values + ship-feel reference constants into `GameTuning` spec (BUILD §5.3, §6) | The loop was signed off *at these numbers*; Unity must reproduce the ratios exactly |
| `UNITY` | Handoff specifies assembly split, SO data model (`HullDefinition`/`PilotDefinition`/`GameTuning`), per-system port, asset inventory, and portrait/Safe-Area/uGUI+TMP/PrimeTween conventions | Executable, studio-convention-aligned build plan |
| `UNITY` | Flagged **orientation (portrait per convention vs landscape)** and Unity version as open inputs; Core is orientation-agnostic | Racer-in-portrait is unusual — surfaced for Seni rather than silently assumed |

**⏭ Next:** Seni answers BUILD §12 open inputs (Unity version, orientation, repo, backup), then the Unity build agent runs **U1** and stops at its gate.

---

## 2026-07-09 — Session 2

| Type | Change | Rationale |
|---|---|---|
| `PROTOTYPE` | **Built the playable browser prototype** (`Afterburn_Prototype.html`) — single self-contained file, Three.js r128 (CDN) + vanilla JS modules + DOM HUD | Fastest path to a file Seni can double-click and play; module boundaries map 1:1 to the planned Unity assembly split |
| `PROTOTYPE` | Implemented **all phases P0–P6** in one build: greybox track + chase-cam, EnergyCore, fire/shield/boost with mutual exclusion, 3 hulls, 4 pilot abilities, ghost opponents, bounty + intel, run summary + restart, 12-slider tuning panel | Deliver a complete loop to playtest, not just P0–P2 |
| `PROTOTYPE` | **Core P2 mechanic wired exactly to spec:** EnergyCore is the sole spend-gate; only one of boost/shield/fire active at once; energy regenerates *only* during coast (no action) | This mutual exclusion is the whole game — it must be honest to test the kill gate |
| `UNITY` | Deviation from architecture §3: used **vanilla JS + DOM HUD instead of a React artifact** | Standalone file needs to run at 60fps from disk and load Three from a CDN; the loop is pure-JS via refs either way, and HUD→uGUI in Unity regardless. Module split preserved for handoff |
| `FIX` | Redesigned the track centreline — original hand-placed control points **cusped at the start/finish seam** (path doubled back ~180°, 2.9-unit turn radius). Replaced with a generated star-convex loop (min radius now 57.9, drivable at boost speed) | Original track was un-completable at the start line |
| `TUNING` | Softened wall contact from a hard `×0.82`/frame scrape (compounded to a dead stop) to a `×0.92` scrape + slide-along-wall heading nudge | Wall contact should cost speed, not trap you |
| `TUNING` | Nerfed ghost AI skill band to 0.80–0.90× top speed | Ghosts never scrape walls or mis-corner; needed a handicap so a skilled human can win on energy play |
| `SWEEP` | Verified in-browser: Three loads, scene builds (60 meshes), render never throws, **zero console errors**; full 3-lap race → finish → summary loop confirmed; boost confirmed +40% top speed (62→86); all 4 abilities + combat + bounty reward fire correctly | Evidence the loop is functional before asking Seni to judge the kill gate |

**⏭ Next:** Seni playtests and rules on the **P2 kill gate** — is the boost/fire/shield choice fun? If yes, freeze tuning values and write the Unity handoff. If no, tune (⚙ panel) or kill.

---

## 2026-07-09 — Session 1

| Type | Change | Rationale |
|---|---|---|
| `DESIGN` | Concept defined: 3D space-race arena, ship + pilot dual progression, race mode + battle mode | Seni's original concept for slot A |
| `DESIGN` | **Core mechanic set: single shared Energy pool** driving boost / fire / shield with mutual exclusion | Creates a live trade-off every second — the source of strategic depth |
| `DESIGN` | Ship upgrades redefined as **sidegrades** (Light / Medium / Heavy), never straight power | Linear "+10% speed" creates power, not decisions; also prevents pay-to-win |
| `DESIGN` | Pilot upgrades restricted to **cooldown reduction only** | Protects competitive integrity while keeping monetisation alive |
| `DESIGN` | Added **Bounty** (leader marked, 2× reward) and **Intel catch-up** (trailers see leader's energy) | Comebacks without insulting rubber-banding |
| `DESIGN` | Added **spec-gated shortcuts** (light-only gaps, heavy-only breakable walls) | Loadout choice opens different racing lines |
| `SCOPE` | **Race mode ships first**; battle mode deferred to content update | Last-man-standing is the most crowded, costly, netcode-heavy mode |
| `SCOPE` | **Async ghost opponents** replace real-time PvP at launch | Removes netcode, matchmaking, server cost and cheating — highest-leverage risk cut |
| `SCOPE` | Prototype non-goals fixed: no IAP, no art, no save, no touch, one arena | Prototype exists only to test the energy loop |
| `PROTOTYPE` | Architecture drafted: Three.js (r128) + React single-file artifact; module split defined | Studio pattern; r128 constraints noted (no CapsuleGeometry, no OrbitControls) |
| `PROTOTYPE` | **P2 declared the kill gate** — if boost/fire/shield choice isn't fun with primitives, redesign or kill | Prevents spending art/Unity budget on an unproven loop |
| `TUNING` | 12 parameters exposed as live sliders; defaults set | Studio convention: parameterised, never hard-coded |

---

## Pending / awaiting Seni

- [x] Git repo URL — `https://github.com/zeusiam/Afterburn` (2026-07-09)
- [x] Manual backup location — `/Volumes/Veratus Group/manual Backups/Afterburn` (2026-07-09)
- [x] Confirm race-mode-first — D1 ✅
- [x] Confirm async ghosts — D2 ✅
- [x] Confirm 3 hulls for prototype — D3 ✅
- [x] Confirm keyboard controls for prototype — ✅ (prototype shipped keyboard-only)
- [ ] **U1 gate sign-off** (next action once U1 verification passes)
