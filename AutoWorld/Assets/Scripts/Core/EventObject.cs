using System;

namespace AutoWorld.Core
{
    public readonly struct EventObject : IEquatable<EventObject>
    {
        public EventObject(EventObjectType type, int id)
        {
            Type = type;
            Id = id;
        }

        public EventObjectType Type { get; }

        public int Id { get; }

        public bool Equals(EventObject other) => Type == other.Type && Id == other.Id;

        public override bool Equals(object? obj) => obj is EventObject other && Equals(other);

        public override int GetHashCode() => HashCode.Combine((int)Type, Id);

        public override string ToString() => $"{Type}:{Id}";
    }
}
