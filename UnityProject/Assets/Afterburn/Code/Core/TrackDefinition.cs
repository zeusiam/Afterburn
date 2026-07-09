using System;
using UnityEngine;

namespace Afterburn.Core
{
    /// <summary>
    /// A track (BUILD §7.5): closed Catmull-Rom centreline, half-width, checkpoints and
    /// spec-gated shortcut zones. Arena01 reproduces the prototype's star-convex loop —
    /// control points are generated from <see cref="baseRadius"/> × <see cref="radiusShape"/>
    /// by the seeder and stored explicitly in <see cref="controlPoints"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Afterburn/Track Definition", fileName = "Track")]
    public sealed class TrackDefinition : ScriptableObject
    {
        public string displayName = string.Empty;

        [Header("Centreline spline (closed uniform Catmull-Rom)")]
        [Tooltip("Explicit control points (y = 0). Runtime source of truth for the spline.")]
        public Vector3[] controlPoints = Array.Empty<Vector3>();

        [Tooltip("Catmull-Rom tension (prototype: uniform 'catmullrom' at 0.5 — NOT centripetal).")]
        public float catmullTension = 0.5f;

        [Tooltip("Dense sample count for collision + rendering (prototype N = 700).")]
        public int sampleCount = 700;

        [Header("Star-convex generator inputs (authoring aid — seeder bakes controlPoints from these)")]
        [Tooltip("Base radius R0 (prototype 300).")]
        public float baseRadius = 300f;

        [Tooltip("Per-point radius multipliers around the circle. Dips = corners, peaks = straights.")]
        public float[] radiusShape = Array.Empty<float>();

        [Header("Cross-section")]
        [Tooltip("Track half-width (prototype 17).")]
        public float halfWidth = 17f;

        [Tooltip("Greybox wall height (prototype 3.2). Walls sit at ±(halfWidth + 0.4).")]
        public float wallHeight = 3.2f;

        [Header("Progress")]
        [Tooltip("Checkpoint track fractions [0..1). Prototype defines none (PortSpec div #2) — reserved per BUILD §7.5.")]
        public float[] checkpointFractions = Array.Empty<float>();

        [Header("Spec-gated shortcuts (BUILD §7.5)")]
        public ShortcutZone[] shortcuts = Array.Empty<ShortcutZone>();
    }

    /// <summary>
    /// A spec-gated shortcut zone. In range, the inner wall allowance opens only if the hull's
    /// <see cref="HullDefinition.gateAccess"/> matches (Heavy smashes the HeavyWall slab on contact).
    /// </summary>
    [Serializable]
    public sealed class ShortcutZone
    {
        [Tooltip("LightGap or HeavyWall.")]
        public GateAccess access = GateAccess.LightGap;

        [Tooltip("Zone start as a track fraction [0..1). Prototype: light 0.20, heavy 0.61.")]
        public float fromFraction;

        [Tooltip("Zone end as a track fraction [0..1). Prototype: light 0.24, heavy 0.66.")]
        public float toFraction;

        [Tooltip("Extra inner wall allowance while qualified in-zone (prototype: light +20, heavy +22).")]
        public float extraInnerAllowance;

        [Tooltip("+1 = inside (+normal), -1 = outside. Prototype: both +1.")]
        public int side = 1;
    }
}
