using Core.DataStructures;
using Core.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DataStructures
{
    public class CornerTable : IEdgeDataStructure
    {
        public int[] initial;
        public int[] opposite;
        private int[] _indices;

        public CornerTable(int[] indices, int vertexCount)
        {
            _indices = indices;
            initial = new int[vertexCount];
            opposite = new int[indices.Length];

            // Initialize to -1 to handle meshes with borders
            for (int i = 0; i < opposite.Length; i++)
                opposite[i] = -1;

            var dictionary = new Dictionary<Edge, int>();

            for (int i = 0; i < indices.Length / 3; i++)
            {
                int v1 = indices[i * 3];
                int v2 = indices[i * 3 + 1];
                int v3 = indices[i * 3 + 2];

                // For edge (v2, v3), the opposite corner is v1
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
            if (dictionary.ContainsKey(e))
            {
                int oppositeCornerIndex = dictionary[e];
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

        public bool IsOnBorder(int vertexIndex)
        {
            int init = initial[vertexIndex];
            int current = opposite[n(init)];
            while (current != -1)
            {
                if (n(current) == init) return false;
                current = opposite[p(current)];
            }
            return true;
        }

        public int[] VF(int vertexIndex)
        {
            List<int> result = new List<int>();
            int initCorner = initial[vertexIndex];
            if (initCorner == -1) return result.ToArray();

            int curr = initCorner;
            while (true)
            {
                int opp = opposite[p(curr)];
                if (opp == -1) break;
                int cwNext = p(opp); // Anchor back to central vertex
                if (cwNext == initCorner) break; // Closed loop
                curr = cwNext;
            }

            int begin = curr;
            do
            {
                result.Add(curr / 3);

                int opp = opposite[n(curr)];
                if (opp == -1) break;

                curr = n(opp); // Anchor back to central vertex
            } while (curr != begin);

            return result.ToArray();
        }

        public int[] VV(int vertexIndex)
        {
            List<int> result = new List<int>();
            int initCorner = initial[vertexIndex];
            if (initCorner == -1) return result.ToArray();

            // Find true start
            int curr = initCorner;
            while (true)
            {
                int opp = opposite[p(curr)];
                if (opp == -1) break;
                int cwNext = p(opp);
                if (cwNext == initCorner) break;
                curr = cwNext;
            }

            // Collect vertices
            int begin = curr;
            do
            {
                result.Add(v(n(curr)));

                int opp = opposite[n(curr)];
                if (opp == -1)
                {
                    // Open boundary: explicitly add the final vertex on the other side
                    result.Add(v(p(curr)));
                    break;
                }

                curr = n(opp);
            } while (curr != begin);

            return result.ToArray();
        }
    }
}
