using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;

namespace Equus.Calabrese
{

    public class FNodeEquality : IEqualityComparer<FNode>
    {

        bool IEqualityComparer<FNode>.Equals(FNode T1, FNode T2)
        {

            if (T1.Affinity != T2.Affinity)
                return false;

            if (T1.Affinity == FNodeAffinity.FieldRefNode)
                return (T1 as FNodeFieldRef).Index == (T2 as FNodeFieldRef).Index;

            if (T1.Affinity == FNodeAffinity.PointerNode)
                return (T1 as FNodePointer).PointerName == (T2 as FNodePointer).PointerName;

            //if (T1.Affinity == FNodeAffinity.HeapRefNode)
            //    return (T1 as FNodeHeapRef).HeapRef == (T2 as FNodeHeapRef).HeapRef && (T1 as FNodeHeapRef).Pointer == (T2 as FNodeHeapRef).Pointer;

            if (T1.Affinity == FNodeAffinity.ValueNode)
                return (T1 as FNodeValue).InnerValue == (T2 as FNodeValue).InnerValue;

            return T1.NodeID == T2.NodeID;

        }

        int IEqualityComparer<FNode>.GetHashCode(FNode T)
        {

            if (T.Affinity == FNodeAffinity.FieldRefNode)
                return (T as FNodeFieldRef).Index;

            if (T.Affinity == FNodeAffinity.PointerNode)
                return (T as FNodePointer).PointerName.GetHashCode();

            //if (T.Affinity == FNodeAffinity.HeapRefNode)
            //    return (T as FNodeHeapRef).HeapRef.GetSchema.GetHashCode() ^ (T as FNodeHeapRef).Pointer;

            if (T.Affinity == FNodeAffinity.ValueNode)
                return (T as FNodeValue).InnerValue.GetHashCode();

            return T.NodeID.GetHashCode();

        }

    }

}
