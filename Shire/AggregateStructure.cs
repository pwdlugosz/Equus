using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Calabrese;
using Equus.Nokota;

namespace Equus.Shire
{

    /*
    public sealed class RecordKeyValueWriter
    {

        private string _TempDir;
        private List<Header> _Headers;
        private KeyValueSet _Cache;

        public RecordKeyValueWriter(string TempDir, FNodeSet Fields, AggregateSet Reductions)
        {
            this._TempDir = TempDir;
            this._Cache = new KeyValueSet(Fields, Reductions);
            this._Headers = new List<Header>();
        }

        public void Insert()
        {

            // If the cache is full, then dump to disk
            if (this._Cache.IsFull)
            {

                // Create the interim data cache //
                Header h = KeyValueSet.Save(this._TempDir, this._Cache);
                this._Headers.Add(h);

                // Create new record set //
                this._Cache = new KeyValueSet(this._Cache.BaseMappers, this._Cache.BaseReducers);
                
            }

            // Add the record to the cache //
            this._Cache.Add();

        }

        public DataSet FinalDataSet(FNodeSet Fields, Predicate Having)
        {

            // If the header cache is empty, then just return the rendered group by set //
            if (this._Headers.Count == 0)
                return this._Cache.ToFinal(Fields, Having);
            
            // Dump the current set //
            Header h = KeyValueSet.Save(this._TempDir, this._Cache);
            this._Headers.Add(h);

            // Create a table //
            Table t = new Table(this._TempDir, TableHeader.TempName(), this._Cache.OutputSchema);

            // Otherwise, we need to union all the headers //
            for (int i = 0; i < this._Headers.Count - 1; i++)
            {

                KeyValueSet gbs1 = KeyValueSet.Open(this._Headers[i], this._Cache.BaseMappers, this._Cache.BaseReducers);

                for (int j = i + 1; j < this._Headers.Count; j++)
                {

                    KeyValueSet gbs2 = KeyValueSet.Open(this._Headers[j], this._Cache.BaseMappers, this._Cache.BaseReducers);
                    KeyValueSet.Union(gbs1, gbs2);
                    KeyValueSet.Save(this._Headers[j], gbs2);

                }

                // Union in the set //
                t.Union(gbs1.ToFinal(Fields, Having));

            }

            // Drop all headers //
            DataSetManager.DropRecordSet(this._Headers);

            return t;

        }

        public long Render(RecordWriter Output)
        {

            // If the header cache is empty, then just return the rendered group by set //
            if (this._Headers.Count == 0)
            {
                this._Cache.WriteToFinal(Output);
                return (long)this._Cache.Count;
            }

            // Dump the current set //
            this._Headers.Add(KeyValueSet.Save(this._TempDir, this._Cache));

            // Create a table //
            Table t = new Table(this._TempDir, TableHeader.TempName(), this._Cache.OutputSchema);

            // Otherwise, we need to union all the headers //
            long Counter = 0;
            for (int i = 0; i < this._Headers.Count - 1; i++)
            {

                KeyValueSet gbs1 = KeyValueSet.Open(this._Headers[i], this._Cache.BaseMappers, this._Cache.BaseReducers);

                for (int j = i + 1; j < this._Headers.Count; j++)
                {

                    KeyValueSet gbs2 = KeyValueSet.Open(this._Headers[j], this._Cache.BaseMappers, this._Cache.BaseReducers);
                    KeyValueSet.Union(gbs1, gbs2);
                    KeyValueSet.Save(this._Headers[j], gbs2);

                }

                // Union in the set //
                gbs1.WriteToFinal(Output);
                Counter += (long)gbs1.Count;

            }

            // Drop all headers //
            DataSetManager.DropRecordSet(this._Headers);

            return Counter;

        }

        public RecordReader Render(FNodeSet Fields, Predicate Having)
        {
            return this.FinalDataSet(Fields, Having).OpenReader();
        }

        public DataSet Render()
        {

            // If the header cache is empty, then just return the rendered group by set //
            if (this._Headers.Count == 0)
                return this._Cache.ToFinal();

            // Dump the current set //
            Header h = KeyValueSet.Save(this._TempDir, this._Cache);
            this._Headers.Add(h);

            // Create a table //
            Table t = new Table(this._TempDir, TableHeader.TempName(), this._Cache.OutputSchema);

            // Otherwise, we need to union all the headers //
            for (int i = 0; i < this._Headers.Count - 1; i++)
            {

                KeyValueSet gbs1 = KeyValueSet.Open(this._Headers[i], this._Cache.BaseMappers, this._Cache.BaseReducers);

                for (int j = i + 1; j < this._Headers.Count; j++)
                {

                    KeyValueSet gbs2 = KeyValueSet.Open(this._Headers[j], this._Cache.BaseMappers, this._Cache.BaseReducers);
                    KeyValueSet.Union(gbs1, gbs2);
                    KeyValueSet.Save(this._Headers[j], gbs2);

                }

                // Union in the set //
                t.Union(gbs1.ToFinal());

            }

            // Drop all headers //
            DataSetManager.DropRecordSet(this._Headers);

            return t;

        }

    }
    */

    public sealed class AggregateStructure
    {

        private string _TempDir;
        private List<Header> _Headers;
        private KeyValueSet _Cache;
        private FNodeSet _keys;
        private AggregateSet _aggregates;

        // Constructor //
        public AggregateStructure(string TempDir, FNodeSet Fields, AggregateSet Aggregates, List<Header> Headers)
        {
            this._TempDir = TempDir;
            this._Cache = new KeyValueSet(Fields, Aggregates);
            this._Headers = new List<Header>();
            this._keys = Fields;
            this._aggregates = Aggregates;
        }

        public AggregateStructure(string TempDir, FNodeSet Fields, AggregateSet Aggregates)
            :this(TempDir, Fields, Aggregates, new List<Header>())
        {
        }

        // Properties //
        public List<Header> Headers
        {
            get { return this._Headers; }
        }

        // Methods //
        public void Insert()
        {

            // If the cache is full, then dump to disk
            if (this._Cache.IsFull)
            {

                // Create the interim data cache //
                Header h = KeyValueSet.Save(this._TempDir, this._Cache);
                this._Headers.Add(h);

                // Create new record set //
                this._Cache = new KeyValueSet(this._Cache.BaseMappers, this._Cache.BaseReducers);

            }

            // Add the record to the cache //
            this._Cache.Add();

        }

        public void Close()
        {

            // If all in memory, then do nothing //
            if (this._Headers.Count == 0)
                return;

            // otherwise sink the data //
            Header h = KeyValueSet.Save(this._TempDir, this._Cache);
            this._Headers.Add(h);

        }

        // Rendering Methods //
        public DataSet FinalDataSet(FNodeSet Fields, Predicate Having)
        {

            // If the header cache is empty, then just return the rendered group by set //
            if (this._Headers.Count == 0)
                return this._Cache.ToFinal(Fields, Having);

            // Dump the current set //
            Header h = KeyValueSet.Save(this._TempDir, this._Cache);
            this._Headers.Add(h);

            // Create a table //
            Table t = new Table(this._TempDir, TableHeader.TempName(), this._Cache.OutputSchema);

            // Otherwise, we need to union all the headers //
            for (int i = 0; i < this._Headers.Count - 1; i++)
            {

                KeyValueSet gbs1 = KeyValueSet.Open(this._Headers[i], this._Cache.BaseMappers, this._Cache.BaseReducers);

                for (int j = i + 1; j < this._Headers.Count; j++)
                {

                    KeyValueSet gbs2 = KeyValueSet.Open(this._Headers[j], this._Cache.BaseMappers, this._Cache.BaseReducers);
                    KeyValueSet.Union(gbs1, gbs2);
                    KeyValueSet.Save(this._Headers[j], gbs2);

                }

                // Union in the set //
                t.Union(gbs1.ToFinal(Fields, Having));

            }

            // Drop all headers //
            DataSetManager.DropRecordSet(this._Headers);

            return t;

        }

        public long Render(RecordWriter Output)
        {

            // If the header cache is empty, then just return the rendered group by set //
            if (this._Headers.Count == 0)
            {
                this._Cache.WriteToFinal(Output);
                return (long)this._Cache.Count;
            }

            // Dump the current set //
            this._Headers.Add(KeyValueSet.Save(this._TempDir, this._Cache));

            // Create a table //
            Table t = new Table(this._TempDir, TableHeader.TempName(), this._Cache.OutputSchema);

            // Otherwise, we need to union all the headers //
            long Counter = 0;
            for (int i = 0; i < this._Headers.Count - 1; i++)
            {

                KeyValueSet gbs1 = KeyValueSet.Open(this._Headers[i], this._Cache.BaseMappers, this._Cache.BaseReducers);

                for (int j = i + 1; j < this._Headers.Count; j++)
                {

                    KeyValueSet gbs2 = KeyValueSet.Open(this._Headers[j], this._Cache.BaseMappers, this._Cache.BaseReducers);
                    KeyValueSet.Union(gbs1, gbs2);
                    KeyValueSet.Save(this._Headers[j], gbs2);

                }

                // Union in the set //
                gbs1.WriteToFinal(Output);
                Counter += (long)gbs1.Count;

            }

            // Drop all headers //
            DataSetManager.DropRecordSet(this._Headers);

            return Counter;

        }

        public RecordReader Render(FNodeSet Fields, Predicate Having)
        {
            return this.FinalDataSet(Fields, Having).OpenReader();
        }

        public DataSet Render()
        {

            // If the header cache is empty, then just return the rendered group by set //
            if (this._Headers.Count == 0)
                return this._Cache.ToFinal();

            // Dump the current set //
            Header h = KeyValueSet.Save(this._TempDir, this._Cache);
            this._Headers.Add(h);

            // Create a table //
            Table t = new Table(this._TempDir, TableHeader.TempName(), this._Cache.OutputSchema);

            // Otherwise, we need to union all the headers //
            for (int i = 0; i < this._Headers.Count - 1; i++)
            {

                KeyValueSet gbs1 = KeyValueSet.Open(this._Headers[i], this._Cache.BaseMappers, this._Cache.BaseReducers);

                for (int j = i + 1; j < this._Headers.Count; j++)
                {

                    KeyValueSet gbs2 = KeyValueSet.Open(this._Headers[j], this._Cache.BaseMappers, this._Cache.BaseReducers);
                    KeyValueSet.Union(gbs1, gbs2);
                    KeyValueSet.Save(this._Headers[j], gbs2);

                }

                // Union in the set //
                t.Union(gbs1.ToFinal());

            }

            // Drop all headers //
            DataSetManager.DropRecordSet(this._Headers);

            return t;

        }

        // Statics //
        /// <summary>
        /// Merges two aggregate structures
        /// </summary>
        /// <param name="WorkStruct">The structure to be merged into another</param>
        /// <param name="MergeIntoStruc">The structure to be appended</param>
        public static void Merge(AggregateStructure WorkStruct, AggregateStructure MergeIntoStruc)
        {

            // If the merge into structure is all memory based and the work struct is all memory based, try to merge their caches //
            if (MergeIntoStruc._Headers.Count == 0 && WorkStruct._Headers.Count == 0)
            {

                // Combine both cache's //
                KeyValueSet.Union(MergeIntoStruc._Cache, WorkStruct._Cache);
            
                // Check to see if the work structure has any data left //
                if (WorkStruct._Cache.Count == 0)
                    return;

                // Otherwise, we have to 'save' both caches now //
                Header h_mis = KeyValueSet.Save(MergeIntoStruc._TempDir, MergeIntoStruc._Cache);
                Header h_ws = KeyValueSet.Save(WorkStruct._TempDir, WorkStruct._Cache);
                MergeIntoStruc._Headers.Add(h_mis);
                MergeIntoStruc._Headers.Add(h_ws);

            }
            // If the merge into structure has no headers, but the work data does, save the merge into data then add the headers over //
            else if (MergeIntoStruc._Headers.Count == 0 && WorkStruct._Headers.Count != 0)
            {

                Header h_mis = KeyValueSet.Save(MergeIntoStruc._TempDir, MergeIntoStruc._Cache);
                MergeIntoStruc._Headers.Add(h_mis);
                MergeIntoStruc._Headers.AddRange(WorkStruct._Headers);

            }
            // If the merge into structure has headers, but the work doesnt, save the work and add to the merge into collection //
            else if (MergeIntoStruc._Headers.Count != 0 && WorkStruct._Headers.Count == 0)
            {

                Header h_ws = KeyValueSet.Save(WorkStruct._TempDir, WorkStruct._Cache);
                MergeIntoStruc._Headers.Add(h_ws);

            }
            // Otherwise, they both have headers... so just add all the headers into the into structure //
            else
            {
                MergeIntoStruc._Headers.AddRange(WorkStruct._Headers);
            }

        }

        public static void Consolidate(AggregateStructure Data)
        {

            // If the header cache is empty, then just return the rendered group by set //
            if (Data.Headers.Count == 0)
                return;

            // Otherwise, we need to union all the headers //
            for (int i = 0; i < Data._Headers.Count - 1; i++)
            {

                // Open the first data set //
                KeyValueSet gbs1 = KeyValueSet.Open(Data._Headers[i], Data._keys, Data._aggregates);

                for (int j = i + 1; j < Data._Headers.Count; j++)
                {

                    // Open the second set //
                    KeyValueSet gbs2 = KeyValueSet.Open(Data._Headers[j], Data._keys, Data._aggregates);
                    
                    // Merge the two //
                    KeyValueSet.Union(gbs1, gbs2);
                    
                    // Save the second set in case there were deletes //
                    KeyValueSet.Save(Data._Headers[j], gbs2);

                }

                // Save the first set //
                KeyValueSet.Save(Data._Headers[i], gbs1);

            }


        }

        public static RecordSet Render(AggregateStructure Data)
        {

            if (Data._Headers.Count != 0)
                throw new Exception("AggregateStructure cannot render into a RecordSet unless there is only one grouping structure");

            return Data._Cache.ToFinal();

        }

        public static RecordSet Render(AggregateStructure Data, FNodeSet Fields)
        {

            if (Data._Headers.Count != 0)
                throw new Exception("AggregateStructure cannot render into a RecordSet unless there is only one grouping structure");

            return Data._Cache.ToFinal(Fields);

        }

        public static Table Render(AggregateStructure Data, string Dir, string Name, int MaxRecordCount)
        {

            Table t = new Table(Dir, Name, Schema.Join(Data._keys.Columns, Data._aggregates.GetSchema), MaxRecordCount);

            if (Data._Headers.Count == 0)
            {
                RecordSet rs = Data._Cache.ToFinal();
                t.Union(rs);
                return t;
            }

            for (int i = 0; i < Data._Headers.Count; i++)
            {

                KeyValueSet kvs = KeyValueSet.Open(Data._Headers[i], Data._keys, Data._aggregates);
                RecordSet rs = kvs.ToFinal();
                t.Union(rs);

            }

            return t;

        }

        public static Table Render(AggregateStructure Data, string Dir, string Name, int MaxRecordCount, FNodeSet Fields)
        {

            Table t = new Table(Dir, Name, Fields.Columns, MaxRecordCount);

            if (Data._Headers.Count == 0)
            {
                RecordSet rs = Data._Cache.ToFinal(Fields);
                t.Union(rs);
                return t;
            }

            for (int i = 0; i < Data._Headers.Count; i++)
            {

                KeyValueSet kvs = KeyValueSet.Open(Data._Headers[i], Data._keys, Data._aggregates);
                RecordSet rs = kvs.ToFinal(Fields);
                t.Union(rs);

            }

            return t;

        }

        public static long Render(AggregateStructure Data, RecordWriter Output, FNodeSet Fields)
        {

            if (Data._Headers.Count == 0)
            {
                Data._Cache.WriteToFinal(Output);
                return (long)Data._Cache.Count;
            }

            long writes = 0;
            foreach (Header h in Data._Headers)
            {

                KeyValueSet kvs = KeyValueSet.Open(h, Data._keys, Data._aggregates);
                writes += (long)kvs.Count;
                kvs.WriteToFinal(Output, Fields);

            }

            return writes;

        }

    }





}
