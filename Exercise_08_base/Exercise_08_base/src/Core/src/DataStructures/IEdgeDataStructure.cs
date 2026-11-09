using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DataStructures
{
    public interface IEdgeDataStructure
    {
        bool IsOnBorder(int vertexIndex);
        int[] VF(int vertexIndex);
        int[] VV(int vertexIndex);
    }
}
