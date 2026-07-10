using System.Collections.Generic;
using UnityEngine;

namespace Afterburn.Core
{
    /// <summary>
    /// D14 (owner ruling, 2026-07-09): ships are TANGIBLE. Player↔ghost contact deals mutual
    /// energy damage scaled by mass ratio — Heavy shrugs off what batters Light (hull.mass
    /// finally has a gameplay consumer). The player is pushed out of the overlap and scraped;
    /// rail-locked ghosts hold their line (they read as the heavier presence). Sora's Phase
    /// passes through contact exactly as it passes walls and shots.
    ///
    /// §2-safe by construction: contact only DRAINS pools, never grants. Per-pair cooldown stops
    /// overlap from becoming a per-tick drain loop. Finished (frozen) ghosts are non-contact.
    /// </summary>
    public sealed class ShipContactSystem
    {
        private readonly GameTuning _tuning;
        private readonly Dictionary<IRacer, float> _cooldowns = new();

        /// <summary>(player, other, damageToPlayer) — HUD/VFX hook.</summary>
        public event System.Action<IRacer, IRacer, float>? OnShipContact;

        public ShipContactSystem(GameTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new System.ArgumentNullException(nameof(tuning));
        }

        public void Tick(float dt, ShipController player, IReadOnlyList<GhostRacer> ghosts)
        {
            ShipFeel feel = _tuning.shipFeel;
            if (feel.shipContactDamage <= 0f) return;

            foreach (GhostRacer ghost in ghosts)
            {
                if (_cooldowns.TryGetValue(ghost, out float cd) && cd > 0f)
                {
                    _cooldowns[ghost] = cd - dt;
                    continue;
                }
                if (ghost.Finished) continue;                     // frozen finishers are non-contact
                if (player.IntangibleTimer > 0f) continue;        // Sora phases through everything

                float contactRange = player.Hull.collisionRadius + ghost.Hull.collisionRadius;
                Vector3 delta = player.Position - ghost.Position;
                if (delta.sqrMagnitude >= contactRange * contactRange) continue;

                // Mutual, mass-scaled damage: you take (their mass / your mass) × base.
                float toPlayer = feel.shipContactDamage * (ghost.Hull.mass / player.Hull.mass);
                float toGhost = feel.shipContactDamage * (player.Hull.mass / ghost.Hull.mass);
                player.Energy.Damage(toPlayer);
                ghost.Energy.Damage(toGhost);

                // Push the player out of the overlap along the pair axis (XZ), keep ride height.
                Vector3 axis = new Vector3(delta.x, 0f, delta.z);
                axis = axis.sqrMagnitude > 1e-6f ? axis.normalized : player.Forward * -1f;
                Vector3 pushed = ghost.Position + axis * contactRange;
                pushed.y = player.Position.y;
                player.ApplyContactPush(pushed, feel.shipContactSpeedMult);

                _cooldowns[ghost] = feel.shipContactCooldown;
                OnShipContact?.Invoke(player, ghost, toPlayer);
            }
        }
    }
}
