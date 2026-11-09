using Core.DataStructures;
using Core.Structures;
using MathNet.Numerics.LinearAlgebra;
using System.Numerics;
using System.Runtime.Intrinsics.X86;


// Use this alias to avoid System.Numerics conflicts
using MathNetVector = MathNet.Numerics.LinearAlgebra.Vector<double>;

namespace Zcu.Mve.ZMalik.Simplification
{
    public class BasicSimplificator
    {
        private readonly Vector3[] _positions;
        private readonly Collapser _clusterizer;
        private readonly double _maxError;
        private readonly int _triangleCount;

        // Data structures for quadric simplification
        private readonly HashSet<int>[] relevantTriangles;
        private readonly Dictionary<int, Matrix<double>> aggregateQuadrics = [];
        private readonly Dictionary<int, Matrix<double>> triangleQuadrics = [];

        public BasicSimplificator(Vertex[] vertices, int[] indices, double maximumError, int triangleCount, bool useQuadrics)
        {
            _maxError = maximumError;
            _triangleCount = triangleCount;
            _positions = vertices.Select(v => v.Position).ToArray();

            int faceCount = indices.Length / 3;
            Triangle[] triangles = new Triangle[faceCount];
            for (int i = 0; i < faceCount; i++)
                triangles[i] = new Triangle(indices[i * 3], indices[i * 3 + 1], indices[i * 3 + 2]);

            // Hint: You can use the CornerTable to easily find adjacent faces for a given vertex
            var table = new CornerTable(indices, vertices.Length);

            // TODO: initialize any data structures that you might need
            // in particular, for each vertex, you should keep a list of relevant triangles
            // you should also initialize the list of quadrics for each triangle.

            // INITIALIZE TRIANGLE QUADRICS
            for (int i = 0; i <  triangles.Length; ++i)
            {
                var t = triangles[i];
                // calculate quadric
                var v1 = vertices[t.V1].Position;
                var v2 = vertices[t.V2].Position;
                var v3 = vertices[t.V3].Position;

                var cross = Vector3.Cross(v2 - v1, v3 - v1);
                var n = Vector3.Normalize(cross);
                var d = -Vector3.Dot(n, v1);

                var q = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.Dense(new double[] { n.X, n.Y, n.Z, d});
                var Q = q.OuterProduct(q);

                triangleQuadrics[i] = Q;
            }

            // INITIALIZE VERTEX QUADRICS
            relevantTriangles = new HashSet<int>[vertices.Length];
            for (int i = 0; i < vertices.Length; ++i)
            {
                var neighs = table.VF(i);
                var sum = MathNet.Numerics.LinearAlgebra.Matrix<double>.Build.Dense(4, 4);
                foreach (var t in neighs)
                {
                    sum += triangleQuadrics[t];
                }
                aggregateQuadrics[i] = sum;
                relevantTriangles[i] = new HashSet<int>(neighs);
            }

            _clusterizer = useQuadrics
                ? new Collapser(triangles, vertices.Length, CostFunctionQuadric, null, TriangleCntStopCondition, SetNewVertexPositionQuadric, _positions)
                : new Collapser(triangles, vertices.Length, CostFunctionEuclid, null, TriangleCntStopCondition, SetNewVertexPositionEuclid, _positions);
        }

        public double CostFunctionEuclid(int v1, int v2, ICollection<int> v1Nbs, ICollection<int> v2Nbs) =>
            Vector3.DistanceSquared(_positions[v1], _positions[v2]);

        public double CostFunctionQuadric(int v1, int v2, ICollection<int> v1Nbs, ICollection<int> v2Nbs)
        {
            // TODO: implement the quadrics cost function. It might be a good idea to use the quadric as well to 
            // compute and store the new vertex position
            var neighs1 = relevantTriangles[v1];
            var neighs2 = relevantTriangles[v2];

            var commonNeighs = neighs1.Intersect(neighs2);

            var Q = aggregateQuadrics[v1] + aggregateQuadrics[v2];
            foreach (var t in commonNeighs)
            {
                Q -= triangleQuadrics[t];
            }

            var Q2 = MathNet.Numerics.LinearAlgebra.Matrix<double>.Build.Dense(4, 4);
            Q.CopyTo(Q2);

            Q[3, 0] = 0;
            Q[3, 1] = 0;
            Q[3, 2] = 0;
            Q[3, 3] = 1;

            var p = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.Dense(new double[] { 0, 0, 0, 1 });

            p = Q.Solve(p);

            var r = p * (Q2 * p);

            return r;
        }

        private bool TriangleCntStopCondition(int clusters, int edges, double nextCost) => clusters <= _triangleCount;
        private bool MaxErrStopCondition(int clusters, int edges, double nextCost) => nextCost > _maxError;

        private void SetNewVertexPositionEuclid(int v1, int v2)
        {
            _positions[v2] = (_positions[v1] + _positions[v2]) * 0.5f;
        }

        private void SetNewVertexPositionQuadric(int v1, int v2)
        {
            // TODO: set the new position of the vertex according to the quadric
            // *AND* update any data structures you might need later
            var neighs1 = relevantTriangles[v1];
            var neighs2 = relevantTriangles[v2];

            var commonNeighs = neighs1.Intersect(neighs2);

            var Q = aggregateQuadrics[v1] + aggregateQuadrics[v2];
            foreach (var t in commonNeighs)
            {
                Q -= triangleQuadrics[t];
            }

            var Q2 = MathNet.Numerics.LinearAlgebra.Matrix<double>.Build.Dense(4, 4);
            Q.CopyTo(Q2);

            Q[3, 0] = 0;
            Q[3, 1] = 0;
            Q[3, 2] = 0;
            Q[3, 3] = 1;

            var p = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.Dense(new double[] { 0, 0, 0, 1 });

            p = Q.Solve(p);

            _positions[v2] = new System.Numerics.Vector3((float)p[0], (float)p[1], (float)p[2]);

            // remove common neighs
            foreach (var t in commonNeighs) {
                relevantTriangles[v1].Remove(t);
                //relevantTriangles[v2].Remove(t);
            }
            // add from v1 to v2
            relevantTriangles[v2].UnionWith(relevantTriangles[v1]);
            // book-keeping: clear v1
            relevantTriangles[v1].Clear();

            // recalculate aggregate quadrics
            aggregateQuadrics[v2] = Q2;
            aggregateQuadrics[v1].Clear();
        }

        public (Vertex[] Vertices, int[] Indices) Simplify()
        {
            int[] children = _clusterizer.GetClusterChildren();
            Vertex[] outVertices = new Vertex[children.Length];
            for (int i = 0; i < children.Length; i++)
            {
                Vector3 p = _positions[children[i]];
                outVertices[i] = Vertex.FromPositionNormal(p.X, p.Y, p.Z);
            }

            Triangle[] trisCells = _clusterizer.GetTriangles();
            int[] outIndices = new int[trisCells.Length * 3];
            for (int i = 0; i < trisCells.Length; i++)
            {
                outIndices[i * 3] = trisCells[i].V1;
                outIndices[i * 3 + 1] = trisCells[i].V2;
                outIndices[i * 3 + 2] = trisCells[i].V3;
            }

            return (outVertices, outIndices);
        }
    }
}