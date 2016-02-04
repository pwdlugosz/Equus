using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;

namespace Equus.Calabrese
{

    public enum FNodeAffinity
    {

        // Ref nodes //
        FieldRefNode,
        HeapRefNode,
        MatrixRefNode,

        // Result nodes //
        ResultNode,

        // Value nodes //
        PointerNode,
        ValueNode

    }

}
