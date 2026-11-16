using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Framework;
using MathNet.Numerics.LinearAlgebra;

namespace Zcu.Mve.ZMalik.Simplification
{
    public class BasicSimplificator
    {
        private TriangleMesh _mesh;

        private Point3D[] _positions;

        private Collapser _clusterizer;

        private double _maxError;

        private int _triangleCount;

        private int _origTriangleCount;

        private HashSet<int>[] relevantTriangles;
        private Dictionary<int, Matrix<double>> aggregateQuadrics = new Dictionary<int, Matrix<double>>();
        private Dictionary<int, Matrix<double>> triangleQuadrics = new Dictionary<int, Matrix<double>>();

        private bool _useQuadrics;

        public BasicSimplificator(TriangleMesh mesh, double maximumError, int triangleCount, bool useQuadrics)
        {
            _maxError = maximumError;

            _triangleCount = triangleCount;

            StopCondition stopCondMethod = TriangleCntStopCondition;
                        
            _mesh = mesh.DeepCopy();

            _positions = _mesh.Points;
            _useQuadrics = useQuadrics;

            Triangle[] triangles = mesh.Triangles;
            _origTriangleCount = triangles.Length;
            int N = _mesh.Points.Length;

            // TODO: initialize any data structures that you might need
            // in particular, for each vertex, you should keep a list of relevant triangles
            // you should also initialize the list of quadrics for each triangle.
            #region Hidden
            for (int i = 0; i < _origTriangleCount; i++)
            {
                Triangle t = _mesh.Triangles[i];
                Point3D normal = _mesh.TriangleNormal(i);
                Point3D v1 = _positions[t.V1];

                Vector<double> q = Vector<double>.Build.DenseOfArray(new double[] { normal.X, normal.Y, normal.Z, -(normal.DotProduct(v1)) });

                // Triangle quadric computation
                Matrix<double> quadric = q.OuterProduct(q);
                triangleQuadrics[i] = quadric;
            }

            relevantTriangles = new HashSet<int>[N];
            for (int i = 0; i < N; i++)
            {
                // For each vertex we compute ag. quadric from incident triangles
                int[] relevant = _mesh.VF(i);
                relevantTriangles[i] = new HashSet<int>(relevant);

                aggregateQuadrics[i] = Matrix<double>.Build.Dense(4, 4);

                // Sum of triangle quadrics of neighbor triangles
                foreach (var triangle in relevant)
                {
                    aggregateQuadrics[i] += triangleQuadrics[triangle];
                }
            }
            #endregion

            // Decision between Euclid and Quadric cost function and vertex positions computation
            if (useQuadrics)
                _clusterizer = new Collapser(triangles, N, CostFunctionQuadric, null, stopCondMethod, SetNewVertexPositionQuadric, _positions);
            else
                _clusterizer = new Collapser(triangles, N, CostFunctionEuclid, null, stopCondMethod, SetNewVertexPositionEuclid, _positions);
        }

        public double CostFunctionEuclid(int v1, int v2, ICollection<int> v1Nbs, ICollection<int> v2Nbs)
        {
            // TODO: implement the quadrics cost function. It might be a good idea to use the quadric as well to 
            // compute and store the new vertex position
            return _positions[v1].SquareDistanceTo(_positions[v2]);
        }

        public double CostFunctionQuadric(int v1, int v2, ICollection<int> v1Nbs, ICollection<int> v2Nbs)
        {
            // Get indices of triangles shared by both ag. quadrics
            IEnumerable<int> toSubtract = relevantTriangles[v1].Intersect(relevantTriangles[v2]);

            // Sum of ag. quadrics of both vertices v1 and v2
            Matrix<double> aggregateQuadric = aggregateQuadrics[v1] + aggregateQuadrics[v2];

            // Subtract shared triangle quadrics
            foreach (var subtracted in toSubtract)
            {
                aggregateQuadric -= triangleQuadrics[subtracted];
            }

            // Get position of optimal point in projective space
            Vector<double> optimal = getOptimalPoint(aggregateQuadric);

            // Compute cost funciton value
            Console.WriteLine(optimal * aggregateQuadric * optimal);
            return optimal * aggregateQuadric * optimal;
        }

        private Vector<double> getOptimalPoint(Matrix<double> quadric)
        {
            // Copy quadric matrix
            Matrix<double> mat = quadric.Clone();

            // Set last row to enforce homogenous coordinate equal to one
            mat.SetRow(3, new double[] { 0, 0, 0, 1 });

            // Solve point coordinates
            return mat.Solve(Vector<double>.Build.DenseOfArray(new double[] { 0, 0, 0, 1 }));
        }

        private Vector<double> getOptimalPointNew(Matrix<double> quadric)
        {
            // Copy quadric matrix
            Matrix<double> mat = quadric.Clone();
             
            // Set last row to enforce homogenous coordinate equal to one
            // mat.SetRow(3, new double[] { 0, 0, 0, 1 });

            // Solve point coordinates
            var res = mat.Solve(Vector<double>.Build.DenseOfArray(new double[] { 0, 0, 0, 1 }));
            res /= res[3];
            return res;
        }

        private bool TriangleCntStopCondition(int numberTriangles, int numberEdges, double nextCost)
        {
            if (numberTriangles <= _triangleCount) return true;
            return false;
        }

        private bool MaxErrStopCondition(int numberClusters, int numberEdges, double nextCost)
        {
            if (nextCost > _maxError) return true;
            return false;
        }

        private void SetNewVertexPositionEuclid(int v1, int v2)
        {
            // TODO: set the new position of the vertex according to the quadric
            // *AND* update any data structures you might need later

            _positions[v2] = 0.5 * _positions[v1] + 0.5 * _positions[v2];
        }

        private void SetNewVertexPositionQuadric(int v1, int v2)
        {
            // TODO: set the new position of the vertex according to the quadric
            // *AND* update any data structures you might need later
            IEnumerable<int> toSubtract = relevantTriangles[v1].Intersect(relevantTriangles[v2]);

            Matrix<double> aggregateQuadric = aggregateQuadrics[v1] + aggregateQuadrics[v2];

            foreach (var subtracted in toSubtract)
            {
                aggregateQuadric -= triangleQuadrics[subtracted];
            }

            Vector<double> optimal = getOptimalPoint(aggregateQuadric);
            // We do not have to divide coordinates by homogenous component, because it is always one
            _positions[v2] = new Point3D(optimal[0], optimal[1], optimal[2]);

            // We should update aggregated quadrics of new point v2 which replaced old vertices v1 and v2
            // We have already computed ag. quadric
            aggregateQuadrics[v2] = aggregateQuadric;
            // Update relevant triangles for new vertex
            relevantTriangles[v2] = new HashSet<int>(relevantTriangles[v1].Union(relevantTriangles[v2]).Distinct());
        }

        public TriangleMesh Simplify()
        {
            int[] children = _clusterizer.GetClusterChildren();

            Point3D[] points = new Point3D[children.Length];
            for (int i = 0; i < children.Length; i++)
            {
                points[i] = _positions[children[i]];
            }

            Triangle[] trisCells = _clusterizer.GetTriangles();

            TriangleMesh res = new TriangleMesh();
            res.Points = points;
            res.Triangles = trisCells;
            return res;
        }

    }
}
