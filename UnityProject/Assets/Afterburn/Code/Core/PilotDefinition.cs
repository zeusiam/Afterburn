using UnityEngine;

namespace Afterburn.Core
{
    /// <summary>
    /// One of the four pilots (BUILD §5.2). Values are frozen from the prototype and seeded by
    /// <c>Veratus/Afterburn/Create or Update SOs</c>.
    ///
    /// MONETISATION GUARDRAIL: ability upgrades reduce <see cref="cooldownSec"/> only.
    /// Never damage, never speed.
    /// </summary>
    [CreateAssetMenu(menuName = "Afterburn/Pilot Definition", fileName = "Pilot")]
    public sealed class PilotDefinition : ScriptableObject
    {
        public string displayName = string.Empty;

        [Tooltip("Ability display name, e.g. 'EMP Pulse'.")]
        public string abilityName = string.Empty;

        [Tooltip("Base cooldown seconds before GameTuning.abilityCooldownScale (Vex 18 / Sora 15 / Kade 20 / Nyx 22).")]
        public float cooldownSec = 18f;

        public AbilityType abilityType = AbilityType.EmpPulse;

        [Tooltip("Primary ability magnitude: EMP energy drained (30) / Phase seconds (1.2) / Siphon energy stolen (25) / Decoy seconds (3.0).")]
        public float abilityParam;

        [Tooltip("Effect radius where applicable: EMP 70, otherwise 0.")]
        public float abilityRadius;

        [TextArea]
        [Tooltip("Loadout-card ability copy shown in the pilot picker (BUILD §8).")]
        public string description = string.Empty;
    }
}
