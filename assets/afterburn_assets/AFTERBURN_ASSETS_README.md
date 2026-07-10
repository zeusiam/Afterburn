# Afterburn — Asset Pack
**Veratus Games · Project ALTERNATE · A7**

Unity-ready asset bundle for the Claude Code build agent.

## Layout
```
afterburn_manifest.json   — master index + frozen tuning
icons/    svg/ + png/      — 28 HUD & systems icons
characters/ pilot|hull|enemy — 12 emblems (svg) + specs (json)
audio/    music|sfx|weapon  — 21 WAV cues (16-bit PCM)
environments/ race|battle    — 40 scene manifests + keyart PNG
afterburn_materials.json  — URP palette + material library
```
## Import
1. Read `afterburn_manifest.json` first — it indexes every file.
2. Icons: Unity Vector Graphics (SVG) or Sprite (PNG).
3. Audio: AudioClip; set `music/` clips to Loop.
4. Environments: build each scene from its manifest (biome, palette, fog, lighting, hazards, props, scale). keyart = mood/skybox reference.
5. Materials: seed via `Veratus/Afterburn/Create or Update SOs`.

## The one rule
EnergyCore is the sole spend authority. Boost / Fire / Shield are mutually exclusive; regen only on coast. Every asset serves that loop.
