using System.Collections.Generic;

namespace Core.Structures
{
    public class CornerTable
    {
        public int[] initial;
        public int[] opposite;
        private readonly int[] _indices;

        public CornerTable(int[] indices, int vertexCount)
        {
            _indices = indices;
            initial = new int[vertexCount];
            opposite = new int[indices.Length];

            for (int i = 0; i < opposite.Length; i++)
                opposite[i] = -1;

            var dictionary = new Dictionary<Edge, int>();

            for (int i = 0; i < indices.Length / 3; i++)
            {
                int v1 = indices[i * 3];
                int v2 = indices[i * 3 + 1];
                int v3 = indices[i * 3 + 2];

                ProcessEdge(dictionary, new Edge(v2, v3), 3 * i);
                initial[v1] = 3 * i;

                ProcessEdge(dictionary, new Edge(v3, v1), 3 * i + 1);
                initial[v2] = 3 * i + 1;

                ProcessEdge(dictionary, new Edge(v1, v2), 3 * i + 2);
                initial[v3] = 3 * i + 2;
            }
        }

        private void ProcessEdge(Dictionary<Edge, int> dictionary, Edge e, int cornerIndex)
        {
            if (dictionary.TryGetValue(e, out int oppositeCornerIndex))
            {
                opposite[cornerIndex] = oppositeCornerIndex;
                opposite[oppositeCornerIndex] = cornerIndex;
                dictionary.Remove(e);
            }
            else
            {
                Edge reversed = new Edge(e.V2, e.V1);
                dictionary.Add(reversed, cornerIndex);
            }
        }

        public int n(int c) => (c % 3 == 2) ? (c - 2) : (c + 1);
        public int p(int c) => (c % 3 == 0) ? (c + 2) : (c - 1);
        public int v(int c) => _indices[c];

        public int[] VF(int vertex)
        {
            List<int> result = [];
            int initCorner = initial[vertex];

            result.Add(initCorner / 3);

            int o = opposite[n(initCorner)];
            while (o != -1)
            {
                if (n(o) == initCorner) break;
                result.Add(o / 3);
                o = opposite[p(o)];
            }

            if (o == -1)
            {
                o = opposite[p(initCorner)];
                while (o != -1)
                {
                    if (p(o) == initCorner) break;
                    result.Add(o / 3);
                    o = opposite[n(o)];
                }
            }
            return [.. result];
        }

        public int[] VV(int vertex)
        {
            List<int> result = [];
            int initCorner = initial[vertex];

            result.Add(v(p(initCorner)));

            int o = opposite[n(initCorner)];
            while (o != -1)
            {
                if (n(o) == initCorner) break;
                result.Add(v(o));
                o = opposite[p(o)];
            }

            if (o == -1)
            {
                result.Add(v(n(initCorner)));

                o = opposite[p(initCorner)];
                while (o != -1)
                {
                    if (p(o) == initCorner) break;
                    result.Add(v(o));
                    o = opposite[n(o)];
                }
            }
            return [.. result];
        }

        public bool IsOnBorder(int vertex)
        {
            int initCorner = initial[vertex];

            int o = opposite[n(initCorner)];
            while (o != -1)
            {
                if (n(o) == initCorner) return false;
                o = opposite[p(o)];
            }
            return true;
        }
    }
}