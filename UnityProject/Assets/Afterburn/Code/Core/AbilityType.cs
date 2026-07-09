namespace Afterburn.Core
{
    /// <summary>
    /// Pilot ability dispatch key (BUILD §5.2 / §7.4). One ability per pilot, cooldown-gated,
    /// zero energy cost. Upgrades reduce cooldown only — never damage, never speed.
    /// </summary>
    public enum AbilityType
    {
        /// <summary>Vex — drain <c>abilityParam</c> (30) energy from all racers within <c>abilityRadius</c> (70).</summary>
        EmpPulse = 0,

        /// <summary>Sora — intangible for <c>abilityParam</c> (1.2) seconds: skips wall and hit collision.</summary>
        PhaseShift = 1,

        /// <summary>Kade — arm the next landed hit to steal <c>abilityParam</c> (25) energy.</summary>
        Siphon = 2,

        /// <summary>Nyx — spawn a decoy that draws AI fire for <c>abilityParam</c> (3.0) seconds.</summary>
        Decoy = 3,
    }
}
