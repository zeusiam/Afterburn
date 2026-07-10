using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afterburn.Core
{
    /// <summary>
    /// D15 (owner ruling, 2026-07-10): gates as TRACK FEATURES — fixed, deterministic, identical
    /// for every racer every lap. NOT pickups (no randomness, nothing consumed, nothing granted
    /// to a pool): SpeedBoost/WarpSurge give SPEED only (never energy — the slipstream precedent);
    /// Blocker only DRAINS (D14 hazard family). Ranked-legal by determinism; recorded ghosts stay
    /// replayable because features live in TrackDefinition.
    /// </summary>
    public enum GateFeatureType
    {
        /// <summary>Fly-through speed surge — cap raise + impulse, short.</summary>
        SpeedBoost = 0,

        /// <summary>The rare, violent version — "warp speed". Speed only, bigger, longer.</summary>
        WarpSurge = 1,

        /// <summary>A harmful ring parked on the fast line — pass through it and pay energy + speed.</summary>
        Blocker = 2,
    }

    /// <summary>One gate feature on the track (authored in TrackDefinition, baked by TrackSystem).</summary>
    [Serializable]
    public sealed class GateFeature
    {
        public GateFeatureType type = GateFeatureType.SpeedBoost;

        [Tooltip("Track fraction [0..1) where the gate stands.")]
        public float fraction;

        [Tooltip("Lateral centre of the gate opening (0 = centreline, + = inside).")]
        public float lateralOffset;

        [Tooltip("Half-width of the trigger opening (u). 8 ≈ half the track; blockers are narrower.")]
        public float halfSpan = 8f;

        [Tooltip("SpeedBoost/WarpSurge: cap multiplier while the surge lasts (speed only, never energy).")]
        public float surgeCapMult = 1.25f;

        [Tooltip("SpeedBoost/WarpSurge: one-time speed impulse on entry (clamped to the surged cap).")]
        public float surgeImpulse = 12f;

        [Tooltip("SpeedBoost/WarpSurge: surge duration in seconds.")]
        public float surgeDuration = 1.2f;

        [Tooltip("Blocker: energy drained on contact (D14 hazard family — drains only).")]
        public float blockerDamage = 15f;

        [Tooltip("Blocker: ×speed on contact.")]
        public float blockerSpeedMult = 0.85f;
    }

    /// <summary>
    /// Detects racers crossing gate features and applies effects. One trigger per gate pass —
    /// re-arms when the racer leaves the gate's index window. Synthetic ghosts are exempt in v1
    /// (rail-locked — they can't steer for or around gates; the skill band absorbs it; flagged
    /// for the U4 re-tune). Replayed ghosts run ShipController and get gates deterministically.
    /// </summary>
    public sealed class GateFeatureSystem
    {
        private sealed class Baked
        {
            public GateFeature Feature = null!;
            public int Index;                    // sample index of the gate plane
            public int Window;                   // ± samples counted as "at the gate"
        }

        private readonly TrackSystem _track;
        private readonly List<Baked> _gates = new();
        private readonly Dictionary<ShipController, bool[]> _inside = new();

        /// <summary>(racer, feature) — fired once per pass. HUD/VFX hook.</summary>
        public event Action<ShipController, GateFeature>? OnGateTriggered;

        public GateFeatureSystem(TrackSystem track, TrackDefinition def)
        {
            _track = track ?? throw new ArgumentNullException(nameof(track));
            int n = track.SampleCount;
            foreach (GateFeature f in def.gateFeatures ?? Array.Empty<GateFeature>())
            {
                _gates.Add(new Baked
                {
                    Feature = f,
                    Index = Mathf.FloorToInt(Mathf.Repeat(f.fraction, 1f) * n),
                    Window = 3,                   // ±3 samples ≈ ±7.8 u — generous at 60 Hz
                });
            }
        }

        public int GateCount => _gates.Count;

        public void Register(ShipController racer)
        {
            _inside[racer] = new bool[_gates.Count];
        }

        /// <summary>Per fixed tick, after the racer stepped.</summary>
        public void Tick(ShipController racer)
        {
            if (_gates.Count == 0 || !_inside.TryGetValue(racer, out bool[]? inside)) return;

            int n = _track.SampleCount;
            int at = racer.NearestIndex;

            for (int g = 0; g < _gates.Count; g++)
            {
                Baked gate = _gates[g];
                int delta = Mathf.Abs(at - gate.Index);
                bool inWindow = Mathf.Min(delta, n - delta) <= gate.Window;

                if (!inWindow)
                {
                    inside[g] = false;            // left the window — re-arm
                    continue;
                }
                if (inside[g]) continue;          // already triggered this pass
                inside[g] = true;

                // Lateral check: must pass through the opening.
                TrackSample s = _track[gate.Index];
                float lateral = Vector3.Dot(racer.Position - s.Pos, s.Nrm);
                if (Mathf.Abs(lateral - gate.Feature.lateralOffset) > gate.Feature.halfSpan) continue;

                Apply(racer, gate.Feature);
                OnGateTriggered?.Invoke(racer, gate.Feature);
            }
        }

        private static void Apply(ShipController racer, GateFeature f)
        {
            switch (f.type)
            {
                case GateFeatureType.SpeedBoost:
                case GateFeatureType.WarpSurge:
                    racer.ApplyGateSurge(f.surgeCapMult, f.surgeImpulse, f.surgeDuration);
                    break;

                case GateFeatureType.Blocker:
                    racer.ApplyHazard(f.blockerDamage, f.blockerSpeedMult);
                    break;
            }
        }
    }
}
