using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Shire;

namespace Equus.Andalusian
{

    /// <summary>
    /// Represents a tree of TNodes //
    /// </summary>
    public class TNodeSet
    {

        protected List<TNode> _tree;
        protected List<int> _ReturnRefs;

        public TNodeSet()
        {
            this._tree = new List<TNode>();
            this._ReturnRefs = new List<int>();
        }

        public int Count
        {
            get { return this._tree.Count; }
        }

        public bool CheckBreak
        {
            get
            {
                for (int i = 0; i < this.Count; i++)
                {
                    if (this._tree[i].Raise == 2)
                        return true;
                }
                return false;
            }
        }

        public long Writes
        {
            get
            {
                long writes = 0;
                for (int i = 0; i < this._ReturnRefs.Count; i++)
                {
                    TNodeAppendTo node = (this._tree[this._ReturnRefs[i]] as TNodeAppendTo);
                    writes += node.Writes;
                }
                return writes;
            }
        }

        public void Add(TNode Node)
        {
            this._tree.Add(Node);
            if (Node is TNodeAppendTo)
            {
                this._ReturnRefs.Add(this._tree.Count - 1);
            }
        }

        public void AssignRegister(Register Register)
        {
            foreach (TNode t in _tree)
                t.InnerRegister = Register;
        }

        public void AssignNullRegister(Register Register)
        {
            foreach (TNode t in _tree)
            {
                if (t.InnerRegister == null)
                    t.InnerRegister = Register;
            }
        }

        public void Invoke()
        {
            foreach (TNode n in this._tree)
                n.Invoke();
        }

        public void BeginInvoke()
        {
            foreach (TNode n in this._tree)
                n.BeginInvoke();
        }

        public void EndInvoke()
        {
            foreach (TNode n in this._tree)
                n.EndInvoke();
        }

        public void InvokeChildren()
        {
            foreach (TNode n in this._tree)
                n.InvokeChildren();
        }

        public void BeginInvokeChildren()
        {
            foreach (TNode n in this._tree)
                n.BeginInvokeChildren();
        }

        public void EndInvokeChildren()
        {
            foreach (TNode n in this._tree)
                n.EndInvokeChildren();
        }

        public void InvokeAll()
        {
            this.BeginInvoke();
            this.Invoke();
            this.EndInvoke();
        }

        public void InvokeChildrenAll()
        {
            this.BeginInvokeChildren();
            this.InvokeChildren();
            this.EndInvokeChildren();
        }

        public TNodeSet CloneOfMe()
        {

            TNodeSet nodes = new TNodeSet();
            foreach (TNode t in this._tree)
            {
                nodes.Add(t.CloneOfMe());
            }
            return nodes;

        }

        public void AssignLocalHeap(MemoryStruct Mem)
        {
            foreach (TNode t in this._tree)
                t.AssignLocalHeap(Mem);
        }

    }

}
