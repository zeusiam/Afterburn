using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afterburn.Core
{
    /// <summary>
    /// Projectiles + hit resolution (BUILD §7.3 combat half / PortSpec §7). Prototype-exact:
    /// fixed pool of 40 bullets (pool-dry = shot silently dropped), world-space straight flight
    /// at 170 + 0.4×shooter speed, ttl 1.6 s, hit when distance &lt; hull.radius + 1.6.
    /// Track-space conversion is deliberately deferred to the 3D-arena phase — on flat Arena01
    /// world-space IS the prototype (parity beats the refactor; see DesignReview U3 note).
    ///
    /// onHit: damage ×0.40 through shield → spinout 0.7 → bounty reward 8 (×2 vs leader) →
    /// siphon steal CAPPED at the victim's pool (ruling #7 — the prototype could mint energy).
    /// </summary>
    public sealed class CombatSystem
    {
        /// <summary>Prototype: 40 pre-created bullets; y locked at 1.5.</summary>
        public const int PoolSize = 40;
        public const float BulletRideHeight = 1.5f;

        public sealed class Projectile
        {
            public bool Live;
            public Vector3 Position;
            public Vector3 Velocity;
            public float Ttl;
            public IRacer Owner = null!;
            public bool Siphon;
        }

        private readonly GameTuning _tuning;
        private readonly PilotAbilitySystem _abilities;
        private readonly Projectile[] _pool = new Projectile[PoolSize];
        private readonly Func<IRacer?> _leaderProvider;

        /// <summary>(shooter, target, rewardPaid, targetWasLeader) — HUD/stats hook (U4/U5).</summary>
        public event Action<IRacer, IRacer, float, bool>? OnHitLanded;

        public CombatSystem(GameTuning tuning, PilotAbilitySystem abilities, Func<IRacer?>? leaderProvider = null)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _abilities = abilities ?? throw new ArgumentNullException(nameof(abilities));
            _leaderProvider = leaderProvider ?? (() => null);
            for (int i = 0; i < PoolSize; i++) _pool[i] = new Projectile();
        }

        public IReadOnlyList<Projectile> Projectiles => _pool;

        /// <summary>Wire a racer: its OnFired event spawns a bullet flagged with its live siphon state.</summary>
        public void Attach(ShipController ship)
        {
            ship.OnFired += shooter => Fire(shooter, _abilities.IsSiphonArmed(shooter));
        }

        /// <summary>Prototype Combat.fire: first dead slot or silently drop.</summary>
        public void Fire(IRacer shooter, bool siphon)
        {
            Projectile? slot = null;
            foreach (Projectile p in _pool)
            {
                if (!p.Live) { slot = p; break; }
            }
            if (slot == null) return;   // pool dry — prototype drops the shot silently

            ShipFeel feel = _tuning.shipFeel;
            Vector3 dir = shooter.Forward;
            Vector3 pos = shooter.Position + dir * (feel.projectileSpawnAhead * feel.worldScale);
            pos.y = BulletRideHeight;

            slot.Live = true;
            slot.Position = pos;
            slot.Velocity = dir * (feel.projectileSpeed * feel.worldScale + feel.projectileInheritFactor * shooter.Speed);
            slot.Ttl = feel.projectileLifetime;
            slot.Owner = shooter;
            slot.Siphon = siphon;
        }

        /// <summary>
        /// Per fixed tick: advance, expire, hit-test every live bullet against every other racer
        /// (skip owner, finished racers, and intangible ships — Sora phases through shots).
        /// First hit kills the bullet.
        /// </summary>
        public void Tick(float dt, IReadOnlyList<IRacer> racers, IReadOnlyList<bool>? finished = null)
        {
            ShipFeel feel = _tuning.shipFeel;
            foreach (Projectile p in _pool)
            {
                if (!p.Live) continue;
                p.Position += p.Velocity * dt;
                p.Ttl -= dt;
                if (p.Ttl <= 0f) { p.Live = false; continue; }

                for (int r = 0; r < racers.Count; r++)
                {
                    IRacer target = racers[r];
                    if (target == p.Owner) continue;
                    if (finished != null && finished[r]) continue;
                    if (target.IntangibleTimer > 0f) continue;

                    float hitRange = target.Hull.collisionRadius + feel.projectileHitPad;
                    if (Vector3.Distance(p.Position, target.Position) < hitRange)
                    {
                        ResolveHit(p.Owner, target, p.Siphon);
                        p.Live = false;
                        break;
                    }
                }
            }
        }

        /// <summary>Prototype onHit with ruling #7 applied.</summary>
        public void ResolveHit(IRacer from, IRacer target, bool siphon)
        {
            ShipFeel feel = _tuning.shipFeel;

            float dmg = feel.projectileDamage;
            if (target.Shielding) dmg *= _tuning.shieldDamageMult;
            target.Energy.Damage(dmg);
            target.ApplySpinout(feel.spinoutDuration);

            bool wasLeader = _leaderProvider() == target;
            float reward = feel.bountyBaseReward;
            if (wasLeader) reward *= _tuning.bountyRewardMult;
            from.Energy.Grant(reward);

            if (siphon)
            {
                // Ruling #7: steal exactly what the victim still has, never mint (prototype was
                // flat −25/+25). Magnitude = the shooter's pilot abilityParam, not projectileDamage.
                float stolen = target.Energy.Damage(_abilities.SiphonAmountOf(from));
                from.Energy.Grant(stolen);
                _abilities.ConsumeSiphon(from);
            }

            OnHitLanded?.Invoke(from, target, reward, wasLeader);
        }
    }
}
