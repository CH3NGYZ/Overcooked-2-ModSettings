using System;
using System.Collections.Generic;

namespace OC2ModSettings
{
    // Replacement invalidates the old lease instead of letting it erase its successor.
    internal sealed class LeaseTable<TKey, TValue>
    {
        private sealed class Slot { internal TValue Value; internal object Token; }
        private readonly Dictionary<TKey, Slot> slots = new Dictionary<TKey, Slot>();
        private readonly object gate = new object();
        private readonly Action changed;
        internal LeaseTable(Action onChanged) { changed = onChanged; }
        internal IDisposable Set(TKey key, TValue value)
        {
            object token = new object();
            lock (gate) slots[key] = new Slot { Value = value, Token = token };
            changed();
            return new ReleaseAction(delegate
            {
                bool removed = false;
                lock (gate)
                {
                    Slot slot;
                    if (slots.TryGetValue(key, out slot) && ReferenceEquals(slot.Token, token)) removed = slots.Remove(key);
                }
                if (removed) changed();
            });
        }
        internal bool TryGet(TKey key, out TValue value)
        {
            lock (gate)
            {
                Slot slot;
                if (slots.TryGetValue(key, out slot)) { value = slot.Value; return true; }
                value = default(TValue); return false;
            }
        }
        internal IEnumerable<TKey> Keys { get { lock (gate) return new List<TKey>(slots.Keys); } }
    }

    internal sealed class ReleaseAction : IDisposable
    {
        private Action release;
        internal ReleaseAction(Action action) { release = action; }
        public void Dispose() { Action action = System.Threading.Interlocked.Exchange(ref release, null); if (action != null) action(); }
    }
}
