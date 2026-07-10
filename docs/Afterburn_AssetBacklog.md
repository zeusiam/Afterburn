# Afterburn — Visual Asset Backlog (design-session work orders)
## Veratus Games · Project ALTERNATE · letter **A**, slot 7

> **Purpose:** the execution backlog for Claude design/build sessions. Each work package (WP) is
> sized for one session, independently shippable, and carries its own acceptance criteria.
> The governing law is `Afterburn_UIEnvSpec.md` (SIGNAL VOID: light is information; palette
> roles LOCKED — gold = leader only, red = damage only; frame-true state visuals; budgets §5).
> Ships are SOLVED (StarSparrow now, Hi-Rez later) — this backlog is everything else.
>
> **Session protocol:** read UIEnvSpec §1 (direction + tokens) first, then the WP. Build against
> `Assets/Afterburn/Code/` conventions (namespaces `Afterburn.View`/`Afterburn.UI`, tokens from
> `AfterburnPalette/Type/Motion`, materials as assets or GreyboxMaterials-style factories, meshes
> procedural, everything pooled). Verify: batchmode compile + `Veratus/Afterburn` menus + Play.
> Never touch `Afterburn.Core` — visuals READ sim state, never write it.

**Status legend:** ⬜ todo · 🟨 functional-greybox (works, no art) · ✅ done

---

## Priority ladder (biggest visual delta per effort, in order)

| # | Package | Status |
|---|---|---|
| WP-01 | TrackRibbon speed-reactive shader | ⬜ |
| WP-02 | Thruster tiers + boost trails (prefab-ship anchored) | ⬜ |
| WP-03 | Combat VFX: fire / hit / spinout / contact sparks | ⬜ |
| WP-04 | Skybox + starfield + celestial backdrop | 🟨 quad starfield |
| WP-05 | Gates: SDF start ring · LightGap beam · HeavyWall shatter | 🟨 torus + primitives |
| WP-06 | Speed-feel package: FOV kick, speed lines, shake | ⬜ (U2 signed off — toggle may enable) |
| WP-07 | Ability VFX ×4 + shield bubble + bounty pillar | ⬜ |
| WP-08 | Icon set + display font | ⬜ (LiberationSans fallback live) |
| WP-09 | UI chrome: cards/buttons/bars via rounded-SDF shader | 🟨 flat rects |
| WP-10 | Screen polish: Lineup · Podium · Summary · Boot/Menu backdrop | 🟨 functional |
| WP-11 | Wall kit + scrape flash | 🟨 plain ribbon |
| WP-12 | ThemeDefinition SO + Deep Void wiring | ⬜ |
| — | Audio (separate track, see foot) | ⬜ |

---

## WP-01 — TrackRibbon shader (the environment hero)

**Goal:** the road itself communicates speed and intent. One ShaderGraph (or HLSL) on the ribbon
mesh; `TrackView.BuildRibbon` supplies it UVs (add: UV.x = lateral −1..1, UV.y = arc-length
metres — modify the mesh builder to write UV2).
**Spec (UIEnvSpec §2.1):** (a) emissive cyan edge rails at lateral ±0.97 (≈±16.5 u), brightness
scales with a global `_PlayerSpeed` float (View sets it from `RaceRunner.Race.Player.Speed`);
(b) centre-guide chevrons scrolling AGAINST travel at speed-proportional rate; (c) lateral tick
lines every 20 u (stroboscopic speed sense); (d) shortcut approach tint — ribbon lerps toward
cyan (LightGap) / orange (HeavyWall) starting 120 u before each zone (bake zone fractions into
UV2.y comparisons or a small lookup); (e) vertex-color darkening toward edges. Emission >1 HDR
so future bloom picks rails only.
**Keep:** the greybox material toggle (parity diffs). Shader never displaces vertices.
**Acceptance:** reads at 86.8 u/s; chevron frequency readable at full boost; ≤2 draw calls for
the ribbon; greybox toggle reproduces PortSpec §10 exactly.

## WP-02 — Thruster tiers + boost trails

**Goal:** energy spend is visible from every camera. Works on BOTH ship paths (greybox cone and
StarSparrow prefabs — anchor via renderer-bounds rear-center, `ShipGreybox` exposes the socket).
**Spec (UIEnvSpec §3.2):** three tiers read from Core every frame: T0 coast (small core glow),
T1 thrust (×1.3), T2 boost (×1.8 + 25 u pooled ribbon-trail, gradient #FF7A3C→#FFE14D).
Trail = tapered ribbon mesh or TrailRenderer with width curve, HDR emissive, pooled.
**THE LAW:** the flame cuts the EXACT frame `EnergyCore` self-cancels at 0 (dry tank) —
a lingering flame misinforms Intel reads. Ghost thrusters: 1.6/0.9 scale (prototype).
**Cosmetic hook:** trail gradient + width curve as parameters (future `trail.*` SKUs).
**Acceptance:** boost→cut transition frame-true (test against EnergyCore edit-mode scenario);
zero allocation after warm-up; visible at 150 u behind.

## WP-03 — Combat VFX suite (fire / hit / spinout / contact)

**Goal:** every hit reads instantly. All pooled via the studio `ObjectPool`, particles + shader
quads only.
**Spec (UIEnvSpec §3.3):** fire = 0.1 s muzzle quad at spawn point + tracer upgrade for the
bullet (core sphere + 8 u additive red streak, replaces plain sphere in `CombatView`); hit =
12-particle radial spark + 0.15 s white flash on victim + world-space floating "−25"
(`type-caption`, rises 2 u, fades 0.6 s); spinout = angular smoke puffs + hull emissive flicker
red 0.7 s; **D14 contact spark** = brief orange crunch burst + radial shockwave quad at the
contact point (`ShipContactSystem.OnShipContact` event carries positions); wall scrape = spark
stream along the contact arc while grinding (`ShipController.OnWallContact`).
**Acceptance:** all effects from pools; no VFX calls into Core; hit spark visible at 100 u;
contact spark distinct from weapon hit (orange vs red family).

## WP-04 — Skybox + starfield + celestial backdrop

**Goal:** replace the quad starfield with the real void. Pure shader math, zero textures.
**Spec (UIEnvSpec §2.4):** procedural skybox material — 3-stop vertical gradient (horizon glow
#0A1C3A → void #05070F), in-shader hash-noise starfield (two density layers), star streaks
elongating along velocity above 70 u/s (`_PlayerVelocity` vector uniform); 1–2 SDF celestial
discs as distant billboards (ringed planet for Deep Void — fixed landmarks for velocity
judgment); fog color matched to horizon stop (RenderSettings already 220→620).
**Acceptance:** zero textures; ≤3 draw calls; streaks engage exactly at the threshold; no
shimmer at rest; the existing `StarfieldView` quads retire.

## WP-05 — Gates: start ring, LightGap, HeavyWall

**Goal:** the track's landmarks become designed objects.
**Spec (UIEnvSpec §2.3):** start/finish = flat SDF ring quad (shader-drawn ring + rotating tick
marks, white/cyan emissive, cheaper than the torus mesh — keep radius 18, y 15, plane ⟂ travel);
LightGap = cyan pillar pair joined by a translucent light-beam quad + floor chevrons that key
cyan ONLY when the local player's hull is Light (`gateAccess` read — information, not power);
HeavyWall = slab pre-fractured into 12–16 procedural chunks; on `TrackSystem.OnWallBroken`,
pooled scripted scatter (PrimeTween arcs, no physics), settle + fade; resets with race
(re-show slab on scene reload — state already per-race).
**Acceptance:** ring reads at 200 u; hull-keyed chevrons flip with loadout; shatter ≤0.8 s,
chunks return to pool; `innerAllow` logic untouched.

## WP-06 — Speed-feel package (the U5 gate said yes)

**Goal:** boost feels like ignition. Built behind the parity toggle (default ON now that the
U2 side-by-side is signed off; Settings toggles persist — keys `ab.fovkick`, `ab.shake` exist).
**Spec (UIEnvSpec §3.3 row 1):** FOV 62→68 over 0.25 s on boost entry, mirror on exit
(`ChaseCamera` reads `Player.Boosting`); camera-attached additive speed-line tube spawning
>55 u/s, density scaling to 90; sub-pixel shake ≤0.05 u boost-only. Respect the Settings
toggles (comfort).
**Acceptance:** parity mode OFF reproduces the prototype-exact camera (the ParityTrace suite
must stay green — it tests Core, but the toggle must be provably bypassable); no handling drift.

## WP-07 — Ability VFX + shield + bounty pillar

**Goal:** abilities telegraph and counterplay reads (DesignReview U3 telegraph pass).
**Spec (UIEnvSpec §3.3):** EMP = 0.6 s expanding-ring windup telegraph THEN cyan torus shockwave
to exactly radius 70 (`PilotAbilitySystem.OnAbilityActivated`); Phase = screen-door dissolve to
0.4 opacity + violet rim (StarSparrow path: swap to `LitTransparent` variants or
MaterialPropertyBlock alpha), snap-back flash; Siphon armed = visible teal tether/glow on the
armed ship, mote stream victim→shooter on the landed steal; Decoy = the octahedron becomes a
glitching hologram (vertex-jitter shader + scanlines) — deliberate tell: no thruster flame;
shield = fresnel bubble + scrolling hex lattice in violet, intensity tied to drain, hit ripple
from impact point, vanishes the exact frame shielding ends; bounty = ~60 u gold light pillar
above the leader + HUD crown (gold ONLY here — the palette law).
**Acceptance:** every effect matches Core booleans same-frame; EMP ring diameter measurably 140 u;
Phase visual ends exactly at `IntangibleTimer` 0.

## WP-08 — Icon set + display font

**Goal:** the UI stops borrowing letters. Two deliverables:
(1) **Font:** pick a SIL-OFL geometric sans (Space Grotesk / Chakra Petch class — pick for a
technical-racing voice), generate TMP SDF atlases (Medium + Bold + tabular-numeral preset),
place under `Assets/Afterburn/Art/UI/Fonts/`, point `UIFactory.DefaultFont()` at it (keep the
fallback chain). Verify glyph coverage: −, ·, —, digits tabular.
(2) **Icons:** author as code-drawn SVG → import as one sprite atlas (≤2048²), 2-weight stroke
(4 px / 2.5 px at 96 px artboard), single-color, runtime-tinted via tokens. Set: hull ×3
(dart/arrow/hammerhead silhouettes), ability ×4 (pulse/phase/siphon/decoy), mode ×4, crown,
salvage, store, season, settings, Game Center, ghost, locked, rematch, speaker.
**Acceptance:** legible at 44 px; zero baked color; HUD/menu swap to icons where labels
currently carry the load (chips keep text+icon).

## WP-09 — UI chrome: the rounded-SDF panel shader

**Goal:** cards, buttons, bars stop being flat rects. ONE shader does all chrome: a UI shader
drawing rounded-rect SDF fill + 1–2 px stroke + optional underglow, parameterised (radius,
stroke, glow color) — applied via a small `Chrome` factory extension to `UIFactory` so every
existing screen upgrades by swapping the background constructor.
**Spec:** surface tokens for fills, `--stroke` for borders, mode/tint colors for underglows;
press states scale 0.96 (`AfterburnMotion.ButtonPress`); selected cards get the 4 px underglow
strip in tint. Energy bar gets a chrome pass: inner bevel rim, brighter tick marks, dry-out rim
flash. No purchasable pixel implications — this is base UI.
**Acceptance:** zero new textures (SDF math); all screens visually consistent; batch count for
HUD canvas unchanged (±2).

## WP-10 — Screen polish: Lineup · Podium · Summary · Boot/Menu

**Goal:** the billboard screens earn their mandatory status.
**Spec:** Lineup — racer name cards slide in staggered (`ScreenIn` motion), each with hull icon,
pilot name, tint chip; camera sweep keeps its skip. Podium — top-3 ships repositioned onto a
simple three-step dais (spawn placement, no physics), place cards with times, gold treatment for
P1 only. Summary — panel gets chrome (WP-09), delta line animates its count, next-goal chip
pulses once. Boot/Menu — slow parallax starfield backdrop (reuse WP-04 shader on a quad),
title gets a one-time cyan sweep. MainMenu ship podium: render the SELECTED hull's actual
prefab rotating slowly on the left half (RenderTexture or world-space camera layer).
**Acceptance:** lineup ≤8 s skippable ≥3 s; every equipped visual visible in lineup + podium;
Summary keeps EXACTLY three hooks (gate law).

## WP-11 — Wall kit + scrape flash

**Spec (UIEnvSpec §2.2):** wall ribbon gets fresnel top-edge glow shader; scrape-reactive
localized emissive pulse driven by `ShipController.OnWallContact` (arc position + falloff
uniforms, pooled per ship) — the ×0.92 + 6/s cost must be FELT. Walls dissolve into fog.
**Acceptance:** pulse within 1 frame of contact at the right arc position; no collision change.

## WP-12 — ThemeDefinition plumbing

**Spec (UIEnvSpec §2.6):** `ThemeDefinition` SO (skybox stops, fog color/range, track palette,
wall tint, backdrop prop set, starfield density/streak threshold); seeder creates `Theme_DeepVoid`;
`TrackView`/skybox/fog read the theme instead of hard-coded hexes. Arena02/03 themes become
data files later.
**Acceptance:** swapping the theme asset restyles the scene with zero code edits.

---

## Audio (separate track — not a Claude-design deliverable)

Claude sessions can WIRE audio, not author waveforms. Source from asset-store SFX packs
(or a commissioned pack) to this cue list: engine loop (pitch by speed/boost), boost ignition +
sustain, fire shot, hit impact, shield hum + hit ripple, dry-tank warning, EMP sting + windup,
phase in/out, siphon steal, decoy deploy, wall scrape loop, ship contact crunch, countdown
beeps + GO, lap chime, finish sting, bounty gained/lost, UI tap/confirm/denial, menu music bed,
race music bed (or engine-forward, no music — a taste call for Seni). Mixing: engine ducks
under stings; toggle in Settings. **Recommend:** pick packs AFTER WP-01–06 land so the audio
matches the visual energy.

---

*Written 2026-07-10. Governing law: `Afterburn_UIEnvSpec.md` · design rationale:
`Afterburn_DesignReview.md`. One WP per design session; verify with the 69-test suite +
batchmode compile before commit; the parity suite must stay green through every package.*
