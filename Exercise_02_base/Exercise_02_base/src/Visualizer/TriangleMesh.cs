using Core.Structures;
using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Visualizer
{
    internal class Edge
    {
        public int faceIncident = -1;
        public int edgeRefNext = -1, edgeRefPrev = -1, edgeRefOpposite = -1;
        public int vertexRefEnd = -1;
        public int vertexRefStart = -1; // only for eq???

        public Edge(int v1Idx, int v2Idx)
        {
            vertexRefEnd = v2Idx;
            vertexRefStart = v1Idx;
        }
        public override bool Equals(object? obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }

            
            var other = obj as Edge;

            return vertexRefEnd == other!.vertexRefEnd && vertexRefStart == other!.vertexRefStart;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(vertexRefEnd, vertexRefStart);
            //return vertexRefEnd;
        }

    }
    internal class Face
    {
        public int halfEdge = -1;

        public Face(int halfEdgeIdx)
        {
            halfEdge = halfEdgeIdx;
        }
    }

    internal class TriangleMesh
    {
        public Vertex[] vertices; // truth for vertices
        public int[] incidentEdges; // true for incident edges for each vertex
        public List<Edge> edges = new(); // truth for edges
        public List<Face> faces = new(); // truth for triangles
        public Dictionary<Edge, int> dict = new Dictionary<Edge, int>(); // only for init
        public TriangleMesh(int nVertices)
        {
            vertices = new Vertex[nVertices];
            incidentEdges = new int[nVertices];
        }

        private void AddTriangle(int v1, int v2, int v3, int faceIdx)
        {
            int e1 = AddHalfEdge(v1, v2);
            int e2 = AddHalfEdge(v2, v3);
            int e3 = AddHalfEdge(v3, v1);

            // next
            edges[e1].edgeRefNext = e2;
            edges[e2].edgeRefNext = e3;
            edges[e3].edgeRefNext = e1;

            // prev
            edges[e1].edgeRefPrev = e3;
            edges[e2].edgeRefPrev = e1;
            edges[e3].edgeRefPrev = e2;

            // face
            edges[e1].faceIncident = faceIdx;
            edges[e2].faceIncident = faceIdx;
            edges[e3].faceIncident = faceIdx;
        }
        private int AddHalfEdge(int start, int end) {
            var edge = new Edge(start, end);

            if (dict.TryGetValue(edge, out int existing))
            {
                return existing;
            }

            // ORIENTED
            int index = edges.Count;
            edges.Add(edge);
            dict.Add(edge, index);

            // OPPOSITE
            var opposite = new Edge(end, start);

            int oppositeIndex = edges.Count;
            edges.Add(opposite);
            dict.Add(opposite, oppositeIndex);

            // LINK THEM
            edges[index].edgeRefOpposite = oppositeIndex;
            edges[oppositeIndex].edgeRefOpposite = index;

            // REWRITE INCIDENCE
            incidentEdges[end] = index;
            incidentEdges[start] = oppositeIndex;

            return index;
        }
        public void initDataStructure(Vertex[] verts, int[] indices) {
            vertices = (Vertex[])verts.Clone();

            for (int i = 0; i < incidentEdges.Length; i++)
            {
                incidentEdges[i] = 0;
            }


            for (int i = 0; i < indices.Length; i += 3) // TRIANGLES
            {
                int i1 = indices[i];
                int i2 = indices[i + 1];
                int i3 = indices[i + 2];

                int faceIdx = i / 3;

                AddTriangle(i1, i2, i3, faceIdx);
            }

            dict = new Dictionary<Edge, int>(); // delete
        }

        // return array of ints, which are all triangles incident with vertex
        public int[] VFincidenceQuery(int vertexIdx)
        {
            var incFaces = new List<int>();

            var incEdge = incidentEdges[vertexIdx];
            var originalEdge = edges[incEdge];
            var originalOppositeEdge = edges[originalEdge.edgeRefOpposite];
            var edge = edges[incEdge];

            while (true)
            {
                
                if (edge.vertexRefEnd != vertexIdx)
                {
                    edge = edges[edge.edgeRefOpposite];
                }
                incFaces.Add(edge.faceIncident);

                edge = edges[edge.edgeRefNext];
                if (edge.Equals(originalEdge) || edge.Equals(originalOppositeEdge))
                {
                    break;
                }
            }

            return incFaces.ToArray();
        }

        public int[] VVincidenceQuery(int vertexIdx)
        {
            var incVerts = new List<int>();

            var incEdge = incidentEdges[vertexIdx];
            var originalEdge = edges[incEdge];
            var originalOppositeEdge = edges[originalEdge.edgeRefOpposite];
            var edge = edges[incEdge];

            while (true)
            {

                if (edge.vertexRefEnd != vertexIdx)
                {
                    incVerts.Add(edge.vertexRefEnd);
                    edge = edges[edge.edgeRefOpposite];
                }
                else
                {
                    var opp_e = edges[edge.edgeRefOpposite];
                    incVerts.Add(opp_e.vertexRefEnd);
                }
                

                edge = edges[edge.edgeRefNext];
                if (edge.Equals(originalEdge) || edge.Equals(originalOppositeEdge))
                {
                    break;
                }
            }

            return incVerts.ToArray();
        }

        // better impl
        public int[] VFincidenceQueryWithBounds(int vertexIdx)
        {
            var incFaces = new List<int>();

            var incEdge = incidentEdges[vertexIdx];
            var originalEdge = edges[incEdge];
            var originalOppositeEdge = edges[originalEdge.edgeRefOpposite];
            var edge = edges[incEdge];

            while (true)
            {

                if (edge.vertexRefEnd != vertexIdx)
                {
                    edge = edges[edge.edgeRefOpposite];
                }
                incFaces.Add(edge.faceIncident);

                if (edge.edgeRefNext == -1)
                {
                    break;
                }

                edge = edges[edge.edgeRefNext];
                if (edge.Equals(originalEdge) || edge.Equals(originalOppositeEdge))
                {
                    return incFaces.ToArray();
                }
            }

            edge = originalEdge;

            while (true)
            {

                if (edge.vertexRefEnd == vertexIdx)
                {
                    edge = edges[edge.edgeRefOpposite];
                }
                incFaces.Add(edge.faceIncident);

                if (edge.edgeRefPrev == -1)
                {
                    break;
                }

                edge = edges[edge.edgeRefPrev];
                if (edge.Equals(originalEdge) || edge.Equals(originalOppositeEdge))
                {
                    return incFaces.ToArray();
                }
            }

            return incFaces.ToArray();
        }


        public int[] VVincidenceQueryWithBounds(int vertexIdx)
        {
            var incVerts = new List<int>();

            var incEdge = incidentEdges[vertexIdx];
            var originalEdge = edges[incEdge];
            var originalOppositeEdge = edges[originalEdge.edgeRefOpposite];
            var edge = edges[incEdge];

            while (true)
            {

                if (edge.vertexRefEnd != vertexIdx)
                {
                    incVerts.Add(edge.vertexRefEnd);
                    edge = edges[edge.edgeRefOpposite];
                }
                else
                {
                    var opp_e = edges[edge.edgeRefOpposite];
                    incVerts.Add(opp_e.vertexRefEnd);
                }

                if (edge.edgeRefNext == -1)
                {
                    break;
                }

                edge = edges[edge.edgeRefNext];
                if (edge.Equals(originalEdge) || edge.Equals(originalOppositeEdge))
                {
                    return incVerts.ToArray();
                }
            }

            edge = originalEdge;

            while (true)
            {

                if (edge.vertexRefEnd == vertexIdx)
                {
                    incVerts.Add(edge.vertexRefEnd);
                    edge = edges[edge.edgeRefOpposite];
                }
                else
                {
                    var opp_e = edges[edge.edgeRefOpposite];
                    incVerts.Add(opp_e.vertexRefEnd);
                }

                if (edge.edgeRefPrev == -1)
                {
                    break;
                }

                edge = edges[edge.edgeRefPrev];
                if (edge.Equals(originalEdge) || edge.Equals(originalOppositeEdge))
                {
                    return incVerts.ToArray();
                }
            }

            return incVerts.ToArray();
        }
    }
}
