# Afterburn — UI & Environment Requirements Spec
## Veratus Games · Project ALTERNATE · letter **A**, slot 7

**Companion docs:** `Afterburn_BUILD.md` (master handoff — tech stack §3, screens §8, assets §9, gates §10) · `Afterburn_PortSpec.md` (prototype-exact values, greybox §10) · `Afterburn_DesignReview.md` (4-lens panel).
**Written 2026-07-09. This document governs all visual, UI, and environment work from U2 art unlock through fast-follow arenas.**

---

## 0. How to use this document

Written to be executed by a **Claude Code design/build agent**. Read before authoring any material, mesh, shader, screen, or VFX.

- **Work is gated.** Greybox (PortSpec §10 inventory) is the only permitted visual state until the **U2 gate passes** ("feels like the prototype", side-by-side parity). No art-pass asset ships, is enabled, or is wired into the Race scene before its phase in §8. Building an asset ahead as a disabled prefab is permitted only where §8 marks it "spec now".
- **Every deliverable has acceptance criteria.** A deliverable is not done until its criteria are objectively verifiable (a number, a toggle test, an editor assertion, or a side-by-side capture). If a criterion cannot be checked, stop and escalate.
- **Nothing is hard-coded.** Every color, size, duration, and easing in this document lives in the token system (§2.3–§2.4, seeded as data per §7). No hex literal, pixel value, or duration is typed into a MonoBehaviour or prefab field directly — reference the token.
- **Untouchable:** the BUILD §2 energy rule (EnergyCore sole spend authority, boost/shield mutual exclusion, regen only on coast) and the frozen `GameTuning` values (BUILD §5.3, §6). Visual and UI work **reads** Core state and never writes it. No VFX, shader, or UI element may call `TrySpend`, grant energy, alter collision, or imply an energy state that is not true this frame.
- **Owner decisions locked 2026-07-09:** 1 launch arena (Arena01, flat) + 2 full-3D fast-follow arenas; ship physics is 3D-track-frame from U2 (ship glued to ribbon, no slope gravity); **no ads, ever — no ad SDK, no ad slots in any layout**; Game Center first; Store + Season screens exist at launch scope (cosmetics-only monetisation); cosmetic hook points (liveries incl. reactive, boost trails, nameplates/badges) are first-class citizens of every relevant system.
- **The builder's means are the medium.** Everything below is achievable with: procedural C# meshes, HLSL/ShaderGraph, emissive/gradient/vertex-color/SDF work, URP particles, uGUI+TMP built by editor scripts, SVG→sprite icons, token systems, PrimeTween. **No painted textures, no sculpting, no photo-sourced assets — anywhere.** If a requirement seems to need one, the requirement is misread; re-derive it from shader math or geometry.

---

## 1. Art direction — SIGNAL VOID

**Named direction: `SIGNAL VOID` — light is information.** Lineage: Rez / Thumper / Tron. The code-only pipeline is the style, not a limitation.

### 1.1 Pillars (all four are acceptance criteria for every visual asset)

1. **Light is information.** The world stays near-black (`--void`). Every emissive element carries gameplay meaning; decorative glow is banned. Palette roles (§2.3) are fixed and enforced everywhere — gameplay, cosmetics, UI, themes.
2. **Geometry over texture.** Zero painted textures. Form comes from procedural low-poly meshes with hard normals, vertex-color gradients, and SDF patterns computed in shaders.
3. **Readable at 90 u/s.** Environment is low-frequency (big shapes, slow gradients). High-frequency detail is reserved for interactables within ~50 u of the racing line. Contrast hierarchy, brightest to dimmest: **actionable now > state change > guidance > structure > backdrop**.
4. **State-true, frame-true.** Any visual that restates Core state (thruster tier, shield bubble, phase opacity, energy fill) must match Core's booleans/values the same frame. A lingering boost flame after EnergyCore self-cancels is a bug of the same severity as an energy leak.

### 1.2 Readability-at-speed rules (verify at full boost ≈ 86.8 u/s, on device)

| Rule | Requirement | Check |
|---|---|---|
| Hull ID at range | The 3 hull silhouettes distinguishable at 100 u against the void | Screenshot at 100 u; a viewer names the hull |
| State colors at range | Boost/shield/phase state of a rival readable at 55 u (aiFireRange) | Capture each state at 55 u; mode color identifiable |
| Track edge | Edge rails visible ≥ 150 u ahead at all speeds | Frame capture at boost on longest straight |
| Interactables | Gates/shortcut telegraphs begin reading ≥ 120 u before the zone | Ribbon tint lead-in distance = 120 u (token) |
| HUD glance cost | Energy remaining + mode readable in < 200 ms glance | Bar is bottom-center, mode = fill color, ticks = shots |
| No noise on the line | No emissive element within ±17 u of centreline that is not gameplay-meaningful | Audit script lists emissives per scene (§7 #3) |

### 1.3 Color tokens

Base palette is locked (PortSpec §10). Semantic tokens extend it. **All values below are the token table — the single source. Seeded as `AfterburnPalette` static class + swatch asset per §7.**

**Core roles (locked — never reassigned):**

| Token | Hex | Role |
|---|---|---|
| `--void` | `#05070F` | Background, world base, surface-0 |
| `--cyan` | `#37D0FF` | Player/track guidance, Light hull, regen/coast |
| `--violet` | `#9D7BFF` | Shield/Phase, Medium hull |
| `--orange` | `#FF8A3C` | Boost, Heavy hull, heat |
| `--gold` | `#FFD23F` | Leader/bounty/reward **only** |
| `--red` | `#FF4D6D` | Damage/danger **only** |
| `--wall-blue` | `#2B4D8F` | Passive structure |
| `--teal` | `#38F5C9` | Self-marker (minimap player), Siphon transfer |
| `--star` | `#6F8BD0` | Starfield, tertiary/ambient info |

**Mode tokens (HUD + reactive surfaces):** `--mode-boost` = `--orange` · `--mode-shield` = `--violet` · `--mode-fire` = `--red` (flash only) · `--mode-coast` = `--cyan` · `--energy-low` = `--red` at 2 Hz pulse below 25% · `--energy-empty` = `--red` full-bar flash ×2 on dry-out.

**UI surface/text tiers (derived from greybox family — no new hues):**

| Token | Hex | Use |
|---|---|---|
| `--surface-0` | `#05070F` | Screen background |
| `--surface-1` | `#0A1C3A` | Panels, cards |
| `--surface-2` | `#12203C` | Raised cards, inputs |
| `--surface-3` | `#1C2F57` | Hover/selected fill |
| `--stroke` | `#2B4D8F` | Borders, dividers |
| `--text-hi` | `#EAF2FF` | Primary text |
| `--text-mid` | `#9FC4FF` | Secondary text |
| `--text-low` | `#6F8BD0` | Tertiary/disabled labels |

**Enforcement:** gold and red never appear in any non-leader / non-damage context (incl. cosmetics — §3.4 hue-distance validator). Acceptance: audit script (§7 #3) finds zero out-of-role uses of `--gold`/`--red` per scene/screen.

### 1.4 Typography (TMP, Canvas 2796×1290)

One SIL-OFL geometric sans (builder selects, e.g. Space Grotesk-class), imported as TMP SDF atlases: **Medium + Bold**, plus tabular-numeral material preset for timers. All sizes are tokens (`AfterburnType`):

| Token | Size (pt @ 2796×1290) | Weight | Use |
|---|---|---|---|
| `type-countdown` | 220 | Bold | Race countdown numerals |
| `type-display` | 96 | Bold | Screen titles |
| `type-h1` | 72 | Bold | Section heads, finish place |
| `type-h2` | 56 | Bold | Card titles, HUD timer (tabular) |
| `type-body` | 40 | Medium | Body copy, toasts |
| `type-label` | 34 | Medium | Buttons, chips |
| `type-caption` | 28 | Medium | Stat labels, world nameplates |
| `type-micro` | 22 | Medium | Legal, tick labels |

HUD numerals always tabular. Minimum text size anywhere: 22 pt. Acceptance: every TMP component references a preset asset; zero per-component font-size overrides (audit script).

---

## 2. Environment system

All environment geometry is authored against the **track frame** `{pos, tan, nrm, up}` (flat Arena01 is the degenerate case, `up` = world-up). Ribbon shader UVs are frame-agnostic: `UV.x` = lateral −1..1, `UV.y` = arc-length meters from the 700-sample cache. Nothing in this section moves collision — the analytic lateral clamp in Core is untouched.

### 2.1 TrackRibbon shader (the environment hero)

One ShaderGraph on the spline-extruded ribbon. Layers, all shader math, no textures:

- **(a) Edge rails:** emissive cyan at lateral ±16.5 u, brightness scales with `_PlayerSpeed`.
- **(b) Centre-guide chevrons:** scrolling against travel, rate proportional to `_PlayerSpeed` — the ground itself communicates velocity.
- **(c) Lateral tick lines** every 20 u (stroboscopic speed sense).
- **(d) Shortcut approach tint:** ribbon tints toward the gate color (cyan/orange) starting 120 u before each zone.
- **(e) Vertex-color darkening** toward the walls for depth.

Construction: procedural ribbon mesh chunked into ~12 arc segments for frustum culling; emission > 1.0 HDR so bloom picks up rails/chevrons only. A **greybox-material toggle** must remain for parity diffs.
**Acceptance:** shader never displaces vertices; toggling greybox material reproduces PortSpec §10 road/stripe exactly; chevron frequency A/B'd at full boost at the U3 gate (readability may not regress vs greybox); ≤ 2 draw calls for the whole ribbon (SRP-batched chunks).

### 2.2 Wall / barrier kit

Same ribbon technique. Body `--wall-blue`, fresnel top-edge glow, **scrape-reactive flash**: ShipController's wall-contact event drives a localized emissive pulse (shader params: arc position + falloff, pooled per ship) so the ×0.92 scrape cost is *felt* visually.
**Acceptance:** scrape pulse appears within 1 frame of the contact event at the contact arc position; walls dissolve into fog at range (fog color match, §2.5); zero collision-affecting geometry.

### 2.3 Gates

- **Start/finish + checkpoint gates:** replace the greybox torus with a flat SDF ring quad, rotating tick marks, white/cyan emissive. Cheaper than torus geometry and reads at distance.
- **LightGap (`Gate_LightShortcut.prefab`):** cyan pillars joined by a light beam; floor chevrons key cyan **only when the player's hull is Light** (View reads `gateAccess` — personalized telegraph, information-not-power).
- **HeavyWall (`Wall_HeavyShortcut.prefab`):** orange slab pre-fractured into 12–16 procedural chunks; on `breakHeavy`, swap to pooled scripted chunk scatter animated with PrimeTween (no physics). Broken state resets at race start (Divergence #3 ruling); recorded ghosts serialize their wall-break so replays render the slab correctly.

**Acceptance:** SDF ring reads at 200 u; hull-keyed chevrons change with loadout swap and display only the local player's own `gateAccess`; slab shatter completes ≤ 0.8 s, chunks return to pool; telegraphs alter no allowance logic (Core `innerAllow` untouched).

### 2.4 Skybox / backdrop

Gradient skybox shader: 3-stop vertical gradient (horizon glow into void), **hash-noise in-shader starfield** (no texture), plus 1–2 large SDF-shaded celestial discs as distant billboards (stationary reference for velocity judgment). Starfield points **stretch along velocity above 70 u/s** (billboard elongation by `_PlayerSpeed`) for a free warp-streak effect. All parameters live in `ThemeDefinition`.
**Acceptance:** zero textures; skybox + backdrop ≤ 3 draw calls; streaks engage at exactly the token threshold; no visible tiling or noise shimmer at rest.

### 2.5 Fog & lighting recipe (per arena, all values in ThemeDefinition)

- Linear fog, Arena01 range **220→620**, fog color **matched to the skybox horizon stop** so walls dissolve into void — this is the LOD system; no other LOD exists.
- Lights: HemisphereLight-equivalent ambient (sky `#9FC4FF`, ground `#0A0F1E`, 0.9) + one directional (`#FFFFFF`, 0.9). **No realtime shadows anywhere** (prototype parity + budget §5).
**Acceptance:** at boost speed on device, no pop-in and no visible far track edge; fog/horizon delta ΔE imperceptible in capture.

### 2.6 ThemeDefinition SO + three arena treatments

`ThemeDefinition` (SO, seeded per §7): skybox gradient stops, fog color/range, track palette, wall tint, backdrop prop set, starfield density/color/streak threshold. A new arena look = a data file + prop list, not an art production.

| Arena | Theme | Signature | Shortcut manifestation |
|---|---|---|---|
| **Arena01 (launch)** | **Deep Void** — coldest, cleanest: void + cyan/`--wall-blue`, single ringed planet. Teaches the visual language. Flat; greybox until U2 passes. | The baseline star-convex loop | LightGap = cyan pillar gap; HeavyWall = orange breakable slab (as §2.3) |
| **Arena02 (fast-follow)** | **Ember Belt** — horizon warmed to deep ember, orange/gold rails (gold as *reward-route* accents only), silhouetted asteroid billboards, sparse gold starfield. Applied to **Redline Canyon**: track drops ~40 u to the canyon floor through 55° wall-carved banked sweepers. | **The Overrun** — blind crest into a canyon gap: pre-lip boost clears it; coasting drops to a safe mid-ledge route that rejoins. Airtime is a regen window. | LightGap = slot-canyon fissure (verticalClearance duct through a rock fin); HeavyWall = smashed ore-conveyor plug opening a drilled bore that cuts the last switchback |
| **Arena03 (fast-follow)** | **Reactor Violet** — interior megastructure: instanced procedural rib arches over the track, desaturated magenta-violet structure (pure `--red` stays reserved for damage), pulsing core-glow backdrop, no stars. Applied to **Anchor Station**: orbital-shipyard figure-8, over/under + spine tunnel with emissive strips and camera pull-in. | **The Gantry Cross** — the lap crosses over itself through drydock superstructure; rivals visible through grating below (Intel made physical) | LightGap = low service duct under a gantry (verticalClearance); HeavyWall = breakable floor grate dropping Heavy one layer onto an under-route |

**Acceptance:** Arena01 wired to Deep Void via SO at U3 (plumbing only until then); each fast-follow theme keeps `--red` damage-exclusive and `--gold` reward-exclusive (audit); swapping ThemeDefinition assets restyles the shared kit with zero shader or mesh edits.

### 2.7 Props / ambience list (all procedural, all backdrop-distance)

Ringed planet disc (SDF billboard) · reactor sun disc · asteroid silhouette billboards (Ember Belt) · instanced rib arches (Reactor Violet, GPU-instanced, ≤ 2 draw calls) · distant traffic streaks (optional particle ribbon, ≥ 400 u away). **No prop within 50 u of the racing line unless gameplay-meaningful.** Acceptance: backdrop total ≤ 10k tris, ≤ 6 draw calls.

---

## 3. Ship & VFX treatment

### 3.1 Hulls

Three procedural faceted hulls (**2–4k tris each**, hard normals, flat-shaded), one shared uber-material: vertex-color gradient, fresnel rim in the hull tint (from `HullDefinition.tintColor` — never hard-coded), SDF panel-line emission. Silhouettes are **gameplay information** (shortcut prediction, energy-pool estimate at range):

- **Light** = narrow dart · **Medium** = arrow (straightened-nose lineage of the prototype cone) · **Heavy** = wide hammerhead.

Ghosts use the same meshes with a **hologram variant material** (scanlines, ~0.7 opacity, slight desaturation) — the player is always the most solid object on track.
**Acceptance:** the three silhouettes distinguishable at 100 u (per §1.2); tint reads from `HullDefinition`; cosmetics may never alter silhouette (§4.5 guard); one material family, SRP-batched, 1–2 draw calls per ship.

### 3.2 Thruster tiers (read-only from Core state)

| Tier | State | Core glow | Trail |
|---|---|---|---|
| T0 | Coast | small core | faint 6 u |
| T1 | Thrust | core ×1.3 | 10 u |
| T2 | Boost | core ×1.8 (prototype scale parity) | 25 u + flare, color `#FF7A3C → #FFE14D` (prototype ramp) |

Trails: pooled ribbon meshes, tapered width curves, HDR emission. **Cosmetic hook: trail gradient + width curve are `CosmeticDefinition` parameters — boost trails are the purchased item players see (visible while boosting only).**
**Acceptance:** tier transitions occur the exact frame Core state changes; the boost flame cuts the **same frame** EnergyCore self-cancels at 0 (test against EnergyCore edit-mode scenarios); trails pooled, zero per-frame allocation.

### 3.3 VFX suite (all pooled via the studio pool; particles + shaders only; no textures)

| Effect | Spec | Cosmetic hook |
|---|---|---|
| **Boost feel package** | FOV **62→68 over 0.25 s** on entry (mirror on exit); camera-attached additive speed-line particle tube spawning **> 55 u/s**, scaling to 90; sub-pixel shake amplitude **≤ 0.05 u**, boost only. Entire package behind the **parity-mode toggle** — OFF for the U2 capture, enabled at U5 with sign-off. | Trail (§3.2) |
| **Fire** | 0.1 s muzzle quad; projectile = core sphere + 8 u additive `--red` tracer; hit = 12-particle radial spark + 0.15 s white target flash + floating "−25" (`type-caption`, world-space) | Muzzle tint (livery accent) |
| **Spinout (0.7 s)** | Thruster stutter, angular smoke puffs, hull emission flickers `--red` | — |
| **Shield** | Fresnel bubble, scrolling SDF hex lattice in `--violet`, intensity tied to drain state; hits while shielded ripple from impact point; bubble vanishes the exact frame shield ends | Lattice pattern index |
| **Dry-tank warning** | At energy = 0 under held drain: thruster cut to T0 same frame, hull emission gutters 0.5 s, HUD bar rim flashes `--red` ×2, action-wheel denial shake (§4.3) | Reactive liveries gutter below 25% (§3.4) |
| **EMP (Vex)** | **0.6 s expanding-ring windup telegraph + audio sting**, then cyan torus shockwave expanding to exactly radius 70 in ~0.4 s; affected ships flash cyan and shed drain motes | Ring trim tint |
| **Phase (Sora)** | 0.3 s pre-glow tell; screen-door dissolve at 0.4 opacity (prototype parity) with `--violet` rim; snap-back flash on exit | Rim pattern |
| **Siphon (Kade)** | Armed state = visible tether/glow on the ship (victim counterplay tell); on armed hit, 0.5 s mote stream victim→shooter in `--teal` | Mote shape index |
| **Decoy (Nyx)** | Octahedron becomes glitching hologram (vertex-jitter + scanlines); **deliberate tell: no thruster flame** — reading fakes is a learnable skill | Hologram tint |
| **Bounty marker** | ~60 u vertical `--gold` light pillar above the leader + gold minimap treatment + HUD crown tag; pillar visible through structure at Gantry-Cross-type overlaps | Nameplate/badge renders on ghost tag (§4.4) |
| **Wall-smash shatter** | §2.3 HeavyWall chunk scatter, PrimeTween, pooled | — |

**Acceptance (suite-wide):** every effect pooled (zero `Instantiate` at runtime after warm-up); no VFX calls `TrySpend` or grants effects; shield/phase/boost visuals diffed against Core booleans in an edit-mode test; ability VFX radii match `GameTuning`/`PilotDefinition` values read from the SOs.

### 3.4 Cosmetic hook points (first-class — build the sockets even before any SKU exists)

1. **Liveries:** the ship uber-shader exposes a cosmetic parameter block — base gradient (2 stops), SDF panel-line pattern index, emission pattern index, rim color, thruster core palette. `CosmeticDefinition` SO = that block. **Reactive liveries** (premium tier) read EnergyCore state read-only: trim flares on boost, shimmers on shield, pulses on fire, gutters below 25% — matching the HUD red-line.
2. **Thruster trails:** gradient + width curve parameters (§3.2), rendered only while boosting.
3. **Nameplates/badges:** TMP nameplate (`type-caption`, 28 pt, fades inside 40 u) + badge sprite on ghost holograms and on every leaderboard row (§4.4 list rows).

**Guardrails (validated in the seeder):** cosmetics may not alter silhouette; may not use reserved-state colorways as dominant schemes (no all-gold "fake leader", no violet trail reading as shield) — enforced by an **automated hue-distance check** against §1.3 reserved roles. Ghost record header carries `{liveryId, trailId, plateId}` (U4 format — header only, never the input stream).
**Acceptance:** hue-distance validator exists and fails a deliberately-violating test cosmetic; cosmetic block has no code path into Core (asmdef boundary + review); a ghost renders its header cosmetics in another player's race.

---

## 4. UI system

Canvas **2796×1290, Match 0.5, landscape**. uGUI + TMP. **Safe Area component on every screen root.** PrimeTween for all motion. HUD lives on a **Screen Space Overlay** canvas (native-res text over the 0.8-render-scale 3D pass, §6). **No ad slots exist in any layout — permanent.**

### 4.1 Screen inventory

| Screen | Layout spec |
|---|---|
| **Boot** | `--surface-0`, centered studio mark (SVG sprite), progress hairline (`--stroke`→`--cyan`), auto-advance to MainMenu. No interaction. |
| **MainMenu / Loadout** | Left 55%: 3D ship podium render (live hull + equipped cosmetics). Right 45%: **hull picker** (3 cards showing sidegrade copy + maxEnergy/regen/topSpeed bars from SO) and **pilot picker** (4 cards: ability name, copy, cooldown from SO). START (primary button, bottom-right). Top bar: Salvage balance, Store / Season / Settings / Game Center entry icons. |
| **Race HUD** | §4.2 — the most specified screen in the game. |
| **Summary** | Finish place (`type-h1`), time + best-lap delta line ("0.4 s off your best lap"), stat row (shots/hits/bounty-hits/boost-time), Salvage earned with style-bonus breakdown, **exactly ONE next-goal chip** — three impulse hooks total, hard cap (U5 gate criterion). Buttons: **One more run** (primary), **Rematch** (same ghost seeds), Change loadout. |
| **Settings** | Audio sliders, comfort toggles (FOV kick magnitude, shake on/off — ship conservative), control scheme, credits, restore purchases. List rows on `--surface-1`. |
| **Store** | Cosmetics-only, direct visible prices, no hard currency. Category tabs: Liveries (Standard/Reactive/Prestige tiers) / Trails / Plates / Pilots. Cards show live preview render of the cosmetic on the equipped hull. Salvage-purchasable items show Salvage price; IAP items show local currency. Copy slot for "nothing you can buy makes you faster". |
| **Season ("Circuit")** | 30-tier horizontal track, free lane + paid lane, weekly-objective list (retroactively completable), tier cards with cosmetic previews, anti-FOMO footer copy. Builds at U6 as scaffolding; populated post-launch. |
| **Game Center panels** | GC access point anchored top-left of MainMenu; native GC sheets for the 7 boards + achievements; in-game leaderboard list rows (rank, plate/badge, name, time, "Race this ghost" button). |

### 4.2 Race HUD — the hero screen

**Energy bar (THE hero element):**
- Bottom-center. Height 44 px. **Width proportional to hull `maxEnergy`:** 9 px per energy point → Light 720 px, Medium 900 px, Heavy 1170 px — the sidegrade is physically visible.
- **Tick marks every 20 energy = one shot** — players count remaining shots at a glance.
- **Primary fill is frame-true with NO tween** — it equals `EnergyCore.Ratio` the same frame, always. A trailing **loss-ghost fill** (dim `--red`, 35% alpha) tweens down over 0.4 s after a 0.25 s delay to dramatize damage.
- Fill color by mode: regen = `--cyan` with subtle upward shimmer · boost = `--orange` · shield = `--violet` · `--red` flash on fire spend. Whole bar pulses `--red` at 2 Hz below 25% (prototype parity); dry-out = rim flash ×2.
- Bottom edge 72 px above safe-area bottom.

**Other HUD elements:** state chips (BOOST/FIRE/SHIELD/COAST, above the bar, active chip lit in mode color, 90 ms swap) · position `n/4` (`type-h1`, top-left) · lap `n/3` (`type-h2`) · race timer (`type-h2` tabular, top-center-left) · **pilot cooldown**: radial sweep on the ability button (§4.3) + ready pulse · **★BOUNTY tag** (`--gold`, top-center, when leading; **threat-pulses when any racer is within 55 u behind**) · **Intel readout** (trailers only): mini leader-energy bar pinned to the leader's screen-edge indicator — never rendered for the leader · **minimap** top-right, 300×300 px, world-scale outline, player `--teal` r8, leader `--gold` r8 + ring, others `--red` r6; elevation tint layer reserved for Arenas 02/03 · **toast feed** top-center, max 2 stacked, **1.1 s lifetime (prototype parity)**.
**Layout law:** all HUD clusters ≥ 64 px inside the safe edge; **central 60% of the screen is UI-free** except the top-center toasts.

### 4.3 Touch layout

- **Left zone (left 38% of screen):** touch-and-hold = thrust; horizontal drag from touch origin = steer (dead zone 24 px, full deflection ±220 px); **release = coast** — regen is a physical release of the thumb. Keyboard maps 1:1 for dev builds.
- **Energy-action wheel (bottom-right):** outer radius 300 px, hub radius 120 px, center 96 px inside the safe corner. **Hub = FIRE** (discrete tap). **Upper arc segment (100°) = BOOST** (hold). **Lower arc segment (100°) = SHIELD** (hold). A held touch may slide between segments; the wheel emits **at most one held-mode intent per frame** (slide-to-switch releases the old intent the same frame the new engages) — **mutual exclusion enforced at the input layer**, before Core even arbitrates. Hub taps emit no fire intent while a segment is held.
- **Wheel states:** idle (segments at 40% emission) · held (segment 100%, mode color) · fire-flash (hub, 0.1 s) · **denial** (insufficient energy: 60 ms shake + dim, no intent emitted) · countdown-locked (all 30%).
- **Ability button:** above the wheel, radius 100 px; radial cooldown sweep; ready = rim pulse in pilot tint.

**Acceptance:** input layer provably never emits boost+shield in the same frame (edit-mode test on the wheel state machine); all touch targets ≥ 176 px diameter; layout valid on iPhone 17 Pro Max and SE-class safe areas.

### 4.4 Component library (each with all states: default / pressed / selected / disabled / [locked])

- **Buttons:** primary (filled `--cyan` on `--surface-0`, `--void` text), secondary (stroke), destructive (`--red` stroke). Press: scale 0.96.
- **Cards:** hull/pilot/cosmetic/tier cards on `--surface-1`, selected = `--surface-3` + tint stroke + 2 px underglow. Locked = 50% desaturation + price/unlock-condition chip.
- **Bars:** energy (hero spec §4.2), stat compare bars (loadout), cooldown radial, season progress.
- **Chips:** state chips, next-goal chip, style-bonus chips, price chips.
- **Toasts:** `--surface-2`, `type-body`, icon slot, 1.1 s hold.
- **List rows:** leaderboard rows (rank · plate/badge sprite · name · time · ghost button) — **the nameplate/badge cosmetic surface**.
**Acceptance:** every component exists once as a prefab, built by editor script; screens compose prefabs — zero one-off styled controls (audit).

### 4.5 Iconography (SVG → sprite)

All icons are authored as vector SVG by the build agent and imported to one UI sprite atlas. Style: 2-weight stroke system (4 px primary / 2.5 px detail at 96 px artboard), rounded caps, single-color — tinted at runtime via token, never baked-in color. Required set: hull class ×3, ability ×4, mode ×4 (boost/fire/shield/coast), bounty crown, salvage, store/season/settings/Game Center, ghost, locked, rematch.
**Acceptance:** every icon legible at 44 px; one atlas ≤ 2048²; all tints applied via palette tokens (audit).

### 4.6 Motion rules (PrimeTween — the only tween system)

| Motion | Duration | Ease |
|---|---|---|
| Screen in / out | 220 ms / 160 ms | OutCubic / InCubic |
| Button press / release | 80 ms / 120 ms | OutQuad / OutBack |
| Card select | 150 ms | OutCubic |
| Chip state swap | 90 ms | OutQuad |
| Toast in / out | 120 ms / 180 ms | OutCubic / InCubic |
| Loss-ghost fill | 400 ms (delay 250 ms) | OutQuad |
| Cooldown ready pulse | 300 ms | OutSine |
| Denial shake | 60 ms | punch |

**Law: the primary energy fill is never tweened.** Acceptance: audit script finds no tween targeting the primary fill; all durations referenced from the motion token table.

---

## 5. Performance budgets (the URP mobile contract — locked at U2, asserted at every gate)

**Target: 60 fps sustained on A15-class, 5-minute soak, 4 racers + max VFX.**

| Item | Budget |
|---|---|
| 3D render scale | **0.8** (~2237×1032); HUD on Screen Space Overlay at native 2796×1290 |
| MSAA | **4×** (near-free on Apple TBDR, ideal for vector edges). Depth Texture **OFF**, Opaque Texture **OFF** |
| Draw calls | **≤ 120** (SRP Batcher + uber-shader families; track ~12 chunks; 1–2 calls/ship; instanced VFX) |
| Visible tris | **≤ 150k** (track+walls ~80k · 4 ships ~16k · backdrop ~10k · remainder VFX) |
| Texture memory | **≤ 48 MB** (TMP atlases + one UI sprite atlas; all else is shader math) |
| Post-processing | **Bloom only** — load-bearing for SIGNAL VOID: half-res, max iterations 4, threshold 1.1, intensity ~0.6, HDR B10G11R11. No vignette pass (static UI quad instead), no CA/DoF/motion blur |
| Shadows | **Zero realtime shadow maps** (prototype parity) |
| Bloom fallback | If device thermals throttle: pre-authored billboard glow halos, bloom off — **decide by measurement, not taste** |

**Measurement procedure (per scene, per gate):**
1. Editor: run `Veratus/Afterburn/Audit Performance Budget` (§7 #3) — asserts draw calls (Frame Debugger count at the worst camera angle), tri count, material/texture inventory, out-of-role palette uses, un-pooled spawners.
2. Device: Xcode GPU capture on an A15-class iPhone — confirm render scale, MSAA, no depth/opaque resolves, bloom cost < 1.2 ms.
3. Soak: 5-minute race loop with 4 racers, all abilities cycling, boost held on straights — 60 fps sustained, no thermal downshift below 60 within the soak.
4. Record results in the gate sign-off note. **A scene over budget does not pass its gate.**

---

## 6. Asset & code conventions

- **Folders (extends BUILD §4):** `Assets/Afterburn/Art/` → `Shaders/`, `Materials/`, `Meshes/` (baked procedural), `VFX/`, `UI/` (sprites, fonts, TMP presets), `Themes/` (ThemeDefinition assets). Prefabs stay in `Assets/Afterburn/Prefabs/` per BUILD §9 names. Greybox stays in `Art/Greybox/` untouched — it is the permanent parity fixture.
- **Naming:** shaders `AB_<Thing>` (e.g. `AB_TrackRibbon`), materials `M_<Thing>`, meshes `SM_<Thing>`, VFX prefabs `FX_<Thing>`, UI prefabs `UI_<Thing>`, sprites `SPR_<Thing>`, themes `Theme_<Name>`.
- **Editor-script generation:** mirror the existing SO-seeder pattern — **idempotent create-or-update**, re-runnable without duplication, menu items under `Veratus/Afterburn/` (`…/Build UI Screens`, `…/Bake Track Meshes`, `…/Create or Update Themes`, `…/Audit Performance Budget`). Static meshes (track chunks, hulls, props) are generated by editor script and **saved as mesh assets**; only trails/VFX ribbons generate at runtime.
- **Materials are `.mat` assets referenced by prefabs — never runtime `Shader.Find`** (device-build shader stripping removes unreferenced shaders; Shader.Find returns magenta on device). Every shader must be reachable via a serialized material reference.
- **Prefab structure:** one root per prefab; View components read Core through a single presenter script; no `GetComponentInChildren` in per-frame paths (cache on Awake); all pooled prefabs implement the studio pool interface.
- **Tokens as data:** `AfterburnPalette` + `AfterburnType` + motion table seeded by the SO seeder; prefabs bind via token-reference components, not literal values.

---

## 7. Deliverables checklist (phased — do not build ahead)

| # | Deliverable | Phase | Acceptance criteria |
|---|---|---|---|
| 1 | Palette/typography/motion token system + swatch assets | **Now (spec, greybox-safe)** | Tokens seeded idempotently; zero literals in components (audit) |
| 2 | `ThemeDefinition` + `CosmeticDefinition` SO schemas | Now | Schemas compile; Deep Void asset exists (unwired) |
| 3 | `Veratus/Afterburn/Audit Performance Budget` editor script | Now | Asserts §5 budgets + §1.3 palette roles on any open scene |
| 4 | Greybox materials exactly per PortSpec §10 | Now (U2 uses these) | Side-by-side match vs prototype capture |
| 5 | Parity-mode toggle scaffold (speed-feel + art switches, default ON=bare) | Now | U2 capture runs provably bare |
| 6 | TrackRibbon shader + chunked ribbon bake | **U2 pass → U3** | §2.1 criteria; greybox toggle intact |
| 7 | Wall kit + scrape flash | U3 | §2.2 criteria |
| 8 | SDF gates + LightGap + HeavyWall shatter | U3 | §2.3 criteria; Div #3 reset honored |
| 9 | Skybox/starfield/backdrop + fog wiring | U3 | §2.4–2.5 criteria |
| 10 | Deep Void theme wired to Arena01 | U3 | Theme swap restyles with zero code edits |
| 11 | 3 hull meshes + uber-material + hologram variant | U3 | §3.1 criteria; 100 u silhouette test |
| 12 | Thruster tier system + pooled trails | U3 | §3.2 criteria; frame-true cut test |
| 13 | Combat/ability VFX suite (fire, shield, spinout, EMP, Phase, Siphon, Decoy) | U3→U4 | §3.3 criteria; telegraph timings exact |
| 14 | Bounty pillar/crown + Intel edge indicator + ghost nameplates | U4 | Intel trailer-only verified; header cosmetics render |
| 15 | Boost speed-feel package (FOV 62→68/0.25 s, lines, shake) | Built U3+, **enabled U5** behind parity flag | Flag off at U2/U3 captures; comfort settings exposed |
| 16 | HUD.prefab (energy-bar hero + full §4.2) | **U5** | Fill == `Ratio` same-frame audit; layout law; safe area |
| 17 | Touch layout: left zone + energy-action wheel + ability button | U5 | Input-layer exclusion edit-mode test passes |
| 18 | Component library + Boot/MainMenu/Loadout/Summary/Settings screens | U5 | Screens composed from library prefabs only; Summary = 3 hooks exactly |
| 19 | Motion pass (PrimeTween table) + icon set | U5 | §4.6 table; no tween on primary fill; §4.5 icon criteria |
| 20 | Store + Season screens (scaffolding, cosmetics-only, no ads) | **U6** | SKU categories render; no hard currency; validator (#21) wired |
| 21 | Cosmetic param block + reactive liveries + hue-distance validator | U6 | §3.4 criteria; validator fails violating test asset |
| 22 | Game Center panels + leaderboard rows with plates/badges | **U7** | 7 boards listed; rows carry cosmetic surface |
| 23 | App icon + store screenshots (SIGNAL VOID language) | U7 | Icon legible at 60 px; no out-of-role gold/red |
| 24 | Arena02 Redline Canyon: greybox → Ember Belt kit dressing | **Fast-follow** | Greybox gate first; §2.6 row criteria; budgets §5 hold |
| 25 | Arena03 Anchor Station: greybox → Reactor Violet + minimap elevation tint | Fast-follow | Over/under readable on minimap; §2.6 row criteria |

---

## 8. Footer

Companion design rationale: **`Afterburn_DesignReview.md`** (the 4-lens panel this spec's direction, budgets, and arena ladder are drawn from).

> **Greybox-first discipline:** the greybox is not a placeholder to escape — it is the parity instrument that proves the loop survived the port. Nothing in §2–§4 exists on screen until U2 signs off, and the greybox materials remain in the project forever as the regression fixture. Light is information; ship nothing that glows without meaning.

*Spec written 2026-07-09 · Veratus Games · Project ALTERNATE · slot 7.*
