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

    public abstract class MNode
    {

        private MNode _ParentNode;
        protected List<MNode> _Cache;

        public MNode(MNode Parent)
        {
            this._ParentNode = Parent;
            this._Cache = new List<MNode>();
        }

        public MNode ParentNode
        {
            get { return _ParentNode; }
            set { this._ParentNode = value; }
        }

        public bool IsMaster
        {
            get { return _ParentNode == null; }
        }

        public bool IsTerminal
        {
            get { return this.Children.Count == 0; }
        }

        public bool IsQuasiTerminal
        {
            get
            {
                if (this.IsTerminal) return false;
                return this.Children.TrueForAll((n) => { return n.IsTerminal; });
            }
        }

        public MNode this[int IndexOf]
        {
            get { return this._Cache[IndexOf]; }
        }

        // Methods //
        public void AddChildNode(MNode Node)
        {
            Node.ParentNode = this;
            this._Cache.Add(Node);
        }

        public void AddChildren(params MNode[] Nodes)
        {
            foreach (MNode n in Nodes)
                this.AddChildNode(n);
        }

        public List<MNode> Children
        {
            get { return _Cache; }
        }

        public CellMatrix[] EvaluateChildren()
        {
            List<CellMatrix> c = new List<CellMatrix>();
            foreach (MNode x in _Cache)
                c.Add(x.Evaluate());
            return c.ToArray();
        }

        public CellAffinity[] ReturnAffinityChildren()
        {
            List<CellAffinity> c = new List<CellAffinity>();
            foreach (MNode x in _Cache)
                c.Add(x.ReturnAffinity());
            return c.ToArray();
        }

        public void Deallocate()
        {
            if (this.IsMaster) return;
            this.ParentNode.Children.Remove(this);
        }

        // Abstracts //
        public abstract CellMatrix Evaluate();

        public abstract CellAffinity ReturnAffinity();

        public abstract MNode CloneOfMe();

    }

}
