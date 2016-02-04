using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Calabrese;
using Equus.Shire;

namespace Equus.Horse
{
    
    public class RecordSet : DataSet
    {

        internal const long DEFAULT_MAX_RECORD_COUNT = 10000000;
        internal const long DEFAULT_MEMORY_FOOTPRINT = 65536; // = 64 mb in kb
        internal const long ESTIMATE_META_DATA = 4096; // estimate 4 kb in meta data
        
        internal List<Record> _Cache;
        protected Schema _Columns;
        protected Header _Head;
        protected long _MaxRecordCount = -1;
        protected Key _OrderBy;
        protected bool _Fix = true;
        protected string _GhostName; // only used when recordset is in memory only
        protected int _DataLen = 0;
        
        // Constructor //
        public RecordSet(Schema NewColumns, Header NewHeader, List<Record> NewCache, Key NewOrderBy)
        {
            this._Columns = NewColumns;
            this._Cache = NewCache;
            this._OrderBy = NewOrderBy;
            this._Head = NewHeader;
            if (NewHeader != null)
            {
                this._MaxRecordCount = NewHeader.MaxRecordCount;
                this._GhostName = NewHeader.Name;
            }
            else
            {
                this._MaxRecordCount = EstimateMaxRecords(NewColumns);
                this._GhostName = "CHUNK";
            }
        }

        public RecordSet(Schema NewColumns, Header NewHeader)
            : this(NewColumns, NewHeader, new List<Record>(), new Key())
        {

        }

        public RecordSet(string ColumnText, Header NewHeader)
            : this(new Schema(ColumnText), NewHeader)
        {
        }

        public RecordSet(Schema NewColumns)
            : this(NewColumns, null)
        {
        }
        
        public RecordSet(string ColumnText)
            : this(ColumnText, null)
        {
        }

        public RecordSet(string Directory, string Name, Schema S, long MaxRecords)
            : this(S)
        {
            this.MaxRecords = MaxRecords;
            Header h = new Header(Directory, Name, 0, this, HeaderType.Table);
            this.Attach(h);
            BinarySerializer.FlushRecordSet(this);
        }

        public RecordSet(string Directory, string Name, Schema S)
            : this(Directory, Name, S, EstimateMaxRecords(S))
        {
        }

        // DataSet Override Properties //
        public override Schema Columns
        {
            get
            {
                return this._Columns;
            }
        }

        public override Key SortBy
        {
            get
            {
                return this._OrderBy;
            }
        }

        public override long MaxRecords
        {
            get
            {
                return this._MaxRecordCount;
            }
            set
            {
                if (value < 0) this._MaxRecordCount = -value;
                this._MaxRecordCount = value;
            }
        }

        public override string Name
        {
            get 
            {
                if (this.IsAttached)
                    return this.Header.Name;
                else
                    return this._GhostName;
            }
        }

        public override string Directory
        {
            get
            {
                if (this.IsAttached)
                    return this.Header.Directory;
                else
                    return null;
            }
        }

        public override bool IsEmpty
        {
            get
            {
                return this.Count == 0;
            }
        }

        public override bool IsSorted
        {
            get
            {
                return this._OrderBy.Count != 0;
            }
        }

        public override IEnumerable<RecordSet> Extents
        {
            get
            {
                List<RecordSet> lts = new List<RecordSet>();
                lts.Add(this);
                return lts;
            }
        }

        public override int ExtentCount
        {
            get { return 1; }
        }

        public override long CellCount
        {
            get { return this._Cache.Count * this._Columns.Count; }
        }

        // Non Overide Properties //
        public Header Header
        {
            get
            {
                return this._Head;
            }
        }

        public bool IsFull
        {
            get
            {
                return this.Count == this._MaxRecordCount;
            }
        }
        
        public int Count
        {
            get
            {
                return this._Cache.Count;
            }
        }

        public bool IsFixing
        {
            get
            {
                return this._Fix;
            }
            set
            {
                this._Fix = value;
            }
        }

        public bool IsAttached
        {
            get
            {
                return !(this._Head == null);
            }
        }

        public Record this[int Index]
        {
            get
            {
                return this._Cache[Index];
            }
            set
            {
                this._Cache[Index] = value;
            }
        }

        public long ID
        {
            get
            {
                if (this.IsAttached) return this.Header.ID;
                return 0;
            }
        }

        public int Size
        {
            get
            {
                return this._Cache.Sum((x) => { return x.Size; });
            }
        }

        // Methods //
        public void Add(Record Data)
        {
            
            if (this.IsFull) 
                throw new Exception("RecordSet is full");
            if (!this.Columns.Check(Data, true))
                throw new Exception("Record passed does not match schema");
            this._Cache.Add(Data);

        }

        public void AddData(params object[] Data)
        {
            Record r = Record.TryUnboxInto(this._Columns, Data);
            this.Add(r);
        }

        public Record Get(int Index)
        {
            return this[Index];
        }

        public void Set(int Index, Record Data)
        {
            this[Index] = Data;
        }

        public void Remove(int Index)
        {
            this._Cache.RemoveAt(Index);
        }

        // Seek / contains //
        public int Seek(Record R)
        {

            // Toss this in incase we seek over no records //
            if (this.Count == 0) return -1;

            int i = 0, j = this.Count - 1;
            while (j >= i)
            {
                if (Record.Compare(this[i], R) == 0) return i;
                if (Record.Compare(this[j], R) == 0) return j;
                i++; 
                j--;
            }
            return -1;
        }

        public int Seek(Record R, Key K)
        {

            // Toss this in incase we seek over no records //
            if (this.Count == 0) return -1;
            
            int i = 0, j = this.Count - 1;
            //Console.WriteLine("i {0} : j {1}", i, j);

            while (j >= i)
            {
                if (Record.Compare(this[i], R, K) == 0) return i;
                if (Record.Compare(this[j], R, K) == 0) return j;
                i++;
                j--;
            }
            return -1;

        }

        public int Seek(Cell C, int Index)
        {
            // Toss this in incase we seek over no records //
            if (this.Count == 0) return -1;

            int i = 0, j = this.Count - 1;
            //Console.WriteLine("i {0} : j {1}", i, j);

            while (j >= i)
            {
                if (this[i][Index] == C) return i;
                if (this[j][Index] == C) return j;
                i++;
                j--;
            }
            return -1;
        }

        public bool Contains(Record R)
        {
            return this.Seek(R) == -1;
        }

        public bool Contains(Record R, Key K)
        {
            return this.Seek(R, K) == -1;
        }

        public Cell Sum(int Index)
        {
            Cell c = Cell.ZeroValue(this.Columns.ColumnAffinity(Index));
            foreach (Record r in this._Cache)
                if (!r[Index].IsNull) c += r[Index];
            return c;
        }

        public Cell Min(int Index)
        {
            Cell c = new Cell(this.Columns.ColumnAffinity(Index));
            foreach (Record r in this._Cache)
            {
                if (!r[Index].IsNull)
                {
                    if (c.IsNull) 
                        c = r[Index];
                    else if (r[Index] < c)
                        c = r[Index];
                }

            }
            return c;
        }

        public Cell Max(int Index)
        {
            Cell c = new Cell(this.Columns.ColumnAffinity(Index));
            foreach (Record r in this._Cache)
            {
                if (!r[Index].IsNull)
                {
                    if (c.IsNull)
                        c = r[Index];
                    else if (r[Index] > c)
                        c = r[Index];
                }

            }
            return c;
        }

        // Changes //
        public void Swap(int Index1, int Index2)
        {
            Record r = this[Index1];
            this[Index1] = this[Index2];
            this[Index2] = r;
        }

        // Internals //
        internal void PrintStatistics()
        {
            
            if (this.IsAttached)
            {
                Comm.WriteLine("Name: {0}", this.Header.Name);
                Comm.WriteLine("ID: {0}", this.Header.ID);
                Comm.WriteLine("Directory: {0}", this.Header.Directory);
            }

            Comm.WriteLine("Record Count: {0}", this.Count);
            Comm.WriteLine("Max Count: {0}", this.MaxRecords);
            Comm.WriteLine("ColumnCount: {0}", this.Columns.Count);
            Comm.WriteLine("Fixing On: {0}", this.IsFixing);
            if (this.IsSorted)
            {
                Comm.WriteLine("Sort Columns");
                for (int i = 0; i < this.SortBy.Count; i++)
                {
                    Comm.WriteLine("\t{0}", this.Columns.ColumnName(this.SortBy[i]));
                }
            }

        }

        internal void Attach(Header Meta)
        {
            this._Head = Meta;
        }

        internal void Import(RecordSet RS)
        {

            RS.Columns.Print();
            Console.WriteLine("----------------");
            this.Columns.Print();

            if (RS.Columns.GetHashCode() != this.Columns.GetHashCode())
                throw new Exception("RecordSets have incompatible schemas " + RS.Columns.GetHashCode().ToString() + " : " + this.GetHashCode());
            this._Cache.AddRange(RS._Cache);
        }

        // DataSet Overrides //
        internal override void Update()
        {
            if (!this.IsAttached) 
                return;
            this._Head.Update(this);
        }

        public override RecordSet PopAt(int Index)
        {
            return this;
        }
        
        public override void Print(int N)
        {

            Console.WriteLine(this.Columns.ToNameString());
            Console.WriteLine(this.Columns.ToAffinityString());

            N = Math.Min(this.Count, N);
            for (int i = 0; i < N; i++)
                Comm.WriteLine(this[i].ToString());
        }

        public override void Print()
        {
            this.Print(this.Count);
        }
        
        public override string ToString()
        {
            if (this.IsAttached) return this.Header.Name;
            return base.ToString();
        }

        public override int GetHashCode()
        {
            long l = this._Cache.Sum<Record>((r) => { return (long)(r.GetHashCode() & sbyte.MaxValue); });
            return (int)l;
        }

        public override RecordReader OpenReader()
        {
            return new RecordReader(this);
        }

        public override RecordReader OpenReader(Predicate P)
        {
            return new RecordReader(this, P);
        }

        public override RecordWriter OpenWriter()
        {
            return new RecordWriter(this);
        }

        public override bool IsBig
        {
            get { return false; }
        }

        public override RecordSet ToRecordSet
        {
            get { return this; }
        }

        public override Table ToBigRecordSet
        {
            get 
            {
                if (!this.IsAttached)
                    throw new NullReferenceException("ToBigRecordSet cannot be called from a record set that is not attached");
                return new Table(this.Header.Directory, this.Header.Name, this.Columns, this.MaxRecords);
            }
        }

        public override string About
        {
            get { return string.Format("RecordSet | Name: {0} : Records {1} : CheckSum {2}", this.Name, this.Count, this.GetHashCode()); }
        }

        // Mutations //
        public override void Sort(Key K)
        {
            this._OrderBy = K;
            this._Cache.Sort((x, y) => { return Record.Compare(x, y, K); });
        }

        public override void Reverse()
        {
            this._OrderBy = new Key();
            this._Cache.Reverse();
            //if (this.IsAttached) BinarySerializer.Flush(this);
        }

        public override void Clear()
        {
            this._OrderBy = new Key();
            this._Cache.Clear();
        }

        public override void Shuffle(int Seed)
        {
            this._OrderBy = new Key();
            Random r = new Random(Seed);
            int n = this.Count;
            for (int i = 0; i < this.Count; i++)
                this.Swap(i, r.Next(n));
            //if (this.IsAttached) BinarySerializer.Flush(this);
        }

        public override void Distinct()
        {
            this._OrderBy = new Key();
            this._Cache = this._Cache.Distinct().ToList();
        }

        public override void SortDistinct(Key K)
        {
            this._OrderBy = K;
            this._Cache = this._Cache.Distinct().ToList();
            this._Cache.Sort((x, y) => { return Record.Compare(x, y, K); });
        }

        public override void Union(RecordSet Data)
        {
            this._Cache.AddRange(Data._Cache);
        }

        internal void SetGhostName(string Name)
        {
            if (this.IsAttached)
                return;
            this._GhostName = Name;
        }

        // Statics //
        public static long EstimateMaxRecords(Schema Columns, long TotalMemoryFootPrintKB)
        {
            return (TotalMemoryFootPrintKB * 1024 - ESTIMATE_META_DATA) / Columns.RecordLength;
        }

        public static long EstimateMaxRecords(Schema Columns)
        {
            return EstimateMaxRecords(Columns, DEFAULT_MEMORY_FOOTPRINT);
        }

    }

}
