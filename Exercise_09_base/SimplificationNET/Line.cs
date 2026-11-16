using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zcu.Mve.ZMalik.Simplification
{
    public struct Line
    {
        int v1, v2;

        public int V2
        {
            get { return v2; }
            set { v2 = value; }
        }

        public int V1
        {
            get { return v1; }
            set { v1 = value; }
        }

        public Line(int v1, int v2, bool oriented)
        {
            if (oriented)
            {
                this.v1 = v1;
                this.v2 = v2;
            }
            else
            {
                if (v1 < v2)
                {
                    this.v1 = v1;
                    this.v2 = v2;
                }
                else
                {
                    this.v1 = v2;
                    this.v2 = v1;
                }
            }
        }
    }

}
