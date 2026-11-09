using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Framework;

namespace Zcu.Mve.ZMalik.Simplification
{
    public class ConnectedEdge : IEquatable<ConnectedEdge>
    {
        private Line _edge;
        private double _cost;
        private Int32 _index;

        public int V1
        {
            get { return _edge.V1; }
        }

        public int V2
        {
            get { return _edge.V2; }
        }

        public double Cost
        {
            get { return _cost; }
            set { _cost = value; }
        }

        public Int32 Index
        {
            get { return _index; }
            set { _index = value; }
        }

        public ConnectedEdge(int v1, int v2, double cost)
        {
            _edge = new Line(v1, v2, false);
            _index = -1;
            _cost = cost;
        }


        public bool Equals(ConnectedEdge other)
        {
            return (this._edge.Equals(other._edge));
        }

        public override bool Equals(object obj)
        {
            if (this == null || obj == null || !(obj is ConnectedEdge)) return false;
            return this.Equals((ConnectedEdge)obj);
        }

        public override int GetHashCode()
        {
            return (_edge.GetHashCode());
        }
    }
}
