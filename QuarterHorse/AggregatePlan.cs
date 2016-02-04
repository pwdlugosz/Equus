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

    public sealed class AggregatePlan : CommandPlan
    {

        private RecordWriter _writer;
        private DataSet _source;
        private Predicate _filter;
        private FNodeSet _keys;
        private AggregateSet _aggregates;
        private FNodeSet _returnset;
        private StaticRegister _basememory;
        private StaticRegister _returnmemory;
        private string _sink;

        public AggregatePlan(RecordWriter Output, DataSet Source, Predicate Filter, FNodeSet Keys, AggregateSet Aggregates, FNodeSet ReturnSet,
            StaticRegister BaseMem, StaticRegister ReturnMem, string TempDir)
            : base()
        {

            this._writer = Output;
            this._source = Source;
            this._filter = Filter;
            this._keys = Keys ?? new FNodeSet();
            this._aggregates = Aggregates;
            this._returnset = ReturnSet;
            this._basememory = BaseMem;
            this._returnmemory = ReturnMem;
            this._sink = TempDir ?? Source.Directory;
            this.Name = "AGGREGATE";

        }

        public override void Execute()
        {

            // Get some meta data //
            this.Message.AppendLine(string.Format("Source: {0}", this._source.Name));
            this.Message.AppendLine(string.Format("Keys: {0}", this._keys.Count));
            this.Message.AppendLine(string.Format("Aggregates: {0}", this._aggregates.Count));

            // Run //
            this._timer = System.Diagnostics.Stopwatch.StartNew();
            if (this._keys.Count != 0)
                this.ExecuteHashTable();
            else
                this.ExecuteNoTuple();
            this._writer.Close();
            this._timer.Stop();

            this.Message.AppendLine(string.Format("Reads: {0}", this._reads));
            this.Message.AppendLine(string.Format("Writes: {0}", this._writes));
            
        }

        /*
        private void ExecuteHashTable()
        {

            // Build a reader //
            RecordReader BaseReader = this._source.OpenReader(this._filter);

            // Build the aggregate compiler //
            AggregateStructure group_by = new AggregateStructure(this._sink, this._keys, this._aggregates);
            this._reads = 0;
            this._writes = 0;

            // Load the aggregator //
            while (!BaseReader.EndOfData)
            {
                Record rec = BaseReader.ReadNext();
                this._basememory.Assign(rec);
                group_by.Insert(rec);
                this._reads++;
            }

            // Get the record reader //
            RecordReader aggregate_reader = group_by.Render().OpenReader();
            DataSet rs = group_by.Render();

            // Write the data to the output //
            while (!aggregate_reader.EndOfData)
            {

                // Assign //
                this._returnmemory.Assign(aggregate_reader.ReadNext());

                // Increment the ticks //
                this._writes++;

                // Add the record //
                this._writer.Insert(this._returnset.Evaluate());

            }


        }
        */

        private void ExecuteHashTable()
        {

            // Build a reader //
            RecordReader BaseReader = this._source.OpenReader(this._filter);

            // Build the aggregate compiler //
            AggregateStructure group_by = new AggregateStructure(this._sink, this._keys, this._aggregates);
            this._reads = 0;
            this._writes = 0;

            // Load the aggregator //
            while (!BaseReader.EndOfData)
            {
                Record rec = BaseReader.ReadNext();
                this._basememory.Assign(rec);
                group_by.Insert();
                this._reads++;
            }

            // Close the structure //
            group_by.Close();

            // Consolidate //
            AggregateStructure.Consolidate(group_by);

            // Write the data //
            this._writes = AggregateStructure.Render(group_by, this._writer, this._returnset);

            // Drop the tables //
            DataSetManager.DropRecordSet(group_by.Headers);

        }

        private void ExecuteNoTuple()
        {

            RecordReader BaseReader = this._source.OpenReader(this._filter);

            CompoundRecord work_data = this._aggregates.Initialize();
            this._reads = 0;

            while (!BaseReader.EndOfData)
            {
                this._basememory.Assign(BaseReader.ReadNext());
                this._aggregates.Accumulate(work_data);
                this._reads++;
            }

            this._returnmemory.Assign(this._aggregates.Evaluate(work_data));
            this._writer.Insert(this._returnset.Evaluate());

            this._reads = Reads;
            this._writes = 1;

        }

        public static Schema GetInterimSchema(FNodeSet Keys, AggregateSet Aggregates)
        {
            return Schema.Join(Keys.Columns, Aggregates.GetSchema);
        }

        /*
        private DataSet GenerateSortedDataSet(DataSet Data, FNodeSet Keys, AggregateSet Aggregates, Predicate Where)
        {

            // Create the output nodes //
            FNodeSet nodes = new FNodeSet();
            for(int i = 0; i < Keys.Count; i++)
            {
                nodes.Add(Keys.Alias(i), Keys[i].CloneOfMe());
            }
            List<int> indexes = Aggregates.FieldRefs;
            foreach(int i in indexes)
            {
                nodes.Add(Data.Columns.ColumnName(i), new FNodeFieldRef(null, i, Data.Columns.ColumnAffinity(i), null));
            }

            // Create the temp table //
            DataSet t = new RecordSet(nodes.Columns);
            if (Data.IsBig)
            {
                t = new Table(Data.Directory, Header.TempName(), nodes.Columns);
            }

            // Get data //
            RecordWriter w = t.OpenWriter();
            FastReadPlan frp = new FastReadPlan(Data, Where, nodes, w);
            w.Close();

            // Sort the data //
            Key k = Key.Build(Keys.Count);
            t.Sort(k);

            // Return the data //
            return t;


        }

        private void ExecuteSortedSet(RecordWriter Output, RecordReader BaseReader, FNodeSet Keys, AggregateSet Aggregates, FNodeSet ReturnSet,
            StaticRegister BaseMem, StaticRegister ReturnMem)
        {

            CompoundRecord agg_data = Aggregates.Initialize();
            Record key_data = null;
            Record lag_key = null;
            long Reads = 0;
            long Writes = 0;

            while (!BaseReader.EndOfData)
            {

                // Assign the current register //
                BaseMem.Assign(BaseReader.ReadNext());

                // Get the key value //
                key_data = Keys.Evaluate();

                // Check for a key change //
                if (lag_key == null)
                    lag_key = key_data;
                if (!Record.Equals(key_data, lag_key))
                {

                    // Assing the combined records to the register //
                    ReturnMem.Assign(Record.Join(key_data, Aggregates.Evaluate(agg_data)));

                    // Add the record to the output dataset //
                    Output.Insert(ReturnSet.Evaluate());

                    // Reset the aggregate //
                    agg_data = Aggregates.Initialize();

                    // Writes //
                    Writes++;

                }

                // Accumulate the data //
                Aggregates.Accumulate(agg_data);
                Reads++;

            }

            ReturnMem.Assign(Record.Join(key_data, Aggregates.Evaluate(agg_data)));
            Output.Insert(ReturnSet.Evaluate());

            this._reads = Reads;
            this._writes = Writes + 1;

        }
        */

    }

}
