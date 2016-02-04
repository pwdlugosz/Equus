using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Shire;

namespace Equus.Calabrese
{

    public sealed class FNodeHeapRef : FNode
    {

        private MemoryStruct _Heap;
        private int _Pointer;
        private CellAffinity _ReturnType;

        public FNodeHeapRef(FNode Parent, MemoryStruct Heap, int DirectRef, CellAffinity ReturnType)
            : base(Parent, FNodeAffinity.HeapRefNode)
        {
            this._Pointer = DirectRef;
            this._Heap = Heap;
            this._ReturnType = ReturnType;
            this._name = Heap.Scalars.Name(DirectRef);
        }

        public FNodeHeapRef(FNode Parent, MemoryStruct Heap, int DirectRef)
            : this(Parent, Heap, DirectRef, Heap.Scalars[DirectRef].AFFINITY)
        {
        }

        public FNodeHeapRef(FNode Parent, MemoryStruct Heap, string Name)
            : this(Parent, Heap, Heap.Scalars.GetPointer(Name), Heap.Scalars[Name].Affinity)
        {
        }

        public override Cell Evaluate()
        {
            return this._Heap.Scalars[this._Pointer];
        }

        public override CellAffinity ReturnAffinity()
        {
            return this._ReturnType;
        }

        public override string ToString()
        {
            return this.Affinity.ToString() + " : " + this._Heap.Scalars[this._Pointer].ToString();
        }

        public override int GetHashCode()
        {
            return this._Heap.Scalars[this._Pointer].GetHashCode() ^ FNode.HashCode(this._Cache);
        }

        public override string Unparse(Schema S)
        {
            return this._Heap.Scalars[this._Pointer].ToString();
        }

        public override FNode CloneOfMe()
        {
            return new FNodeHeapRef(this.ParentNode, this.HeapRef, this._Pointer, this._ReturnType);
        }

        public override void AssignHeap(MemoryStruct Mem)
        {
            this._Heap = Mem;
        }

        public override int DataSize()
        {
            return this._Heap.Scalars[this._Pointer].Size;
        }

        public MemoryStruct HeapRef
        {
            get { return this._Heap; }
        }

        public int Pointer
        {
            get { return this._Pointer; }
        }

        public MemoryStruct Heap
        {
            get { return this._Heap; }
            set { this._Heap = value; }
        }

    }

}
