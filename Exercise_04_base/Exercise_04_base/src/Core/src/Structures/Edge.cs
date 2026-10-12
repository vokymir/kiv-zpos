using System;

namespace Core.Structures
{
    public readonly struct Edge : IEquatable<Edge>
    {
        public int V1 { get; }
        public int V2 { get; }

        public Edge(int v1, int v2)
        {
            V1 = v1;
            V2 = v2;
        }

        public override bool Equals(object? obj) => obj is Edge t && Equals(t);

        public bool Equals(Edge other) => V1 == other.V1 && V2 == other.V2;

        public override int GetHashCode() => V1 ^ (V2 * 1000);
    }
}