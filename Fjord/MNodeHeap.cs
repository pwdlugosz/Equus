using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Gidran;
using Equus.Horse;
using Equus.Shire;
using Equus.Calabrese;

namespace Equus.Fjord
{
    public sealed class MNodeHeap : MNode
    {

        private int _ref;
        private Heap<CellMatrix> _heap;

        public MNodeHeap(MNode Parent, Heap<CellMatrix> Heap, int Ref)
            : base(Parent)
        {
            this._ref = Ref;
            this._heap = Heap;
        }

        public override CellMatrix Evaluate()
        {
            return this._heap[this._ref];
        }

        public override CellAffinity ReturnAffinity()
        {
            return this._heap[this._ref].Affinity;
        }

        public override MNode CloneOfMe()
        {
            return new MNodeHeap(this.ParentNode, this._heap, this._ref);
        }

    }

}
