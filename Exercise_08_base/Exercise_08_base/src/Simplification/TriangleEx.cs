using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Framework;

namespace Zcu.Mve.ZMalik.Simplification
{
    public class TriangleEx
    {
        private Triangle _triangle;

        public Triangle TriangleStruct
        {
            get { return _triangle; }
            set { _triangle = value; }
        }

        public TriangleEx(Triangle t)
        {
            _triangle = t;
        }
    }
}
