using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Shire;

namespace Equus.Calabrese
{

    public abstract class FNode
    {

        private FNode _ParentNode;
        private FNodeAffinity _Affinity;
        protected List<FNode> _Cache;
        protected Guid _UID;
        protected string _name;

        public FNode(FNode Parent, FNodeAffinity Affinity)
        {
            this._ParentNode = Parent;
            this._Affinity = Affinity;
            this._Cache = new List<FNode>();
            this._UID = Guid.NewGuid();
            this._name = null;
        }

        public FNode ParentNode
        {
            get { return _ParentNode; }
            set { this._ParentNode = value; }
        }

        public FNodeAffinity Affinity
        {
            get { return _Affinity; }
        }

        public bool IsMaster
        {
            get { return _ParentNode == null; }
        }

        public bool IsTerminal
        {
            get { return this.Children.Count == 0; }
        }

        public bool IsResult
        {
            get { return this._Affinity == FNodeAffinity.ResultNode; }
        }

        public Guid NodeID
        {
            get { return this._UID; }
        }

        public bool IsQuasiTerminal
        {
            get
            {
                if (this.IsTerminal) return false;
                return this.Children.TrueForAll((n) => { return n.IsTerminal; });
            }
        }

        public string Name
        {
            get { return this._name; }
            set { this._name = value; }
        }

        public FNode this[int IndexOf]
        {
            get { return this._Cache[IndexOf]; }
        }

        // Methods //
        public void AddChildNode(FNode Node)
        {
            Node.ParentNode = this;
            this._Cache.Add(Node);
        }

        public void AddChildren(params FNode[] Nodes)
        {
            foreach (FNode n in Nodes)
                this.AddChildNode(n);
        }

        public List<FNode> Children
        {
            get { return _Cache; }
        }

        public Cell[] EvaluateChildren()
        {
            List<Cell> c = new List<Cell>();
            foreach (FNode x in _Cache)
                c.Add(x.Evaluate());
            return c.ToArray();
        }

        public CellAffinity[] ReturnAffinityChildren()
        {
            List<CellAffinity> c = new List<CellAffinity>();
            foreach (FNode x in _Cache)
                c.Add(x.ReturnAffinity());
            return c.ToArray();
        }

        public int[] ReturnSizeChildren()
        {

            List<int> c = new List<int>();
            foreach (FNode x in _Cache)
                c.Add(x.DataSize());
            return c.ToArray();

        }

        public void Deallocate()
        {
            if (this.IsMaster) return;
            this.ParentNode.Children.Remove(this);
        }

        public void Deallocate(FNode Node)
        {
            if (this.IsTerminal) return;
            this._Cache.Remove(Node);
        }

        // Abstracts //
        public abstract string Unparse(Schema S);

        public abstract FNode CloneOfMe();

        public abstract Cell Evaluate();

        public abstract CellAffinity ReturnAffinity();

        // Virtuals //
        public virtual void AssignRegister(Register Memory)
        { 
            // do something 
        }

        public virtual void AssignHeap(MemoryStruct Mem)
        {
        }

        public virtual int DataSize()
        {

            int max = int.MinValue;
            foreach (FNode n in this._Cache)
            {
                max = Math.Max(n.DataSize(), max);
            }
            return max;

        }

        // Statics //
        public static FNode BuildParent(FNode L, CellFunction F)
        {
            FNode n = new FNodeResult(null, F);
            n.AddChildNode(L);
            return n;
        }

        public static int HashCode(List<FNode> Cache)
        {
            int h = int.MaxValue;
            foreach (FNode q in Cache)
                h = h ^ q.GetHashCode();
            return h;
        }

        // Opperators //
        public static FNode operator +(FNode Left, FNode Right)
        {
            FNodeResult t = new FNodeResult(null, new CellBinPlus());
            t.AddChildren(Left, Right);
            return t;
        }

        public static FNode operator -(FNode Left, FNode Right)
        {
            FNodeResult t = new FNodeResult(null, new CellBinMinus());
            t.AddChildren(Left, Right);
            return t;
        }

        public static FNode operator *(FNode Left, FNode Right)
        {
            FNodeResult t = new FNodeResult(null, new CellBinMult());
            t.AddChildren(Left, Right);
            return t;
        }

        public static FNode operator /(FNode Left, FNode Right)
        {
            FNodeResult t = new FNodeResult(null, new CellBinDiv());
            t.AddChildren(Left, Right);
            return t;
        }

        public static FNode operator %(FNode Left, FNode Right)
        {
            FNodeResult t = new FNodeResult(null, new CellBinMod());
            t.AddChildren(Left, Right);
            return t;
        }

        public static FNode operator ^(FNode Left, FNode Right)
        {
            FNodeResult t = new FNodeResult(null, new CellFuncFVPower());
            t.AddChildren(Left, Right);
            return t;
        }


    }

}
