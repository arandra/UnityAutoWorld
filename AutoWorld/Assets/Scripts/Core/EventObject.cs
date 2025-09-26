using System;

namespace AutoWorld.Core
{
    public struct EventObject : IEquatable<EventObject>
    {
        public EventObject(EventObjectType type, int id)
        {
            Type = type;
            Id = id;
        }

        public EventObjectType Type { get; }

        public int Id { get; }

        public bool Equals(EventObject other)
        {
            return Type == other.Type && Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is EventObject other && Equals(other);
        }

        public static bool operator ==(EventObject left, EventObject right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EventObject left, EventObject right)
        {
            return !left.Equals(right);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 31) + (int)Type;
                hash = (hash * 31) + Id;
                return hash;
            }
        }

        public override string ToString()
        {
            return string.Format("{0}:{1}", Type, Id);
        }
    }
}
