using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Shire;

namespace Equus.Calabrese
{

    public sealed class FNodeDynamicRef : FNode
    {

        private FNode _idx;
        private CellAffinity _affinity;
        private Register _memory;

        public FNodeDynamicRef(FNode Parent, FNode Index, CellAffinity Affinity, Register MemoryRef)
            : base(Parent, FNodeAffinity.FieldRefNode)
        {
            this._idx = Index;
            this._affinity = Affinity;
            this._memory = MemoryRef;
        }

        public override Cell Evaluate()
        {
            Cell c = this._memory.Value()[(int)this._idx.Evaluate().valueINT];
            if (c.AFFINITY == this._affinity)
                return c;
            return Cell.Cast(c, this._affinity);
        }

        public override CellAffinity ReturnAffinity()
        {
            return _affinity;
        }

        public override string ToString()
        {
            return this.Affinity.ToString() + " : " + this._idx.ToString();
        }

        public override int GetHashCode()
        {
            return this._idx.GetHashCode() ^ FNode.HashCode(this._Cache);
        }

        public override string Unparse(Schema S)
        {
            return string.Format("@R[{0}]", this._idx.Unparse(S));
        }

        public override FNode CloneOfMe()
        {
            return new FNodeDynamicRef(this.ParentNode, this._idx, this._affinity, this._memory);
        }

        public override void AssignRegister(Register Memory)
        {
            this._memory = Memory;
        }

        public override int DataSize()
        {
            return -1;
        }

        public FNode Index
        {
            get { return this._idx; }
        }

        public Register MemoryRegister
        {
            get { return this._memory;}
            set { this._memory = value; }
        }

    }

}
