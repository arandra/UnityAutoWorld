using System;

namespace AutoWorld.Core
{
    /// <summary>
    /// 필드가 차지하는 단일 격자 좌표를 표현한다.
    /// </summary>
    public readonly struct FieldCoordinate : IEquatable<FieldCoordinate>
    {
        public FieldCoordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }

        public int Y { get; }

        public bool Equals(FieldCoordinate other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is FieldCoordinate other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 31) + X;
                hash = (hash * 31) + Y;
                return hash;
            }
        }

        public override string ToString()
        {
            return string.Format("({0}, {1})", X, Y);
        }
    }
}
