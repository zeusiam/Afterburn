using System;
using UnityEngine;

namespace Afterburn.Core
{
    /// <summary>
    /// The frozen prototype tuning (BUILD §5.3) plus the ship-feel reference constants (BUILD §6).
    /// The loop passed the P2 kill gate *at these numbers* — reproduce the ratios exactly.
    /// Field initializers ARE the frozen defaults; the seeder creates the asset with them and
    /// never overwrites hand-edited values afterwards.
    ///
    /// The dev tuning overlay (U5) writes to a runtime copy of this asset, never the asset itself.
    /// </summary>
    [CreateAssetMenu(menuName = "Afterburn/Game Tuning", fileName = "GameTuning")]
    public sealed class GameTuning : ScriptableObject
    {
        [Header("Energy (BUILD §5.3)")]
        [Tooltip("Global × on each hull's maxEnergy. 100 = ×1.0.")]
        public float energyMaxScale = 100f;

        [Tooltip("Global × on each hull's regenPerSec. Baseline 8 = ×1.0.")]
        public float regenScale = 8f;

        [Header("Boost")]
        [Tooltip("Energy/s while boosting.")]
        public float boostDrainPerSec = 25f;

        [Tooltip("Top-speed multiplier while boosting.")]
        public float boostSpeedMult = 1.40f;

        [Header("Fire")]
        [Tooltip("Energy per shot.")]
        public float fireCost = 20f;

        [Tooltip("Multiply current speed once on fire.")]
        public float fireSpeedDip = 0.90f;

        [Header("Shield")]
        [Tooltip("Energy/s while shielding.")]
        public float shieldDrainPerSec = 15f;

        [Tooltip("Incoming damage × while shielding.")]
        public float shieldDamageMult = 0.40f;

        [Tooltip("Top-speed × while shielding.")]
        public float shieldSpeedCap = 0.80f;

        [Header("Bounty / abilities / race")]
        [Tooltip("Reward × for hitting the marked leader.")]
        public float bountyRewardMult = 2.0f;

        [Tooltip("Global × on all pilot cooldowns. Upgrades shrink PilotDefinition.cooldownSec only.")]
        public float abilityCooldownScale = 1.0f;

        [Tooltip("Laps per race.")]
        public int raceLaps = 3;

        [Header("Ship feel (BUILD §6 — prototype-felt values, reproduce ratios exactly)")]
        public ShipFeel shipFeel = new ShipFeel();
    }

    /// <summary>
    /// The prototype's felt handling constants (BUILD §6). Unit note: the prototype track is
    /// ~1823 u long (lap ≈ 29 s at cruise). If a Unity rescale is ever wanted, apply ONE scale
    /// factor to all distance/speed/accel constants together via <see cref="worldScale"/>;
    /// energy, time and multiplier values are unit-less and stay verbatim.
    /// </summary>
    [Serializable]
    public sealed class ShipFeel
    {
        [Tooltip("Single scale factor applied to all distance/speed/accel constants together (BUILD §6 unit note). 1 = prototype units.")]
        public float worldScale = 1f;

        [Tooltip("u/s, × hull topSpeedMult. Medium cruises 62; boost → ~86.8.")]
        public float baseTopSpeed = 62f;

        [Tooltip("u/s².")]
        public float thrustAccel = 55f;

        [Tooltip("×speed per second. Terminal ≈ accel/drag.")]
        public float dragPerSec = 0.6f;

        [Tooltip("u/s².")]
        public float brakeDecel = 80f;

        [Tooltip("rad/s × bite.")]
        public float turnRate = 2.4f;

        [Tooltip("bite = clamp01(speed / turnBiteSpeedDivisor + turnBiteFloor) — less bite at crawl.")]
        public float turnBiteSpeedDivisor = 25f;

        [Tooltip("See turnBiteSpeedDivisor.")]
        public float turnBiteFloor = 0.2f;

        [Tooltip("×speed on wall contact.")]
        public float wallScrapeSpeedMult = 0.92f;

        [Tooltip("Slide-along-wall heading nudge toward tangent on contact.")]
        public float wallSlideHeadingNudge = 0.18f;

        [Tooltip("s — min gap between shots. Held fire auto-repeats at this cadence.")]
        public float fireTapCooldown = 0.28f;

        [Tooltip("Energy applied to target's pool.")]
        public float projectileDamage = 25f;

        [Tooltip("u/s.")]
        public float projectileSpeed = 170f;

        [Tooltip("+ this × shooter forward speed.")]
        public float projectileInheritFactor = 0.4f;

        [Tooltip("s (prototype bullet ttl).")]
        public float projectileLifetime = 1.6f;

        [Tooltip("u ahead of shooter at spawn (prototype).")]
        public float projectileSpawnAhead = 3f;

        [Tooltip("Hit when distance < hull.collisionRadius + this (prototype).")]
        public float projectileHitPad = 1.6f;

        [Tooltip("s — on being hit.")]
        public float spinoutDuration = 0.7f;

        [Tooltip("cap × while spun out (stacks multiplicatively with boost/shield cap — PortSpec div #12).")]
        public float spinoutSpeedCapMult = 0.55f;

        [Tooltip("Energy paid to shooter per hit; × bountyRewardMult when target is the leader.")]
        public float bountyBaseReward = 8f;

        [Tooltip("Ghost skill band lower bound (× top). Ghosts never scrape walls; this keeps them beatable.")]
        public float aiSkillBandMin = 0.80f;

        [Tooltip("Ghost skill band upper bound (× top).")]
        public float aiSkillBandMax = 0.90f;

        [Tooltip("u — AI fires at the bounty leader within this.")]
        public float aiFireRange = 55f;
    }
}
