using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Gidran;
using Equus.Shire;

namespace Equus.Calabrese
{

    public sealed class FNodeArrayDynamicRef : FNode
    {

        private FNode _RowIndex;
        private FNode _ColIndex;
        private MemoryStruct _Heap;
        private int _DirectRef;

        public FNodeArrayDynamicRef(FNode Parent, FNode Row, FNode Col, MemoryStruct Heap, int DirectRef)
            : base(Parent, FNodeAffinity.MatrixRefNode)
        {
            this._RowIndex = Row;
            this._ColIndex = Col;
            this._DirectRef = DirectRef;
            this._Heap = Heap;
        }

        public FNodeArrayDynamicRef(FNode Parent, FNode Row, FNode Col, MemoryStruct Heap, string Name)
            : this(Parent, Row, Col, Heap, Heap.Arrays.GetPointer(Name))
        { 
        }

        public override Horse.Cell Evaluate()
        {
            return this._Heap.Arrays[this._DirectRef][(int)this._RowIndex.Evaluate().INT, (int)this._ColIndex.Evaluate().INT];
        }

        public override string Unparse(Horse.Schema S)
        {
            return string.Format("Matrix[{0},{1}]", this._RowIndex.Unparse(S), this._ColIndex.Unparse(S));
        }

        public override FNode CloneOfMe()
        {
            return new FNodeArrayDynamicRef(this.ParentNode, this._RowIndex, this._ColIndex, this._Heap, this._DirectRef);
        }

        public override string ToString()
        {
            return "MATRIX";
        }

        public override Horse.CellAffinity ReturnAffinity()
        {
            return this._Heap.Arrays[this._DirectRef].Affinity;
        }

        public override void AssignHeap(MemoryStruct Mem)
        {
            this._Heap = Mem;
        }

        public override int DataSize()
        {
            return this._Heap.Arrays[this._DirectRef].Size;
        }

        public MemoryStruct Heap
        {
            get { return this._Heap; }
            set { this._Heap = value; }
        }

    }

}
