using System;
using UnityEngine;

namespace Afterburn.Core
{
    /// <summary>
    /// Runtime track query system (BUILD §7.5 Core half): the dense frame cache, the
    /// progress-coherent windowed nearest-sample query (D6 — prerequisite for self-intersecting
    /// tracks, behaviour-identical to the prototype's global argmin on flat Arena01), the
    /// spec-gated shortcut allowance, and the HeavyWall break state.
    /// Ruling #3 (DesignReview §5): break state is per-race — <see cref="ResetRaceState"/> runs
    /// at every race start; within a race the broken wall opens for ALL hulls (prototype canon).
    /// </summary>
    public sealed class TrackSystem
    {
        /// <summary>±window for the coherent nearest query. At 86.8 u/s a tick moves &lt; 1 sample.</summary>
        public const int NearestWindow = 40;

        private readonly TrackSample[] _samples;
        private readonly TrackDefinition _def;
        private readonly float _length;

        // Shortcut zones baked to sample-index ranges (prototype: floor(N × fraction)).
        private readonly ZoneState[] _zones;

        private sealed class ZoneState
        {
            public GateAccess Access;
            public int From;
            public int To;
            public float Extra;
            public bool Broken;
        }

        /// <summary>Raised once when a Heavy smashes the wall (zone index in track.shortcuts). View hook.</summary>
        public event Action<int>? OnWallBroken;

        public TrackSystem(TrackDefinition def)
        {
            _def = def != null ? def : throw new ArgumentNullException(nameof(def));
            _samples = TrackSampler.BuildSamples(def);
            Spline = TrackSampler.BuildSpline(def);
            _length = Spline.Length;

            int n = _samples.Length;
            var shortcuts = def.shortcuts ?? Array.Empty<ShortcutZone>();
            _zones = new ZoneState[shortcuts.Length];
            for (int z = 0; z < shortcuts.Length; z++)
            {
                _zones[z] = new ZoneState
                {
                    Access = shortcuts[z].access,
                    From = Mathf.FloorToInt(n * shortcuts[z].fromFraction),
                    To = Mathf.FloorToInt(n * shortcuts[z].toFraction),
                    Extra = shortcuts[z].extraInnerAllowance,
                    Broken = false,
                };
            }
        }

        public int SampleCount => _samples.Length;
        public float Length => _length;
        public float HalfWidth => _def.halfWidth;
        public TrackSample this[int i] => _samples[Mod(i, _samples.Length)];

        /// <summary>The continuous centreline — ghosts pose via getPointAt(t) (prototype parity).</summary>
        public CatmullRomSpline Spline { get; }

        /// <summary>Ruling #3: all wall-break state resets at race start (state leakage across
        /// "one more run" made recorded ghosts and leaderboard times incomparable).</summary>
        public void ResetRaceState()
        {
            foreach (ZoneState z in _zones) z.Broken = false;
        }

        public bool IsZoneBroken(int zoneIndex) => _zones[zoneIndex].Broken;

        /// <summary>
        /// Progress-coherent nearest sample: searches ±<see cref="NearestWindow"/> around the
        /// caller's last index; if the best sits on the window edge (window escape after a huge
        /// displacement) it rescans globally. Pass -1 to force a global scan (spawn/reset).
        /// </summary>
        public int Nearest(Vector3 pos, int lastIndex)
        {
            if (lastIndex < 0) return NearestGlobal(pos);

            int n = _samples.Length;
            int best = lastIndex;
            float bestD = float.MaxValue;
            for (int o = -NearestWindow; o <= NearestWindow; o++)
            {
                int i = Mod(lastIndex + o, n);
                float d = SqrDistXZ(pos, _samples[i].Pos);
                if (d < bestD) { bestD = d; best = i; }
            }
            // Detect-and-rescan fallback: a best on the window rim means the true nearest may lie outside.
            int rel = Mod(best - lastIndex + NearestWindow, n);
            if (rel == 0 || rel == NearestWindow * 2) return NearestGlobal(pos);
            return best;
        }

        /// <summary>Prototype Track.nearest: brute-force argmin on squared XZ distance.</summary>
        public int NearestGlobal(Vector3 pos)
        {
            int best = 0;
            float bestD = float.MaxValue;
            for (int i = 0; i < _samples.Length; i++)
            {
                float d = SqrDistXZ(pos, _samples[i].Pos);
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        /// <summary>
        /// Prototype innerAllow: extra inner wall allowance at index for a hull. Wrap-aware range;
        /// LightGap needs the Light hull; HeavyWall needs Heavy OR an already-broken wall (any hull,
        /// within this race — ruling #3).
        /// </summary>
        public float InnerAllow(int idx, GateAccess hullAccess)
        {
            float a = 0f;
            foreach (ZoneState z in _zones)
            {
                bool inRange = z.From <= z.To
                    ? idx >= z.From && idx <= z.To
                    : idx >= z.From || idx <= z.To;
                if (!inRange) continue;
                if (z.Access == GateAccess.LightGap && hullAccess == GateAccess.LightGap)
                    a = Mathf.Max(a, z.Extra);
                if (z.Access == GateAccess.HeavyWall && (hullAccess == GateAccess.HeavyWall || z.Broken))
                    a = Mathf.Max(a, z.Extra);
            }
            return a;
        }

        /// <summary>
        /// Prototype breakHeavy: called when a Heavy presses the outer-of-inner-wall limit inside
        /// the heavy zone. Plain (non-wrap) range test — prototype parity. Returns true only on
        /// the breaking hit.
        /// </summary>
        public bool TryBreakHeavy(int idx)
        {
            for (int z = 0; z < _zones.Length; z++)
            {
                ZoneState zone = _zones[z];
                if (zone.Access != GateAccess.HeavyWall || zone.Broken) continue;
                if (idx >= zone.From && idx <= zone.To)
                {
                    zone.Broken = true;
                    OnWallBroken?.Invoke(z);
                    return true;
                }
            }
            return false;
        }

        private static float SqrDistXZ(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        private static int Mod(int a, int n) => ((a % n) + n) % n;
    }
}
