namespace Afterburn.Core
{
    /// <summary>
    /// Spec-gated shortcut access (BUILD §5.1 / §7.5). A hull's <see cref="HullDefinition.gateAccess"/>
    /// must match a shortcut zone's type for the inner wall allowance to open.
    /// These are sidegrades — never a straight power upgrade.
    /// </summary>
    public enum GateAccess
    {
        None = 0,

        /// <summary>Narrow pillar gap only the Light hull threads (prototype zone 0.20–0.24, +20 allowance).</summary>
        LightGap = 1,

        /// <summary>Breakable slab only the Heavy hull smashes (prototype zone 0.61–0.66, +22 allowance).</summary>
        HeavyWall = 2,
    }
}
