using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zcu.Mve.ZMalik.Simplification
{
    public class ConnectedPoint
    {
        private int _index;
        private List<ConnectedEdge> _edges;
        private List<TriangleEx> _triangles;
        private int _child;
        private List<int> _neighbors;

        public int Index
        {
            get { return _index; }
        }

        public List<ConnectedEdge> Edges
        {
            get { return _edges; }
        }

        public List<TriangleEx> Triangles
        {
            get { return _triangles; }
        }

        public int ChildPoint
        {
            get { return _child; }
            set { _child = value; }
        }


        public List<int> Neighbors
        {
            get { return _neighbors; }
        }

        public ConnectedPoint(int index)
        {
            _index = index;
            _child = -1;
            _edges = new List<ConnectedEdge>();
            _triangles = new List<TriangleEx>();
            _neighbors = new List<int>();
        }
    }
}
