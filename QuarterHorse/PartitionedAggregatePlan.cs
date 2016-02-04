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

namespace Equus.QuarterHorse
{

    public sealed class PartitionedAggregatePlan : CommandPlan
    {

        private RecordWriter _writer;
        private DataSet _source;
        private Predicate _filter;
        private FNodeSet _keys;
        private AggregateSet _aggregates;
        private FNodeSet _returnset;
        private string _sink;
        private int _PartitionCount;

        // Map-Reduce //
        private AggregateMapFactory _factory;
        private AggregateReducer _reducer;
        private MRJob<AggregateMapNode> _engine;

        public PartitionedAggregatePlan(RecordWriter Output, DataSet Source, Predicate Filter, FNodeSet Keys, AggregateSet Aggregates, FNodeSet ReturnSet,
            string TempDir, int PartitionCount)
            : base()
        {

            this._writer = Output;
            this._source = Source;
            this._filter = Filter;
            this._keys = Keys ?? new FNodeSet();
            this._aggregates = Aggregates;
            this._returnset = ReturnSet;
            this._sink = TempDir ?? Source.Directory;
            this.Name = "PARTITIONED_AGGREGATE";
            this._PartitionCount = PartitionCount;

            this._factory = new AggregateMapFactory(this._sink, this._keys, this._aggregates, this._filter);
            this._reducer = new AggregateReducer();
            this._engine = new MRJob<AggregateMapNode>(Source, this._reducer, this._factory, this._PartitionCount);

        }

        public override void Execute()
        {

            // Get some meta data //
            this.Message.AppendLine(string.Format("Source: {0}", this._source.Name));
            this.Message.AppendLine(string.Format("Keys: {0}", this._keys.Count));
            this.Message.AppendLine(string.Format("Aggregates: {0}", this._aggregates.Count));
            this.Message.AppendLine(string.Format("Partitions: {0}", this._PartitionCount));
            this._timer = System.Diagnostics.Stopwatch.StartNew();

            // Run the maps in parallell //
            this._engine.ExecuteMapsConcurrently();
            //this._engine.ExecuteMapsSequentially();

            // Run the reducer //
            this._engine.ReduceMaps();

            // Output the data //
            this._reducer.WriteTo(this._writer, this._returnset);
            this._writer.Close();

            this._timer.Stop();

            // Set the reads and writes //
            this._reads = this._reducer.Reads;
            this._writes = this._reducer.Writes;

            this.Message.AppendLine(string.Format("Reads: {0}", this._reads));
            this.Message.AppendLine(string.Format("Writes: {0}", this._writes));

        }

    }

    public sealed class AggregateMapNode : MapNode
    {

        private FNodeSet _keys;
        private AggregateSet _aggs;
        private Predicate _where;
        private AggregateStructure _struc;
        private string _dir;
        private long _reads = 0;
        private StaticRegister _memory;

        public AggregateMapNode(int ID, string TempDir, FNodeSet Keys, AggregateSet Aggregates, Predicate Where)
            : base(ID)
        {

            this._keys = Keys;
            this._aggs = Aggregates;
            this._where = Where;
            this._dir = TempDir;
            this._memory = new StaticRegister(null);
            this._keys.AssignRegister(this._memory);
            this._aggs.AssignRegister(this._memory);
            this._struc = new AggregateStructure(TempDir, this._keys, this._aggs);

        }

        public long Reads
        {
            get { return this._reads; }
        }

        public AggregateStructure BaseAggregate
        {
            get { return this._struc; }
        }

        public override void Execute(RecordSet Chunk)
        {

            RecordReader reader = Chunk.OpenReader(this._where);

            while (!reader.EndOfData)
            {

                Record rec = reader.ReadNext();
                this._memory.Assign(rec);
                this._struc.Insert();
                this._reads++;

            }

        }

        public override void Close()
        {
            base.Close();
            this._struc.Close();
        }

    }

    public sealed class AggregateMapFactory : MapFactory<AggregateMapNode>
    {

        private FNodeSet _keys;
        private AggregateSet _aggs;
        private Predicate _where;
        private string _dir;

        public AggregateMapFactory(string Directory, FNodeSet Keys, AggregateSet Aggregates, Predicate Where)
            : base()
        {
            this._dir = Directory;
            this._keys = Keys;
            this._aggs = Aggregates;
            this._where = Where;
        }

        public override AggregateMapNode BuildNew(int PartitionID)
        {
            return new AggregateMapNode(PartitionID, this._dir, this._keys.CloneOfMe(), this._aggs.CloneOfMe(), this._where.CloneOfMe());
        }

    }

    public sealed class AggregateReducer : Reducer<AggregateMapNode>
    {

        private AggregateStructure _struc = null;

        public AggregateReducer()
            : base()
        {
        }

        public AggregateStructure BaseAggregate
        {
            get
            {
                return this._struc;
            }
        }

        public long Reads
        {
            get;
            private set;
        }

        public long Writes
        {
            get;
            private set;
        }

        public override void Consume(AggregateMapNode Node)
        {

            // If this is our first round, set the class value //
            if (this._struc == null)
            {
                this._struc = Node.BaseAggregate;
                this.Reads += Node.Reads;
                return;
            }

            // Otherwise merge //
            AggregateStructure.Merge(Node.BaseAggregate, this._struc);
            this.Reads += Node.Reads;

        }

        public void WriteTo(RecordWriter Writer, FNodeSet Fields)
        {

            this.Writes = AggregateStructure.Render(this._struc, Writer, Fields);

        }

    }

}
