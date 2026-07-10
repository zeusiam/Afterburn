# Afterburn — Sweep Log
## Veratus Games · Project ALTERNATE · A7

> Every code sweep, audit, and multi-agent review already performed — scope, method,
> findings, outcome. **Check this BEFORE starting any sweep** so work is never repeated;
> append after every sweep. A sweep with no entry here didn't happen.

| # | Date | Sweep | Scope & method | Outcome |
|---|---|---|---|---|
| SW-1 | 07-09 | **Sibling-project convention sweep** | Explore agent, very thorough, across 4 Veratus Unity projects | Studio standards identified: ObjectPool/SafeArea/seeder pattern, PrimeTween tarball, asmdef styles, portrait canvas convention. Copied into U1 |
| SW-2 | 07-09 | **Prototype extraction** | Deep agent read of Afterburn_Prototype.html, every constant | `Afterburn_PortSpec.md` + 16 divergences. The port bible — do not re-extract |
| SW-3 | 07-09 | **U1 adversarial review** | Workflow: 3 reviewers (frozen values / Unity API / three.js parity) + per-finding refuters, 10 agents | 7 findings, 6 confirmed, all fixed (gate pose, slab tilt, starfield, test dead-code, shader-strip note, cone ruling). Covered: all U1 files |
| SW-4 | 07-09 | **Design panel** | Workflow: 4 lenses (track-3D / combat / monetisation / art-tech) | 30+ recommendations → DesignReview matrices; D6 track-frame directive originated here |
| SW-5 | 07-09 | **Economy adversarial audit** | Workflow: 3 auditors (P2W leak-hunter / pacing math / revenue realist) vs the §6 catalog | 2 catalog breaks + leaks fixed; §6 rewritten as canon. Do not re-audit until the catalog changes |
| SW-6 | 07-09/10 | **Parity instrument calibration** | Node harness runs REAL prototype JS → 3600-frame trace; iterative budget shaping | Final instrument: pre-contact exact, energy ≤0.55, behavioural counts exact, containment invariant. Baselines recorded in the test |
| SW-7 | 07-10 | **Full-suite verification** | Headless batchmode, editor closed | 69/69 green (pre-D15 state) |
| SW-8 | 07-10 | **Vendor collider sweep** | All vendor-prefab spawn paths | `ViewPrefabs.InstantiateWithoutColliders` — pre-activation strip everywhere (gates, scenic, ships) |
| SW-9 | 07-10 | **Vendor pack intake audits** | StarSparrow, then USC/Weapons/WarpGates: shaders, textures, prefab structure, GitHub-limit scan | All URP-native (Colorize ShaderGraphs); 108 MB file found → vendor gitignore policy; texture budgets flagged (R-11) |

## Not yet swept (candidates — check REVISIT before scheduling)
- UI assembly (HudView/RaceFlow/screens) has had NO adversarial review pass (built S9, playtested only)
- D14/D15/D15.1 Core additions reviewed by tests only — no multi-agent review pass
- Performance sweep (draw calls / tris vs the §5 contract) — never run; meaningful after WP-01/02
- Store-catalog lint (economy invariants as tests) — waits for U6 (R-22)

**Sweep protocol:** state scope → check this log + REVISIT → run (prefer adversarial
verification for findings) → file bugs in BUGS.md, deferrals in REVISIT.md → append entry
here with outcome + commit.
