using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Shire;
using Equus.Calabrese;
using Equus.Nokota;

namespace Equus.Horse
{

    internal sealed class KeyValueSet
    {

        private FNodeSet _Maps;
        private AggregateSet _Reducers;
        private Dictionary<Record, CompoundRecord> _cache = new Dictionary<Record, CompoundRecord>();
        private long _Capacity = RecordSet.DEFAULT_MAX_RECORD_COUNT;

        // Constructor //
        public KeyValueSet(FNodeSet Fields, AggregateSet Aggregates)
        {
            this._Maps = Fields;
            this._Reducers = Aggregates;
            this._cache = new Dictionary<Record, CompoundRecord>(Fields.Columns.NullRecord);
        }

        // Properties //
        public int Count
        {
            get { return this._cache.Count; }
        }

        public bool IsEmpty
        {
            get { return this._cache.Count == 0; }
        }

        public bool IsFull
        {
            get { return this.Count >= this._Capacity; }
        }

        public long Capacity
        {
            get { return this._Capacity; }
            set { this._Capacity = value; }
        }

        public FNodeSet BaseMappers
        {
            get { return this._Maps; }
        }

        public AggregateSet BaseReducers
        {
            get { return this._Reducers; }
        }

        public Schema OutputSchema
        {
            get { return Schema.Join(BaseMappers.Columns, BaseReducers.GetSchema); }
        }

        // Methods //
        public void Add()
        {

            // Check to see if it exists //
            Record r = this._Maps.Evaluate();
            CompoundRecord cr;
            bool b = this._cache.TryGetValue(r, out cr);

            // If exists, then accumulate //
            if (b == true)
            {
                this._Reducers.Accumulate(cr);
            }
            else
            {
                cr = this._Reducers.Initialize();
                this._Reducers.Accumulate(cr);
                this._cache.Add(r, cr);
            }

        }

        /// <summary>
        /// Returns true if it added a new reocrd, false if updated a current record
        /// </summary>
        /// <param name="R"></param>
        /// <param name="CM"></param>
        /// <returns></returns>
        public bool Merge(Record R, CompoundRecord CM)
        {

            // Check to see if it exists //
            CompoundRecord cr;
            bool b = this._cache.TryGetValue(R, out cr);

            // If exists, then merge //
            if (b == true)
            {
                this._Reducers.Merge(CM, cr);
            }
            else
            {
                this._cache.Add(R, CM);
            }
            return !b;

        }

        public void Remove(Record R)
        {
            this._cache.Remove(R);
        }

        // To Methods //
        public RecordSet ToFinal(FNodeSet Fields, Predicate Filter)
        {

            RecordSet rs = new RecordSet(Fields.Columns);
            RecordWriter w = rs.OpenWriter();

            this.WriteToFinal(w, Fields);

            return rs;

        }

        public RecordSet ToFinal(FNodeSet Fields)
        {
            return this.ToFinal(Fields, Predicate.TrueForAll);
        }

        public RecordSet ToFinal()
        {
            Schema s = Schema.Join(this._Maps.Columns, this._Reducers.GetSchema);
            FNodeSet leafs = new FNodeSet(s);
            return ToFinal(leafs);
        }

        public void WriteToFinal(RecordWriter Writter, FNodeSet Fields)
        {

            if (Writter.SourceSchema != Fields.Columns)
                throw new Exception("Base stream and output schema are different");

            // Create a static register //
            StaticRegister reg = new StaticRegister(null);

            // Assign the register to the leaf node set //
            Fields.AssignRegister(reg);

            // Load //
            foreach (KeyValuePair<Record, CompoundRecord> t in this._cache)
            {
                
                // Assign the value to the register //
                reg.Assign(Record.Join(t.Key, this._Reducers.Evaluate(t.Value)));
                
                // Evaluate the record //
                Record r = Fields.Evaluate();

                // Write //
                Writter.Insert(r);

            }

        }

        public void WriteToFinal(RecordWriter Writter)
        {
            Schema s = Schema.Join(this._Maps.Columns, this._Reducers.GetSchema);
            FNodeSet leafs = new FNodeSet(s);
            this.WriteToFinal(Writter, leafs);
        }

        public RecordSet ToInterim()
        {

            // Get schema //
            Schema s = Schema.Join(this._Maps.Columns, this._Reducers.GetInterimSchema);

            // Build the table //
            RecordSet rs = new RecordSet(s);

            // Load //
            foreach (KeyValuePair<Record, CompoundRecord> t in this._cache)
            {
                Record r = Record.Join(t.Key, t.Value.ToRecord());
                rs.Add(r);
            }

            return rs;

        }

        public void ImportFromInterim(RecordSet InterimData)
        {

            int MapperCount = this.BaseMappers.Count;
            int[] Signiture = this.BaseReducers.Signiture;
            int TotalCellCount = MapperCount + Signiture.Sum();

            // Check that this is the correct size //
            if (InterimData.Columns.Count != TotalCellCount)
                throw new Exception(string.Format("RecordSet passed [{0}] has few columns than required by deserializer [{1}]", InterimData.Columns.Count, TotalCellCount));

            // Import the data //
            for (int i = 0; i < InterimData.Count; i++)
            {

                // Build map key //
                RecordBuilder KeyBuilder = new RecordBuilder();
                for (int j = 0; j < MapperCount; j++)
                {
                    KeyBuilder.Add(InterimData[i][j]);
                }

                // Build compound record //
                RecordBuilder ValueBuilder = new RecordBuilder();
                for (int j = MapperCount; j < TotalCellCount; j++)
                {
                    ValueBuilder.Add(InterimData[i][j]);
                }

                // Add to dictionary //
                this._cache.Add(KeyBuilder.ToRecord(), CompoundRecord.FromRecord(ValueBuilder.ToRecord(), Signiture));

            }

        }

        // Statics //
        /// <summary>
        /// Takes all the records from T2 and merges into T1; if the record exists in T1, then:
        /// -- T1's record is updated
        /// -- T2's record is deleted
        /// Otherwise, if the record does not exist in T1, nothing happens
        /// </summary>
        /// <param name="T1"></param>
        /// <param name="T2"></param>
        public static void Union(KeyValueSet T1, KeyValueSet T2)
        {

            List<Record> Deletes = new List<Record>();

            // Merge and tag deletes //
            foreach (KeyValuePair<Record, CompoundRecord> t in T2._cache)
            {
                if (T1._cache.ContainsKey(t.Key))
                {
                    T1.Merge(t.Key, t.Value);
                    Deletes.Add(t.Key);
                }
            }

            // Clear deletes //
            foreach (Record r in Deletes)
                T2._cache.Remove(r);

        }

        public static Header Save(string Dir, KeyValueSet Data)
        {
            RecordSet rs = Data.ToInterim();
            Header h = Header.TempHeader(Dir, rs);
            rs.Attach(h);
            BinarySerializer.FlushRecordSet(rs);
            return h;
        }

        public static void Save(Header H, KeyValueSet Data)
        {
            RecordSet rs = Data.ToInterim();
            rs.Attach(H);
            BinarySerializer.FlushRecordSet(rs);
        }

        public static KeyValueSet Open(Header h, FNodeSet Fields, AggregateSet CR)
        {
            RecordSet rs = BinarySerializer.BufferRecordSet(h.Path);
            KeyValueSet gbs = new KeyValueSet(Fields, CR);
            gbs.ImportFromInterim(rs);
            return gbs;
        }

    }


}
