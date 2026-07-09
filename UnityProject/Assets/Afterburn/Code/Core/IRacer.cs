using UnityEngine;

namespace Afterburn.Core
{
    /// <summary>
    /// The combat-facing contract every racer satisfies — the player's <see cref="ShipController"/>
    /// and U4's rail-locked ghosts alike. Prototype parity: ghosts CAN be hit, spun out, EMP'd
    /// and siphoned (their simulated energy pool is the target); they never scrape walls.
    /// </summary>
    public interface IRacer
    {
        Vector3 Position { get; }
        Vector3 Forward { get; }
        float Speed { get; }
        HullDefinition Hull { get; }
        EnergyCore Energy { get; }
        bool Shielding { get; }
        float IntangibleTimer { get; }
        float SpinoutTimer { get; }

        void ApplySpinout(float duration);
        void ApplyIntangible(float duration);
    }
}
