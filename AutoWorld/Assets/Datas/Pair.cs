using System;
using System.Collections.Generic;

namespace Datas
{
    [Serializable]
    public struct Pair<TKey, TValue> : IEquatable<Pair<TKey, TValue>>
    {
        public TKey Key;
        public TValue Value;

        public Pair(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }

        // KVP ↔ Pair
        public static implicit operator KeyValuePair<TKey, TValue>(Pair<TKey, TValue> p)
            => new KeyValuePair<TKey, TValue>(p.Key, p.Value);

        public static implicit operator Pair<TKey, TValue>(KeyValuePair<TKey, TValue> kv)
            => new Pair<TKey, TValue>(kv.Key, kv.Value);

        // tuple ↔ Pair
        public static implicit operator (TKey, TValue)(Pair<TKey, TValue> p) => (p.Key, p.Value);
        public static implicit operator Pair<TKey, TValue>((TKey Key, TValue Value) t) => new Pair<TKey, TValue>(t.Key, t.Value);

        public void Deconstruct(out TKey key, out TValue value)
        {
            key = Key;
            value = Value;
        }

        public bool Equals(Pair<TKey, TValue> other)
            => EqualityComparer<TKey>.Default.Equals(Key, other.Key)
               && EqualityComparer<TValue>.Default.Equals(Value, other.Value);

        public override bool Equals(object obj)
            => obj is Pair<TKey, TValue> other && Equals(other);

        public override int GetHashCode()
        {
            return HashCode.Combine(Key, Value);
        }

        public static bool operator ==(Pair<TKey, TValue> left, Pair<TKey, TValue> right) => left.Equals(right);
        public static bool operator !=(Pair<TKey, TValue> left, Pair<TKey, TValue> right) => !left.Equals(right);

        public override string ToString() => $"({Key}, {Value})";
    }
}