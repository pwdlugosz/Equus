using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Calabrese;
using Equus.Shire;
using Equus.Gidran;

namespace Equus.Andalusian
{

    /// <summary>
    /// Base class for TNode; TNodes perform a task, contrast to an FNode which return
    /// </summary>
    public abstract class TNode
    {

        protected TNode _Parent;
        protected List<TNode> _Children;
        protected MemoryStruct _Heap;
        protected int _RaiseElement = 0; // 0 == normal, 1 == break loop, 2 == break main read
        protected Register _reg;

        public TNode(TNode Parent, MemoryStruct Heap)
        {
            this._Parent = Parent;
            this._Children = new List<TNode>();
            this._Heap = Heap;
            this._reg = null;
        }

        public TNode(TNode Parent)
            : this(Parent, new MemoryStruct(true))
        {
        }

        // Properties //
        public TNode Parent
        {
            get { return this._Parent; }
        }

        public List<TNode> Children
        {
            get { return this._Children; }
        }

        public int Raise
        {
            get { return this._RaiseElement; }
            protected set { this._RaiseElement = value; }
        }

        public bool IsMaster
        {
            get { return this._Parent == null; }
        }

        public bool IsTerminal
        {
            get { return this._Children.Count == 0; }
        }

        public bool IsLonely
        {
            get { return this.IsMaster && this.IsTerminal; }
        }

        public MemoryStruct InnerHeap
        {
            get { return this._Heap; }
            set { this._Heap = value; }
        }

        public Register InnerRegister
        {
            get { return this._reg; }
            set { this._reg = value; }
        }

        public bool MustBeLonely
        {
            get;
            protected set;
        }

        public virtual long Writes
        {
            get;
            protected set;
        }

        public bool SupportsMapReduce
        {
            get;
            protected set;
        }

        // Methods //
        public abstract TNode CloneOfMe();

        /// <summary>
        /// Performs an action
        /// </summary>
        public abstract void Invoke();

        /// <summary>
        /// Called once before the first invoke
        /// </summary>
        public virtual void BeginInvoke()
        {

        }

        /// <summary>
        /// Called once after the last invoke
        /// </summary>
        public virtual void EndInvoke()
        {
        }

        /// <summary>
        /// Invokes all children
        /// </summary>
        public virtual void InvokeChildren()
        {
            foreach (TNode n in this._Children)
                n.Invoke();
        }

        /// <summary>
        /// BeginInvokes all children
        /// </summary>
        public virtual void BeginInvokeChildren()
        {
            foreach (TNode n in this._Children)
                n.BeginInvoke();
        }

        /// <summary>
        /// EndInvokes all children
        /// </summary>
        public virtual void EndInvokeChildren()
        {
            foreach (TNode n in this._Children)
                n.EndInvoke();
        }

        /// <summary>
        /// Adds a child node
        /// </summary>
        /// <param name="Node">A single child node to add to the current node</param>
        public void AddChild(TNode Node)
        {
            if (Node.MustBeLonely)
                throw new Exception("This node cannot be an element of a tree");

            Node._Parent = this;
            this._Children.Add(Node);
        }

        /// <summary>
        /// Adds one or more children nodes
        /// </summary>
        /// <param name="Nodes">The collection of nodes to add</param>
        public void AddChildren(params TNode[] Nodes)
        {
            foreach (TNode n in Nodes)
                this.AddChild(n);
        }

        /// <summary>
        /// Raises an element into the parent node; this is really use for exiting loops are read commands
        /// </summary>
        /// <param name="RaiseElement">An integer raise code</param>
        protected void RaiseUp(int RaiseElement)
        {
            this._RaiseElement = RaiseElement;
            if (this._Parent != null)
                this.Parent.RaiseUp(RaiseElement);
        }

        /// <summary>
        /// Returns a message to the user
        /// </summary>
        /// <returns>A string message</returns>
        public virtual string Message()
        {
            return "Action";
        }

        /// <summary>
        /// Assigns a structure to the current node and child nodes
        /// </summary>
        /// <param name="Mem">The memory structure to assign</param>
        public virtual void AssignLocalHeap(MemoryStruct Mem)
        {
            if (this._Heap.IsLocal)
                this._Heap = Mem;
        }

    }

}
