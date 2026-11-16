using Core.DataStructures;
using Core.Structures;
using MathNet.Numerics.LinearAlgebra;
using System.Numerics;

using MathNetVector = MathNet.Numerics.LinearAlgebra.Vector<double>;

namespace Zcu.Mve.ZMalik.Simplification
{
    public class BasicSimplificator
    {
        private readonly Vector3[] _positions;
        private readonly Collapser _clusterizer;
        private readonly double _maxError;
        private readonly int _triangleCount;
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

            for (int i = 0; i < faceCount; i++)
            {
                Triangle t = triangles[i];
                Vector3 p1 = _positions[t.V1], p2 = _positions[t.V2], p3 = _positions[t.V3];
                Vector3 normal = Vector3.Normalize(Vector3.Cross(p2 - p1, p3 - p1));
                if (float.IsNaN(normal.X)) normal = Vector3.UnitY; // degenerate fallback

                MathNetVector q = MathNetVector.Build.DenseOfArray([normal.X, normal.Y, normal.Z, -Vector3.Dot(normal, p1)]);
                triangleQuadrics[i] = q.OuterProduct(q);
            }

            CornerTable table = new CornerTable(indices, vertices.Length);
            relevantTriangles = new HashSet<int>[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                relevantTriangles[i] = [.. table.VF(i)];
                aggregateQuadrics[i] = Matrix<double>.Build.Dense(4, 4);
                foreach (var face in relevantTriangles[i])
                    aggregateQuadrics[i] += triangleQuadrics[face];
            }

            _clusterizer = useQuadrics
                ? new Collapser(triangles, vertices.Length, CostFunctionQuadric, null, TriangleCntStopCondition, SetNewVertexPositionQuadric, _positions)
                : new Collapser(triangles, vertices.Length, CostFunctionEuclid, null, TriangleCntStopCondition, SetNewVertexPositionEuclid, _positions);
        }

        public double CostFunctionEuclid(int v1, int v2, ICollection<int> v1Nbs, ICollection<int> v2Nbs) =>
            Vector3.DistanceSquared(_positions[v1], _positions[v2]);

        public double CostFunctionQuadric(int v1, int v2, ICollection<int> v1Nbs, ICollection<int> v2Nbs)
        {
            IEnumerable<int> toSubtract = relevantTriangles[v1].Intersect(relevantTriangles[v2]);
            Matrix<double> aggQuadric = aggregateQuadrics[v1] + aggregateQuadrics[v2];
            foreach (var sub in toSubtract) aggQuadric -= triangleQuadrics[sub];

            MathNetVector optimal = GetOptimalPoint(aggQuadric, v1, v2);
            return optimal.DotProduct(aggQuadric * optimal);
        }

        private MathNetVector GetOptimalPoint(Matrix<double> quadric, int v1, int v2)
        {
            Matrix<double> mat = quadric.Clone();
            mat.SetRow(3, [0, 0, 0, 1]);
            try
            {
                return mat.Solve(MathNetVector.Build.DenseOfArray([0, 0, 0, 1]));
            }
            catch
            {
                // Coplanar degeneracy fallback: collapse to the edge midpoint
                Vector3 mid = (_positions[v1] + _positions[v2]) * 0.5f;
                return MathNetVector.Build.DenseOfArray([mid.X, mid.Y, mid.Z, 1]);
            }
        }

        private bool TriangleCntStopCondition(int clusters, int edges, double nextCost) => clusters <= _triangleCount;
        private bool MaxErrStopCondition(int clusters, int edges, double nextCost) => nextCost > _maxError;

        private void SetNewVertexPositionEuclid(int v1, int v2)
        {
            _positions[v2] = (_positions[v1] + _positions[v2]) * 0.5f;
        }

        private void SetNewVertexPositionQuadric(int v1, int v2)
        {
            IEnumerable<int> toSubtract = relevantTriangles[v1].Intersect(relevantTriangles[v2]);
            Matrix<double> aggQuadric = aggregateQuadrics[v1] + aggregateQuadrics[v2];
            foreach (var sub in toSubtract) aggQuadric -= triangleQuadrics[sub];

            MathNetVector optimal = GetOptimalPoint(aggQuadric, v1, v2);
            _positions[v2] = new Vector3((float)optimal[0], (float)optimal[1], (float)optimal[2]);

            aggregateQuadrics[v2] = aggQuadric;
            relevantTriangles[v2] = new HashSet<int>(relevantTriangles[v1].Union(relevantTriangles[v2]));
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