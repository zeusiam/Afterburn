using System;
using System.Collections.Generic;

namespace Afterburn.Core
{
    /// <summary>
    /// Minimal allocation-free object pool (BUILD §3: studio object-pool utility for all
    /// projectiles / VFX / decoys). Reuses a backing <see cref="Stack{T}"/> so Get/Release never
    /// allocate after warm-up. Generic over any reference type; the projectile/decoy pools wrap
    /// this with a factory that instantiates the view prefab.
    ///
    /// Copied verbatim (namespace only) from the Veratus Games studio standard (Rune Rouge).
    /// </summary>
    public sealed class ObjectPool<T> where T : class
    {
        private readonly Stack<T> _free;
        private readonly Func<T> _factory;
        private readonly Action<T>? _onGet;
        private readonly Action<T>? _onRelease;

        public ObjectPool(Func<T> factory, Action<T>? onGet = null, Action<T>? onRelease = null, int prewarm = 0)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _onGet = onGet;
            _onRelease = onRelease;
            _free = new Stack<T>(prewarm > 0 ? prewarm : 8);
            if (prewarm > 0) Prewarm(prewarm);
        }

        /// <summary>Count currently parked in the pool (available without allocating).</summary>
        public int CountInactive => _free.Count;

        /// <summary>Take an item — reused if one is parked, otherwise freshly built by the factory.</summary>
        public T Get()
        {
            T item = _free.Count > 0 ? _free.Pop() : _factory();
            _onGet?.Invoke(item);
            return item;
        }

        /// <summary>Return an item to the pool. No-op on null.</summary>
        public void Release(T? item)
        {
            if (item == null) return;
            _onRelease?.Invoke(item);
            _free.Push(item);
        }

        /// <summary>Pre-build <paramref name="count"/> items so the first wave costs no allocation.</summary>
        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                T item = _factory();
                _onRelease?.Invoke(item);
                _free.Push(item);
            }
        }

        /// <summary>Drop every parked item (e.g. on run end). Live items are unaffected.</summary>
        public void Clear() => _free.Clear();
    }
}
