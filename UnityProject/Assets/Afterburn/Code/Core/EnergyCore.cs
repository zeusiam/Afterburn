using System;
using UnityEngine;

namespace Afterburn.Core
{
    /// <summary>
    /// The single spend authority (BUILD §2 — THE inviolable rule). One shared pool per racer:
    /// boost, fire and shield all pay from it; regen happens ONLY on ticks where nothing spent.
    /// Nothing outside this class mutates energy. The mutual-exclusion *resolution* lives in
    /// <see cref="ShipController"/> (boost &gt; shield for held inputs, fire blocked while either
    /// is active — prototype updatePlayer order); this class enforces the accounting.
    /// </summary>
    public sealed class EnergyCore
    {
        private readonly float _max;
        private float _energy;

        public EnergyCore(float hullMaxEnergy, float energyMaxScale)
        {
            _max = hullMaxEnergy * (energyMaxScale / 100f);
            _energy = _max;
        }

        public float Energy => _energy;
        public float Max => _max;
        public float Ratio => _max > 0f ? _energy / _max : 0f;

        /// <summary>Raised whenever the pool value changes (View/HUD read-only hook).</summary>
        public event Action<float, float>? OnEnergyChanged;   // (energy, max)

        /// <summary>Prototype canSpend: activation gate for held modes and discrete costs.</summary>
        public bool CanSpend(float cost) => _energy >= cost;

        /// <summary>
        /// Discrete spend (fire cost). Returns false — and spends nothing — if the pool can't cover it.
        /// </summary>
        public bool TrySpend(float cost)
        {
            if (_energy < cost) return false;
            _energy -= cost;
            OnEnergyChanged?.Invoke(_energy, _max);
            return true;
        }

        /// <summary>
        /// Continuous drain for a held mode this tick (boost/shield). Returns false when the pool
        /// empties — the caller must self-cancel the mode the same tick (prototype: boosting=false
        /// at energy&lt;=0). Clamps at 0 exactly like the prototype.
        /// </summary>
        public bool Drain(float perSecond, float dt)
        {
            _energy -= perSecond * dt;
            if (_energy <= 0f)
            {
                _energy = 0f;
                OnEnergyChanged?.Invoke(_energy, _max);
                return false;
            }
            OnEnergyChanged?.Invoke(_energy, _max);
            return true;
        }

        /// <summary>
        /// Regen this tick — the caller guarantees the §2 precondition (no boost, no shield, no
        /// shot fired this tick). Prototype: hull.regenPerSec × (regenScale/8) × dt.
        /// </summary>
        public void Regen(float hullRegenPerSec, float regenScale, float dt)
        {
            _energy = Mathf.Min(_max, _energy + hullRegenPerSec * (regenScale / 8f) * dt);
            OnEnergyChanged?.Invoke(_energy, _max);
        }

        /// <summary>
        /// Clamped grant — bounty rewards, siphon transfer, perfect-launch bonus. The ONLY way
        /// energy enters from outside; never exceeds max (PortSpec §7 onHit).
        /// </summary>
        public void Grant(float amount)
        {
            _energy = Mathf.Min(_max, _energy + amount);
            OnEnergyChanged?.Invoke(_energy, _max);
        }

        /// <summary>
        /// External damage/drain (projectile hit, EMP, siphon steal). Floors at 0. Returns the
        /// amount actually removed (siphon #7 ruling needs the real figure, never the nominal).
        /// </summary>
        public float Damage(float amount)
        {
            float removed = Mathf.Min(_energy, amount);
            _energy -= removed;
            OnEnergyChanged?.Invoke(_energy, _max);
            return removed;
        }
    }
}
