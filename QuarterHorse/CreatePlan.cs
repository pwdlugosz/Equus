using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.HScript;
using Equus.Fjord;
using Equus.Calabrese;
using Equus.Shire;

namespace Equus.QuarterHorse
{
    
    public sealed class CreateTablePlan : CommandPlan
    {

        private Schema _columns;
        private string _dir;
        private string _name;
        private int _size;

        public CreateTablePlan(string Directory, string Name, Schema Columns, int Size)
            : base()
        {
            this._columns = Columns;
            this._dir = Directory;
            this._name = Name;
            this._size = Size;
            this.Name = "CREATE_TABLE";
        }

        public override void Execute()
        {
            this._timer = System.Diagnostics.Stopwatch.StartNew();
            Table t = new Table(this._dir, this._name, this._columns, this._size);
            this._timer.Stop();
            this.Message.AppendLine(string.Format("Table '{0}' created", this._name));
        }

    }

    public sealed class CreateChunkPlan : CommandPlan
    {

        private string _name;
        private Schema _columns;
        private Workspace _space;

        public CreateChunkPlan(string Name, Schema Columns, Workspace Space)
        {
            this._name = Name;
            this._columns = Columns;
            this._space = Space;
            this.Name = "CREATE_CHUNK";
        }

        public override void Execute()
        {
            this._timer = System.Diagnostics.Stopwatch.StartNew();
            RecordSet chunk = new RecordSet(this._columns);
            chunk.SetGhostName(this._name);
            this._space.ChunkHeap.Reallocate(this._name, chunk);
            this._timer.Stop();
            this.Message.AppendLine(string.Format("Chunk '{0}' created", this._name));
        }

    }

    public sealed class DeclarePlan : CommandPlan
    {

        private List<DeclareNode> _nodes;
        private int _scalarcount = 0;
        private int _matrixcount = 0;
        private MemoryStruct _memory;

        public DeclarePlan(MemoryStruct Memory)
        {
            this._memory = Memory;
            this._nodes = new List<DeclareNode>();
            this.Name = "DECLARE";
        }

        public void Add(string Name, FNode Expression)
        {
            this._scalarcount++;
            this._nodes.Add(new DeclareScalarNode(this._memory, Name, Expression));
        }

        public void Add(string Name, MNode Expression)
        {
            this._matrixcount++;
            this._nodes.Add(new DeclareMatrixNode(this._memory, Name, Expression));
        }

        internal void Add(DeclareNode Node)
        {
            if (Node is DeclareMatrixNode)
                this._matrixcount++;
            else
                this._scalarcount++;
            this._nodes.Add(Node);
        }

        public override void Execute()
        {
            foreach (DeclareNode node in _nodes)
                node.Declare();
            this.Message.AppendLine(string.Format("{0} Scalar object(s) declared", this._scalarcount));
            this.Message.AppendLine(string.Format("{0} Matrix object(s) declared", this._matrixcount));

        }

    }

    public sealed class LambdaPlan : CommandPlan
    {

        private Lambda _lambda;
        private string _name;
        private Heap<Lambda> _heap;

        public LambdaPlan(Heap<Lambda> Heap, string Name, Lambda Expression)
            :base()
        {
            this._heap = Heap;
            this._name = Name;
            this._lambda = Expression;
            this.Name = "LAMBDA";
        }

        public override void Execute()
        {
            this._heap.Reallocate(this._name, this._lambda);
        }

    }

    internal abstract class DeclareNode
    {

        protected MemoryStruct _memory;
        protected string _name;

        public DeclareNode(MemoryStruct Memory, string Name)
        {
            this._memory = Memory;
            this._name = Name;
        }

        public abstract void Declare();

    }

    internal sealed class DeclareScalarNode : DeclareNode
    {

        private FNode _node;

        public DeclareScalarNode(MemoryStruct Memory, string Name, FNode Node)
            : base(Memory, Name)
        {
            this._node = Node;
        }

        public override void Declare()
        {
            this._memory.Scalars.Reallocate(this._name, this._node.Evaluate());
        }

    }

    internal sealed class DeclareMatrixNode : DeclareNode
    {

        private MNode _node;

        public DeclareMatrixNode(MemoryStruct Memory, string Name, MNode Node)
            : base(Memory, Name)
        {
            this._node = Node;
        }

        public override void Declare()
        {
            this._memory.Arrays.Reallocate(this._name, this._node.Evaluate());
        }

    }

}
