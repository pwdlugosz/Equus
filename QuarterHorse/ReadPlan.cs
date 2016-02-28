using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Calabrese;
using Equus.Shire;
using Equus.Gidran;
using Equus.Nokota;
using Equus.Andalusian;
using Equus.Mustang;
using Equus.HScript;

namespace Equus.QuarterHorse
{

    public sealed class FastReadPlan : CommandPlan
    {

        private DataSet _data;
        private Predicate _where;
        private FNodeSet _return;
        private RecordWriter _output;
        internal long _limit = -1;
        
        public FastReadPlan(DataSet Data, Predicate Where, FNodeSet Fields, RecordWriter Output)
        {
            this._data = Data;
            this._where = Where;
            this._return = Fields;
            this._output = Output;
            this.Name = "READ";
        }

        public override void Execute()
        {

            // Start the clock //
            this._timer = System.Diagnostics.Stopwatch.StartNew();

            // Construct a reader //
            RecordReader reader = this._data.OpenReader(this._where);
            
            // Construct the memory register //
            StreamRegister mem = new StreamRegister(reader);
            this._return.AssignRegister(mem);

            // Build a yield node //
            TNodeAppendTo yeild_node = new TNodeAppendTo(null, this._output, this._return);
            
            // Start the node //
            yeild_node.BeginInvoke();

            // Limiter //
            if (this._limit == -1)
                this._limit = long.MaxValue;

            // Read the data //
            while (!reader.EndOfData)
            {

                // Invoke the yield //
                yeild_node.Invoke();

                // Advance the stream //
                reader.Advance();

                // Limiter //
                if (this._reads >= this._limit)
                    break;

                // Accumulate the reads //
                this._reads++;

            }

            // This will close the stream //
            yeild_node.EndInvoke();

            // Stop the timer //
            this._timer.Stop();

            // Set the write count //
            this._writes = yeild_node.Writes;

            // Message //
            this.Message.AppendLine("Source: '" + this._data.Name + "'");
            this.Message.AppendLine("Reads: " + this._reads.ToString());
            this.Message.AppendLine("Writes: " + this._writes.ToString());

        }

        public static RecordSet Render(DataSet Data, Predicate Where, FNodeSet Fields, long Limit)
        {

            RecordSet rs = new RecordSet(Fields.Columns);
            RecordWriter w = rs.OpenWriter();
            FastReadPlan plan = new FastReadPlan(Data, Where, Fields, w);
            plan._limit = Limit;
            plan.Execute();
            w.Close();
            return rs;

        }

        public static RecordSet Render(DataSet Data, Predicate Where, FNodeSet Fields)
        {
            return Render(Data, Where, Fields, long.MaxValue);
        }

        public static RecordSet Render(DataSet Data, Predicate Where)
        {
            return Render(Data, Where, new FNodeSet(Data.Columns), long.MaxValue);
        }

        public static RecordSet Render(DataSet Data, FNodeSet Fields)
        {
            return Render(Data, Predicate.TrueForAll, Fields, long.MaxValue);
        }

    }

    public sealed class StagedReadName : CommandPlan
    {

        private TNode _Initialize;
        private TNode _Main;
        private TNode _Finalize;
        private RecordReader _Reader;

        public StagedReadName(RecordReader BaseReader, TNode Initial, TNode Main, TNode Final)
        {
            this._Initialize = Initial;
            this._Main = Main;
            this._Finalize = Final;
            this._Reader = BaseReader;
            this.Name = "READ";
        }

        public override void Execute()
        {

            // Start the clock //
            this._timer = System.Diagnostics.Stopwatch.StartNew();

            // Trigger initializer //
            this._Initialize.BeginInvoke();
            this._Initialize.Invoke();
            this._Initialize.EndInvoke();

            // Start main //
            this._Main.BeginInvoke();

            // Go through the stream //
            while (!this._Reader.EndOfData)
            {

                // Invoke //
                this._Main.Invoke();

                // Check for a raise //
                if (this._Main.Raise == 2)
                    break;

                // Increment //
                this._reads++;

                // Advance //
                this._Reader.Advance();

            }

            // Stop main //
            this._Main.EndInvoke();

            // Trigger finalize //
            this._Finalize.BeginInvoke();
            this._Finalize.Invoke();
            this._Finalize.EndInvoke();

            // Stop the timer //
            this._timer.Stop();

            // Append the write count //
            this._writes = this._Main.Writes;

            // Message //
            this.Message.AppendLine("Source: '" + this._Reader.BaseData.Name + "'");
            this.Message.AppendLine("Reads: " + this._reads.ToString());
            this.Message.AppendLine("Writes: " + this._writes.ToString());

        }

    }

    public sealed class ConcurrentReadPlan : CommandPlan
    {

        private MRJob<ReadMapNode> _job;

        public ConcurrentReadPlan(MRJob<ReadMapNode> JOB)
            : base()
        {
            this._job = JOB;
            this.Name = "Map_Reduce_Read";
        }

        public override void Execute()
        {

            this._timer = System.Diagnostics.Stopwatch.StartNew();
            this._job.ExecuteMapsConcurrently();
            this._job.ReduceMaps();
            this._timer.Stop();

        }

    }

    public sealed class ReadMapNode : MapNode
    {

        private TNode _Map;
        private TNode _Reduce;
        private StaticRegister _Memory;
        private Predicate _Where;

        public ReadMapNode(int ID, TNode Map, TNode Reduce, StaticRegister Memory, Predicate Where)
            :base(ID)
        {

            this._Map = Map;
            this._Reduce = Reduce;
            this._Where = Where;
            this._Memory = Memory;

            // Begin invoke //
            this._Map.BeginInvoke();

        }

        public TNode BaseMapAction
        {
            get { return this._Map; }
        }

        public TNode BaseReduceAction
        {
            get { return this._Reduce; }
        }

        public StaticRegister Memory
        {
            get { return this._Memory; }
        }

        public Predicate BaseWhere
        {
            get { return this._Where; }
        }

        public override void Execute(RecordSet Chunk)
        {

            RecordReader rr = Chunk.OpenReader(this._Where);

            while (!rr.EndOfData)
            {
                this._Memory.Assign(rr.ReadNext());
                this._Map.Invoke();
            }

        }

        public override void Close()
        {
            base.Close();
            this._Map.EndInvoke();
        }

    }

    public sealed class ReadMapNodeFactory : MapFactory<ReadMapNode>
    {

        private Workspace _home;
        private HScriptParser.Crudam_read_maprContext _context;
        
        public ReadMapNodeFactory(Workspace Home, HScriptParser.Crudam_read_maprContext context)
            :base()
        {
            this._home = Home;
            this._context = context;
        }

        public override ReadMapNode BuildNew(int PartitionID)
        {
            return CommandCompiler.RenderMapNode(this._home, PartitionID, this._context);
        }

    }

    public sealed class ReadReduceNode : Reducer<ReadMapNode>
    {

        public override void Consume(ReadMapNode Node)
        {

            Node.BaseReduceAction.BeginInvoke();
            Node.BaseReduceAction.Invoke();
            Node.BaseReduceAction.EndInvoke();

        }

    }

}
