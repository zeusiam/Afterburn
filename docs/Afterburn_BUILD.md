# Afterburn — Unity Build Handoff
## Veratus Games · Project ALTERNATE · letter **A**, slot 7

**Git repo:** `https://github.com/zeusiam/Afterburn`
**Manual backup location:** `/Volumes/Veratus Group/manual Backups/Afterburn`
**Backup procedure:** every session → `git commit` + copy working tree to the manual backup location above.

> **This is the milestone-U0 gated handoff.** It is the single source of truth for building
> Afterburn in Unity. It exists because the browser prototype (`Afterburn_Prototype.html`)
> **passed the P2 kill gate** — Seni confirmed the shared-energy trade-off is fun (2026-07-09).
> Everything here is downstream of preserving that loop.

---

## 0. How to use this document

This is written to be executed by a **Claude Code Unity build agent**, one gated phase at a time.

- **Build phases U1 → U7 in order.** Each has an **acceptance gate**. Stop at every gate, verify, get Seni's sign-off, then continue. Do not build ahead.
- **The prototype is the spec of feel.** When a number or behaviour is ambiguous, open `Afterburn_Prototype.html` and match it. The prototype's module boundaries were chosen to map 1:1 onto the Unity assemblies below.
- **Nothing is hard-coded.** All balance lives in ScriptableObjects (§5). The build agent never types a tuning constant into a MonoBehaviour.
- **The one rule in §2 is non-negotiable.** If a change would break it, stop and escalate.

---

## 1. What is locked (design lock — Stage 2 complete)

| Decision | Locked value | Source |
|---|---|---|
| **Launch mode** | Race mode only. Battle mode = post-launch content update | D1 confirmed |
| **Opponents** | Async **ghosts**. No real-time PvP / netcode at launch | D2 confirmed |
| **Hull count** | **3** (Light / Medium / Heavy) | D3 confirmed |
| **Core loop** | Single shared **Energy** pool; boost/fire/shield mutually exclusive; regen only while coasting | **P2 passed** |
| **Tuning baseline** | Frozen from prototype → `GameTuning` (§5.3) | D4 frozen |
| **Monetisation** | Cosmetics + pilots. **No stat-power in ranked.** Pilot upgrades reduce cooldown only | Locked |

**Still explicitly OUT of this build** (unchanged from the prototype non-goals): real-time multiplayer, battle mode, multiple arenas, full IAP economy polish, progression/save beyond loadout. One arena, greybox-first.

---

## 2. The one rule that must not break

> **`EnergyCore` is the sole authority on spending.** Boost, Fire and Shield do not touch energy directly — they *ask* `EnergyCore.TrySpend(...)`. **At most one** of {Boost, Shield} is active in a frame; **Fire** is a discrete action blocked while either is active; **regen runs only when none of the three is active this frame.** Energy floors at 0, caps at max, never goes negative.

This mutual exclusion is the entire game. It is what the prototype proved. Every system in §7 is built around it. If any feature (a pilot ability, a pickup, a shortcut) lets a player boost *and* shield, or regen while acting, it has broken Afterburn — reject it.

---

## 3. Tech stack & studio conventions

| Layer | Choice |
|---|---|
| Engine | Unity **6000.4.4f1** (studio-convention version — matches all sibling projects) · URP |
| Language | C#, nullable enabled, `Afterburn.*` assemblies |
| UI | uGUI + **TextMeshPro** |
| Tweening | **PrimeTween** (no DOTween) |
| Canvas | **2796 × 1290, Match 0.5**, **Safe Area component on every screen root** |
| Orientation | **Landscape** (Seni confirmed 2026-07-09 — deliberate deviation from the portrait studio convention for the chase-cam racer) |
| Pooling | Studio object-pool utility for all projectiles / VFX / decoys |
| Data | ScriptableObjects, seeded via editor menu `Veratus/Afterburn/Create or Update SOs` |
| Input | Unity Input System — touch (on-screen) + keyboard (editor/dev) |
| Audio | Synth/placeholder SFX slots wired now; real audio post-loop |

**Assembly split (hard boundary):**

| Assembly | Contains | May reference |
|---|---|---|
| `Afterburn.Core` | Pure game logic & data: EnergyCore, ShipController, CombatSystem, PilotAbility, TrackSystem, GhostSystem, BountySystem, RaceDirector, all SO definitions | UnityEngine (math/transform only), nothing UI |
| `Afterburn.View` | MonoBehaviour presenters: ship visuals, camera, projectile/VFX pooling, track rendering | Core |
| `Afterburn.UI` | HUD, menus, loadout, summary, tuning-debug panel | Core, View |

Core must be unit-testable with no scene. UI never mutates game state directly — it reads Core and raises intents.

---

## 4. Project structure

```
Assets/Afterburn/
  Code/
    Core/        (asmdef: Afterburn.Core)
    View/        (asmdef: Afterburn.View)
    UI/          (asmdef: Afterburn.UI)
    Editor/      (asmdef: Afterburn.Editor — SO seeder, gizmos)
  Data/
    GameTuning.asset
    Hulls/       Light.asset  Medium.asset  Heavy.asset
    Pilots/      Vex.asset  Sora.asset  Kade.asset  Nyx.asset
    Tracks/      Arena01.asset
  Prefabs/
    Ship.prefab  Projectile.prefab  Decoy.prefab
    Gate_Checkpoint.prefab  Gate_LightShortcut.prefab  Wall_HeavyShortcut.prefab
    HUD.prefab  Screen_Loadout.prefab  Screen_Summary.prefab
  Scenes/
    Boot.unity  MainMenu.unity  Race.unity
  Art/Greybox/   (primitives + placeholder materials only)
  Audio/         (SFX slots)
```

---

## 5. Data model → ScriptableObjects

All three definitions are seeded/updated idempotently by the editor menu `Veratus/Afterburn/Create or Update SOs` (studio convention). Values below are **frozen from the prototype** and must be reproduced exactly.

### 5.1 `HullDefinition`  (3 assets)

| Field | Light | Medium | Heavy |
|---|---|---|---|
| `displayName` | Light | Medium | Heavy |
| `maxEnergy` | 80 | 100 | 130 |
| `regenPerSec` | 11 | 8 | 5 |
| `topSpeedMult` | 1.15 | 1.00 | 0.88 |
| `mass` | 0.8 | 1.0 | 1.4 |
| `collisionRadius` | 1.6 | 2.0 | 2.6 |
| `gateAccess` | LightGap | None | HeavyWall |
| `tintColor` | #37D0FF | #9D7BFF | #FF8A3C |

`gateAccess` is an enum flag driving spec-gated shortcuts (§7.5). **These are sidegrades — never expose a straight "+X% power" upgrade.**

### 5.2 `PilotDefinition`  (4 assets)

| Field | Vex | Sora | Kade | Nyx |
|---|---|---|---|---|
| `displayName` | Vex | Sora | Kade | Nyx |
| `abilityName` | EMP Pulse | Phase Shift | Siphon | Decoy |
| `cooldownSec` | 18 | 15 | 20 | 22 |
| `abilityType` | EmpPulse | PhaseShift | Siphon | Decoy |
| `abilityParam` | 30 (energy drained, radius 70) | 1.2 (intangible sec) | 25 (energy stolen) | 3.0 (decoy sec) |

**Ability upgrades reduce `cooldownSec` only.** Never damage, never speed. This is the monetisation guardrail.

### 5.3 `GameTuning`  (1 asset — the frozen prototype defaults)

| Key | Value | Meaning |
|---|---|---|
| `energyMaxScale` | 100 | Global × on each hull's `maxEnergy` (100 = ×1.0) |
| `regenScale` | 8 | Global × on each hull's `regenPerSec` (baseline 8 = ×1.0) |
| `boostDrainPerSec` | 25 | Energy/s while boosting |
| `boostSpeedMult` | 1.40 | Top-speed multiplier while boosting |
| `fireCost` | 20 | Energy per shot |
| `fireSpeedDip` | 0.90 | Multiply current speed once on fire |
| `shieldDrainPerSec` | 15 | Energy/s while shielding |
| `shieldDamageMult` | 0.40 | Incoming damage × while shielding |
| `shieldSpeedCap` | 0.80 | Top-speed × while shielding |
| `bountyRewardMult` | 2.0 | Reward × for hitting the marked leader |
| `abilityCooldownScale` | 1.0 | Global × on all pilot cooldowns |
| `raceLaps` | 3 | Laps per race |

Keep the on-screen tuning panel from the prototype as a **dev-only debug overlay** in Unity (editor + dev builds), writing to a runtime copy of `GameTuning`. It is how balance gets re-tuned on device without recompiling.

---

## 6. Reference feel constants (reproduce the prototype's handling)

These live in `GameTuning` too (a `ShipFeel` sub-block) or in `HullDefinition` where hull-specific. They are the prototype's **felt** values — the loop was signed off *at these numbers*, so reproduce the **ratios** exactly.

| Constant | Prototype value | Notes |
|---|---|---|
| `baseTopSpeed` | 62 u/s (× hull `topSpeedMult`) | Medium cruises 62; boost → ~86.8 |
| `thrustAccel` | 55 u/s² | |
| `dragPerSec` | 0.6 (× speed) | Terminal ≈ accel/drag |
| `brakeDecel` | 80 u/s² | |
| `turnRate` | 2.4 rad/s × `bite` | `bite = clamp01(speed/25 + 0.2)` — less bite at crawl |
| `wallScrape` | ×0.92 speed on contact | + slide-along-wall heading nudge 0.18 toward tangent |
| `fireTapCooldown` | 0.28 s | Min gap between shots |
| `projectileDamage` | 25 energy | Applied to target's pool |
| `projectileSpeed` | 170 u/s | + 0.4 × shooter forward speed |
| `spinoutDuration` | 0.7 s | On being hit: cap × 0.55 |
| `bountyBaseReward` | 8 energy | × `bountyRewardMult` when target is the leader |
| `aiSkillBand` | 0.80–0.90 × top | Ghosts never scrape walls; this handicap keeps them beatable |
| `aiFireRange` | 55 u | AI fires at the bounty leader within this |

> **Unit scaling to Unity:** the prototype track is ~1823 u long (lap ≈ 29 s at cruise). Pick a Unity scale so **1 lap ≈ 25–40 s** and hulls read at a believable metres/sec, then apply that **single scale factor to all distance/speed/accel constants together**. Energy, time, and multiplier values are unit-less — copy them verbatim. Track half-width = 17 u; min corner radius ≈ 58 u (drivable at boost).

---

## 7. System specs (module → Unity)

Each maps a prototype module (`Afterburn_Prototype_Architecture.md §4`) to a Unity class in `Afterburn.Core` unless noted. Presenters that touch scene objects live in `.View`.

### 7.1 `EnergyCore` (Core)
- **Owns:** one float pool per racer, drain/regen, spend-gating. Sole spend authority (§2).
- **API:** `bool TrySpend(float amount)`, `void Drain(float perSec, float dt)`, `void Regen(float perSec, float dt)`, `float Ratio`, `void Set(float max)`.
- **Invariant:** clamp [0, max]; expose `IsActingThisFrame` so nothing regens while acting.

### 7.2 `ShipController` (Core logic + View presenter)
- **Owns:** thrust/steer/speed integration, wall collision (nearest-centreline lateral clamp + slide), speed caps for boost/shield/spinout, hull application.
- **Depends on:** `EnergyCore`, `HullDefinition`, `TrackSystem` (for nearest sample + shortcut allowance).
- **Mutual-exclusion resolver lives here** and calls `EnergyCore` — mirror `updatePlayer()` in the prototype exactly: boost > shield priority for held inputs; fire only if neither active; drain/regen selection; caps.
- **View:** `ShipView` renders the hull mesh + thruster; reads state, never writes.

### 7.3 `CombatSystem` (Core) + pooled `Projectile` (View)
- **Owns:** firing, **pooled** projectiles, hit detection, damage → energy drain + spinout, shield damage reduction, bounty reward payout, Kade siphon resolution.
- **Pooling:** use the studio object pool. Never `Instantiate` per shot.
- **Hit rule:** intangible (Sora) targets ignore hits; shielded targets take `× shieldDamageMult`; hitting the bounty leader pays shooter `bountyBaseReward × bountyRewardMult`.

### 7.4 `PilotAbility` (Core)
- **Owns:** cooldown timer, activation, effect dispatch by `abilityType`.
- **Effects:** EmpPulse (drain 30 from racers within 70), PhaseShift (intangible 1.2 s — skips wall + hit collision), Siphon (arm next hit to steal 25), Decoy (spawn pooled decoy that AI targets for 3 s).
- **Cooldown = `cooldownSec × abilityCooldownScale`.** Upgrades only shrink `cooldownSec`.

### 7.5 `TrackSystem` (Core) + `TrackDefinition` (SO) + track renderer (View)
- **Owns:** centreline spline, dense sample cache (pos/tangent/normal), nearest-sample query, lap/progress bookkeeping (unwrapped fraction + wrap detection), checkpoints, **spec-gated shortcuts**.
- **`TrackDefinition` asset:** spline control points, half-width, checkpoint indices, shortcut zones `{type: LightGap|HeavyWall, range, extraInnerAllowance, side}`.
- **Shortcuts:** in a zone, inner wall allowance opens **only** if the hull's `gateAccess` matches (or Sora is phasing). Heavy smashes the `HeavyWall` slab on contact (disable + shatter VFX).
- **Arena01:** reproduce the prototype's star-convex loop (generated, min radius ≈ 58). Greybox ribbon + walls + gates + start/finish.

### 7.6 `GhostSystem` (Core)
- **Launch approach:** async ghosts = **recorded loadout runs** replayed, *or* the prototype's synthetic curve-follower with an energy sim (skill band 0.80–0.90, boost on straights, coast to regen, fire at bounty leader in range). Ship both: synthetic ghosts for day-one filler, recorded ghosts as they accumulate.
- **Record format:** per-frame input + loadout, deterministic playback. Zero netcode.

### 7.7 `BountySystem` (Core)
- **Owns:** each frame mark the max-progress racer as leader; feed 2× reward to `CombatSystem`; expose leader energy to trailers for **Intel**.
- **Intel = information, not speed:** trailing racers see the leader's live energy; leaders see nothing extra.

### 7.8 `RaceDirector` (Core)
- **Owns:** race state machine (Menu → Countdown → Racing → Finished), racer roster, positions, timer, per-run stats, finish detection (`progress ≥ raceLaps`), run summary payload.

### 7.9 Presentation
- **`ChaseCamera` (View):** smooth follow behind ship, look-ahead. Retune for portrait framing (§8).
- **HUD / screens (UI):** §8.

---

## 8. Front-end / HUD / screens

**Every screen root gets the Safe Area component. Canvas 2796×1290, Match 0.5. uGUI + TMP. PrimeTween for all motion.**

| Screen | Contents |
|---|---|
| **Boot** | Studio splash → load MainMenu |
| **Loadout (MainMenu)** | Hull picker (3), pilot picker (4), each showing its sidegrade/ability copy from the SOs; START |
| **Race HUD** | Energy bar (the hero element — one pool, colour-coded by active mode) · Boost/Fire/Shield/Coast state chips · position `n/4` · lap `n/3` · time · pilot cooldown meter · **Bounty tag** (when you lead) · **Intel** leader-energy readout (when you trail) · minimap · toast feed |
| **Summary** | Finish place, time, shots/hits/bounty-hits/boost-time, "One more run" + "Change loadout" |
| **Touch controls** | On-screen: steer (left/right or tilt-zone), thrust, and **one energy-action wheel** for boost/fire/shield that enforces mutual exclusion at the input layer. Map the prototype's keyboard scheme 1:1 for dev builds |

> **✅ Orientation resolved (Seni, 2026-07-09): LANDSCAPE.** A deliberate deviation from the portrait studio convention — classic racer framing, wider view of the track ahead. Canvas reference is **2796×1290**, chase camera frames low/behind, HUD anchors to the corners. Core is orientation-agnostic; this affects camera framing, Canvas reference, and touch layout only.

---

## 9. Asset inventory — what "build out the Assets" means concretely

Checklist of every asset to author. Greybox/primitive only until the loop is re-proven in Unity (U2 gate).

| Asset | Type | Phase | Notes |
|---|---|---|---|
| `GameTuning.asset` | SO | U1 | §5.3 frozen values, seeded by editor menu |
| `Light/Medium/Heavy.asset` | SO ×3 | U1 | §5.1 |
| `Vex/Sora/Kade/Nyx.asset` | SO ×4 | U1 | §5.2 |
| `Arena01.asset` (`TrackDefinition`) | SO | U1 | spline, checkpoints, shortcut zones |
| SO seeder | Editor script | U1 | `Veratus/Afterburn/Create or Update SOs` |
| Object pool utility | Code/prefab | U1 | studio convention |
| Input Actions asset | Asset | U1 | touch + keyboard |
| `Ship.prefab` | Prefab | U2 | greybox hull, thruster, tint driven by `HullDefinition` |
| Greybox track mesh + materials | Art | U2 | ribbon, walls, centre line, ground grid |
| `Gate_Checkpoint.prefab` | Prefab | U2 | torus/ring |
| `Projectile.prefab` | Prefab (pooled) | U2/U3 | sphere + trail |
| `Gate_LightShortcut.prefab`, `Wall_HeavyShortcut.prefab` | Prefab ×2 | U3 | pillars / breakable slab + shatter |
| `Decoy.prefab` | Prefab (pooled) | U3 | Nyx |
| Bounty marker | Prefab/VFX | U4 | leader crown/ring |
| `HUD.prefab` | Prefab (UI) | U5 | §8, Safe Area |
| `Screen_Loadout.prefab`, `Screen_Summary.prefab` | Prefab ×2 | U5 | Safe Area |
| Dev tuning overlay | Prefab (UI) | U5 | port of prototype panel, dev-only |
| Boot / MainMenu / Race scenes | Scene ×3 | U1/U5 | |
| SFX slots (synth placeholders) | Audio | U2+ | boost, fire, shield, hit, bounty, ability |

---

## 10. Gated build phases (Stage 3 · U1–U7)

Build in order. **Do not pass a gate without Seni's sign-off.**

| Phase | Goal | **Acceptance gate** |
|---|---|---|
| **U1** | Inventory + skeleton: 3 assemblies, folders, 3 scenes, all SOs seeded (§5), object pool, input, chase camera, empty greybox arena | Project compiles; `Veratus/Afterburn/Create or Update SOs` produces all 8 data assets with frozen values; ship spawns and camera follows |
| **U2** | **Core energy loop** on greybox Arena01: `EnergyCore` + `ShipController` + boost/fire/shield **mutual exclusion** + walls + lap logic | **Feels like the prototype.** Boost/shield/fire are mutually exclusive, regen only on coast, running dry feels bad. Side-by-side with the HTML at the frozen tuning — no drift |
| **U3** | Hulls + pilots as SOs, selectable pre-run; abilities + cooldowns; spec-gated shortcuts | Each hull wins a different scenario; each ability visibly swings a race; Light takes the gap, Heavy smashes the wall |
| **U4** | Ghost system (synthetic + recorded), bounty, intel | Ghosts race believably with real loadouts; comebacks happen via bounty/intel, **no rubber-banding** |
| **U5** | Front-end, HUD, **Safe Area on every root**, summary, dev tuning overlay | Full loop menu→race→summary→restart; "one more run" impulse present on device |
| **U6** | Monetisation scaffolding: cosmetics + pilot slots; **ranked normalises stats**, upgrades = cooldown only | Purchasable cosmetics/pilots exist; ranked confirmed pay-to-win-proof (no stat-power) |
| **U7** | Store prep: icons, listings, signed **iOS + Android** builds | Signed builds install and run a full race on device |

**Phase 0 (U1) rule:** produce the inventory + skeleton and **stop for sign-off** before writing gameplay (U2). Standard studio gated-build discipline.

---

## 11. Per-phase verification

- **U2 is the re-validation of the kill gate in Unity.** Before sign-off, diff behaviour against `Afterburn_Prototype.html` at identical `GameTuning` values: boost top-speed ratio (×1.40), regen-only-on-coast, mutual exclusion, dry-out feel, wall scrape. Any divergence = a bug in the port, not a re-tune.
- Every phase: no console errors; Core has edit-mode unit tests for `EnergyCore` (spend/regen/exclusion), `TrackSystem` (progress wrap, nearest, shortcut gating), `BountySystem` (leader selection, reward mult).
- Run the studio verify skill on the affected flow each phase.

---

## 12. Open inputs — ALL RESOLVED (Seni, 2026-07-09)

- [x] **Git repo URL:** `https://github.com/zeusiam/Afterburn`
- [x] **Manual backup path:** `/Volumes/Veratus Group/manual Backups/Afterburn`
- [x] **Unity version:** **6000.4.4f1** (matches all sibling Veratus projects)
- [x] **Orientation:** **LANDSCAPE** — deliberate deviation from portrait convention; Canvas 2796×1290 (§8)
- [x] **Ghost approach:** **synthetic day-one + recorded over time** (as recommended, §7.6)

---

## 13. Appendix — prototype → Unity mapping

| Prototype artefact | Becomes in Unity |
|---|---|
| Tuning panel values (§5.3) | `GameTuning` SO + dev debug overlay |
| `HULLS` table | `HullDefinition` ×3, seeded via editor menu |
| `PILOTS` table | `PilotDefinition` ×4 |
| Module list (`§4` architecture) | Assembly split `Afterburn.Core / .View / .UI` |
| `EnergyCore` spend-gate | `EnergyCore.TrySpend` — the §2 invariant |
| Projectile pool | Studio object-pool utility |
| Star-convex track + shortcuts | `TrackDefinition` (Arena01) + `TrackSystem` |
| Synthetic ghost AI | `GhostSystem` synthetic ghosts (+ recorded) |
| Chase cam / DOM HUD | `ChaseCamera` (View) / uGUI+TMP HUD (UI), Safe Area on every root, Canvas 2796×1290 (landscape) |

---

*Handoff written 2026-07-09. Prototype passed the P2 kill gate; this document authorises the Unity build. Build gated, U1 → U7, sign-off at every gate.*
