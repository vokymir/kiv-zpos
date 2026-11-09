using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Zcu.Mve.ZMalik.Simplification
{
    public delegate double CostFunction(
        int vertex1Index,
        int vertex2Index,
        ICollection<int> vertex1Neighbors,
        ICollection<int> vertex2Neighbors
    );

    public delegate Line[] ConnectComponentsFunction(
        int[] componentIndices);

    public delegate bool StopCondition(
        int numberClusters, int numberEdges, double nextCost);

    public delegate void SetNewVertexPosition(
        int srcVertex, int destVertex);

    public class Collapser
    {
        private ConnectedPoint[] _points;

        private EdgeCollapsePQ _edges;

        private List<double> _costs;

        private int _currentCost;

        private Triangle[] _triangles;

        private CostFunction _costFcn;

        private StopCondition _stopCond;

        private SetNewVertexPosition _setNewVertPos;
        
        private int _k;

        private int _e;

        private int _tCnt;

        private int[] _clusterIdx;

        private int[] _clusterChild;

        private double _progFrac;

        private bool _updateCosts = true;

        private Vector3[] _positions;

        private int _F;

        private bool[] _removed;


        public Collapser(
            Triangle[] triangles, int pointNumber, CostFunction costFunction,
            ConnectComponentsFunction connectFunction, StopCondition stopCondition)
            : this(triangles, pointNumber, costFunction, connectFunction, stopCondition, null, null)
        { }

        public Collapser(
            Triangle[] triangles, int pointNumber, CostFunction costFunction,
            ConnectComponentsFunction connectFunction, StopCondition stopCondition, Vector3[] positions)
            : this(triangles, pointNumber, costFunction, connectFunction, stopCondition, null, positions)
        { }

        public Collapser(
            Triangle[] triangles, int pointNumber, CostFunction costFunction,
            ConnectComponentsFunction connectFunction, StopCondition stopCondition, SetNewVertexPosition newVertPos, Vector3[] positions)
        {

            _tCnt = triangles.Length;
            _triangles = triangles;
            _costFcn = costFunction;
            _stopCond = stopCondition;
            _setNewVertPos = newVertPos;

            _positions = positions;
            _F = positions.Length;

            _costs = new List<double>();

            _currentCost = 0;

            _points = new ConnectedPoint[pointNumber];
            if (_positions == null) for (int i = 0; i < pointNumber; i++) _points[i] = new ConnectedPoint(i);
            else for (int i = 0; i < pointNumber; i++) _points[i] = new ConnectedPoint(i);

            BuildPQ();
            
            _k = pointNumber;

            _e = _edges.Count;

            _clusterIdx = null;

            _clusterChild = null;

            _progFrac = 1.0 / pointNumber;

            _removed = new bool[_points.Length];

        }

        private void BuildPQ()
        {
            ConnectedEdge newEdge;
            ConnectedPoint p1, p2, p3;

            _edges = new EdgeCollapsePQ(_points.Length);

            List<TriangleEx> tris = new List<TriangleEx>();

            foreach (Triangle t in _triangles)
            {
                tris.Add(new TriangleEx(t));
            }

            for (int i = 0; i < tris.Count; i++)
            {
                TriangleEx t = tris[i];
                // get points from indices
                p1 = _points[t.TriangleStruct.V1];
                p2 = _points[t.TriangleStruct.V2];
                p3 = _points[t.TriangleStruct.V3];

                if (!p1.Triangles.Contains(t))
                    p1.Triangles.Add(t);
                if (!p2.Triangles.Contains(t))
                    p2.Triangles.Add(t);
                if (!p3.Triangles.Contains(t))
                    p3.Triangles.Add(t);

                // add neighbor relation between v1 and v2
                if (!p1.Neighbors.Contains(t.TriangleStruct.V2))
                {
                    p1.Neighbors.Add(t.TriangleStruct.V2);
                    p2.Neighbors.Add(t.TriangleStruct.V1);
                }

                // add neighbor relation between v1 and v3
                if (!p1.Neighbors.Contains(t.TriangleStruct.V3))
                {
                    p1.Neighbors.Add(t.TriangleStruct.V3);
                    p3.Neighbors.Add(t.TriangleStruct.V1);
                }

                // add neighbor relation between v2 and v3
                if (!p2.Neighbors.Contains(t.TriangleStruct.V3))
                {
                    p2.Neighbors.Add(t.TriangleStruct.V3);
                    p3.Neighbors.Add(t.TriangleStruct.V2);
                }
            }

            for (int i = 0; i < _points.Length; i++)
            {
                foreach (int j in _points[i].Neighbors)
                {
                    // only from lower index to higher index to create each edge just once
                    if (i < j)
                    {
                        newEdge = CreateEdge(i, j);
                        _edges.Add(newEdge);
                        _points[i].Edges.Add(newEdge);
                        _points[j].Edges.Add(newEdge);
                    }
                }
            }
        }

        private ConnectedEdge CreateEdge(int v1, int v2)
        {            

            // get the points from the indices
            ConnectedPoint p1 = _points[v1];
            ConnectedPoint p2 = _points[v2];

            if (!p1.Neighbors.Contains(v2))
                p1.Neighbors.Add(v2);
            if (!p2.Neighbors.Contains(v1))
                p2.Neighbors.Add(v1);

            ConnectedEdge res = new ConnectedEdge(v1, v2, 0);

            if (_costs.Count <= _currentCost)
            {
                res.Cost = _costFcn(v1, v2, p1.Neighbors, p2.Neighbors);
                if (_updateCosts) _costs.Add(res.Cost);
            }
            else res.Cost = _costs[_currentCost];
            _currentCost++;

            return res;
        }        

        private void CollapseStep()
        {
            // get the edge from the top of the PQ
            ConnectedEdge edge = _edges.Dequeue();

            List<int> neighbors = new List<int>();
            ConnectedPoint p1 = _points[edge.V1];
            ConnectedPoint p2 = _points[edge.V2];
            int pi;

            if (!compatibleNeighbourhoods(p1, p2))
            {                
                return;
            }
            
            p2.ChildPoint = p1.Index;

            _removed[p2.Index] = true;

            if (_setNewVertPos != null)
                _setNewVertPos.Invoke(p2.Index, p1.Index);

            // assign triangles to new point and clear triangles of the second point
            int cnt = 0;
            for (int i = 0; i < p2.Triangles.Count; i++)
            {
                if (!p1.Triangles.Contains(p2.Triangles[i]))
                {
                    Triangle t = p2.Triangles[i].TriangleStruct;
                    if (t.V1 == p2.Index)
                        t.V1 = p1.Index;
                    else if (t.V2 == p2.Index)
                        t.V2 = p1.Index;
                    else if (t.V3 == p2.Index)
                        t.V3 = p1.Index;
                    else
                        throw new Exception("Error - vertex index not found");

                    p2.Triangles[i].TriangleStruct = t;
                    p1.Triangles.Add(p2.Triangles[i]);

                    if ((t.V1 != p1.Index) && (!p1.Neighbors.Contains(t.V1)))
                        p1.Neighbors.Add(t.V1);
                    else if ((t.V2 != p1.Index) && (!p1.Neighbors.Contains(t.V2)))
                        p1.Neighbors.Add(t.V2);
                    else if ((t.V3 != p1.Index) && (!p1.Neighbors.Contains(t.V3)))
                        p1.Neighbors.Add(t.V3);
                    /*else 
                        throw new Exception("Error");*/                    

                }
                else
                {
                    int v3;
                    if (p2.Triangles[i].TriangleStruct.V1 != p1.Index && p2.Triangles[i].TriangleStruct.V1 != p2.Index)
                        v3 = p2.Triangles[i].TriangleStruct.V1;
                    else if (p2.Triangles[i].TriangleStruct.V2 != p1.Index && p2.Triangles[i].TriangleStruct.V2 != p2.Index)
                        v3 = p2.Triangles[i].TriangleStruct.V2;
                    else
                        v3 = p2.Triangles[i].TriangleStruct.V3;
                    ConnectedPoint p3 = _points[v3];
                    p1.Triangles.Remove(p2.Triangles[i]);
                    p3.Triangles.Remove(p2.Triangles[i]);

                    _tCnt--;
                    cnt++;
                }
            }
            //if (cnt != 2)
            //    throw new Exception("Error");
            p2.Triangles.Clear();

            // for all edges going from p1
            foreach (ConnectedEdge e in p1.Edges)
            {
                // find the point at the other end
                if (e.V1 == p1.Index) pi = e.V2;
                else pi = e.V1;

                // remove the edge from that point (including p2)
                _points[pi].Edges.Remove(e);

                // remove the edge from the PQ
                _edges.Remove(e);

                // unless the point is p2, add it to p1's neighbors
                if (pi != p2.Index) neighbors.Add(pi);
            }
            // throw away all the old edges from p1
            p1.Edges.Clear();

            // for all edges going from p2 (the edge to p1 has been already removed)
            foreach (ConnectedEdge e in p2.Edges)
            {
                // find the point at the other end
                if (e.V1 == p2.Index) pi = e.V2;
                else pi = e.V1;

                // remove the edge from that point
                _points[pi].Edges.Remove(e);

                // remove the edge from the PQ
                _edges.Remove(e);

                // add the point to p1's neighbors, unless it's already there
                if (!neighbors.Contains(pi)) neighbors.Add(pi);
            }
            // throw away all the old edges from p2
            p2.Edges.Clear();

            // iterate through the neighbors
            foreach (int p in neighbors)
            {
                // create the updated edge
                edge = CreateEdge(p1.Index, p);

                // add it to p1, the neighboring point and the PQ
                p1.Edges.Add(edge);
                _points[p].Edges.Add(edge);
                _edges.Add(edge);
            }

            // decrease the number of clusters by one
            _k--;
        }

        private bool compatibleNeighbourhoods(ConnectedPoint p1, ConnectedPoint p2)
        {
            int commonNeighbourCount = 0;
            foreach (int n1 in p1.Neighbors)
            {
                if (_removed[n1])
                    continue;
                if (p2.Neighbors.Contains(n1))
                    commonNeighbourCount++;
            }
            if (commonNeighbourCount > 2)
            {
                return (false);
            }
            return(true);
        }

        private void StartSimplification()
        {            
            //double frac = 1.0 / _points.Length;
            double cost;


            // perform the edge decimation until the cost is too high
            _updateCosts = true;
            while (true)
            {
                if (_edges.Count == 0) break;

                cost = _edges.Peek().Cost;
                if (_stopCond(_tCnt, _edges.Count, cost))
                    break;
                
                CollapseStep();
            }

            // create the arrays
            _clusterIdx = new int[_points.Length];
            _clusterChild = new int[_k];

            // put in the cluster indices for the child points
            for (int i = 0, j = 0; i < _points.Length; i++)
            {
                if (_points[i].ChildPoint < 0)
                {
                    _clusterIdx[i] = j;
                    foreach (TriangleEx t in _points[i].Triangles)
                    {
                        Triangle tr = t.TriangleStruct;
                        if (tr.V1 == i)
                            tr.V1 = j;
                        else if (tr.V2 == i)
                            tr.V2 = j;
                        else
                            tr.V3 = j;
                        t.TriangleStruct = tr;
                    }

                    _clusterChild[j] = i;
                    j++;
                }
            }            
        }

        public int[] GetClusterChildren()
        {
            if (_clusterChild == null) StartSimplification();
            return _clusterChild;
        }

        public Triangle[] GetTriangles()
        {
            if (_clusterIdx == null) StartSimplification();

            List<TriangleEx> triangles = new List<TriangleEx>();
            
            foreach (int i in _clusterChild)
            {
                foreach (TriangleEx t in _points[i].Triangles)
                {
                    if (!triangles.Contains(t))
                        triangles.Add(t);
                }
            }

            // prepare the cell array for triangles
            Triangle[] trisCells = new Triangle[triangles.Count];

            // fill the cell array and return it
            for (int i = 0; i < triangles.Count; i++)
            {
                trisCells[i] = triangles[i].TriangleStruct;
            }
            return trisCells;
        }
    }
}
