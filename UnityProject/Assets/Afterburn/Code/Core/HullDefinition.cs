using UnityEngine;

namespace Afterburn.Core
{
    /// <summary>
    /// One of the three hull sidegrades (BUILD §5.1). Values are frozen from the prototype and
    /// seeded by <c>Veratus/Afterburn/Create or Update SOs</c> — never hand-type tuning here.
    /// Sidegrades only: never expose a straight "+X% power" upgrade.
    /// </summary>
    [CreateAssetMenu(menuName = "Afterburn/Hull Definition", fileName = "Hull")]
    public sealed class HullDefinition : ScriptableObject
    {
        public string displayName = string.Empty;

        [Tooltip("Base energy pool before GameTuning.energyMaxScale (Light 80 / Medium 100 / Heavy 130).")]
        public float maxEnergy = 100f;

        [Tooltip("Base regen per second before GameTuning.regenScale (Light 11 / Medium 8 / Heavy 5).")]
        public float regenPerSec = 8f;

        [Tooltip("Multiplier on GameTuning.shipFeel.baseTopSpeed (Light 1.15 / Medium 1.00 / Heavy 0.88).")]
        public float topSpeedMult = 1f;

        [Tooltip("Relative mass (Light 0.8 / Medium 1.0 / Heavy 1.4).")]
        public float mass = 1f;

        [Tooltip("Collision radius in track units (Light 1.6 / Medium 2.0 / Heavy 2.6).")]
        public float collisionRadius = 2f;

        [Tooltip("Which spec-gated shortcut this hull can use (Light→LightGap, Medium→None, Heavy→HeavyWall).")]
        public GateAccess gateAccess = GateAccess.None;

        [Tooltip("Hull tint (Light #37D0FF / Medium #9D7BFF / Heavy #FF8A3C).")]
        public Color tintColor = Color.white;

        [Tooltip("Short loadout-card tag, e.g. 'Fits narrow gaps'.")]
        public string tag = string.Empty;

        [TextArea]
        [Tooltip("Loadout-card sidegrade copy shown in the hull picker (BUILD §8).")]
        public string description = string.Empty;

        [Tooltip("D13 hybrid: optional PBR ship prefab (StarSparrow/Hi-Rez). Null = greybox cone. " +
                 "The View auto-fits it to sim scale (~5 u long, +z forward).")]
        public GameObject? shipPrefab;

        [Tooltip("Visual presence multiplier for the prefab path (sim length 5 u × this). " +
                 "Purely cosmetic — collision radius and camera framing use the sim, not the mesh. " +
                 "Sleek hulls read small at 1.0; ~1.4 restores the greybox cone's screen presence.")]
        public float shipVisualScale = 1.4f;
    }
}
