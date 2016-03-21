using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Equus.Calabrese;
using Equus.Shire;

namespace Equus.Horse
{
    
    public class Table : DataSet
    {

        internal const long DEFAULT_MAX_SIZE = 1000000;
        protected const int OFFSET_ID = 0;
        protected const int OFFSET_COUNT = 1;

        //protected List<Header> _Cache;
        protected RecordSet _Refs; // The move to virtual headers is designed to reduce the memory footprint and make IO faster
        protected Schema _Columns;
        protected TableHeader _Head;
        protected long _MaxRecords = DEFAULT_MAX_SIZE;
        protected Key _OrderBy;
        protected long _RecordCount;
        protected object _lock = new object();

        // Constructors //
        public Table(TableHeader H, Schema S, List<Record> R, Key SortedKeySet)
        {

            //this._Cache = HS;
            this._Columns = S;
            this._Head = H;
            this._MaxRecords = H.MaxRecordCount;
            this._OrderBy = SortedKeySet;
            this._RecordCount = H.RecordCount;
            this._Refs = new RecordSet("ID INT, COUNT INT");
            this._Refs._Cache = R;

        }

        protected Table(string Directory, string Name, Schema S, long MaxRecords, bool Flush)
        {

            TableHeader h = new TableHeader(Directory, Name, 0, S.Count, 0, 0, MaxRecords);

            //this._Cache = new List<Header>();
            this._Columns = S;
            this._Head = h;
            this._MaxRecords = MaxRecords;
            this._OrderBy = new Key();
            this._RecordCount = 0;
            this._Refs = new RecordSet("ID INT, COUNT INT");

            // FlushRecordUnion //
            if (Flush) DataSetManager.DropTable(h);
            if (Flush) BinarySerializer.FlushTable(this);

        }

        /// <summary>
        /// Creates a table, dropping a table of the same name if it exists, and flushes the table to disk
        /// </summary>
        /// <param name="Directory"></param>
        /// <param name="Name"></param>
        /// <param name="S"></param>
        /// <param name="MaxRecords"></param>
        public Table(string Directory, string Name, Schema S, long MaxRecords)
            :this(Directory, Name, S, MaxRecords, true)
        {
        }

        public Table(string Directory, string Name, Schema S)
            :this(Directory, Name, S, DEFAULT_MAX_SIZE)
        {
        }

        // Properties //
        public override int ExtentCount
        {
            get
            {
                return this._Refs.Count;
            }
        }

        public long Count
        {
            get
            {
                return this._RecordCount;
            }
            internal set
            {
                this._RecordCount = value;
            }
        }

        public override Schema Columns
        {
            get
            {
                return this._Columns;
            }
        }

        public TableHeader Header
        {
            get
            {
                return this._Head;
            }
        }

        public IEnumerable<Header> Headers
        {
            get { return new HeaderEnumerable(this); }
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
                return this._MaxRecords;
            }
            set
            {
                if (value < 0) this._MaxRecords = -value;
                this._MaxRecords = value;
            }
        }

        public override bool IsSorted
        {
            get
            {
                return this._OrderBy.Count != 0;
            }
        }

        public override bool IsEmpty
        {
            get { return this.Count == 0; }
        }

        /// <summary>
        /// Returns a new ID based on 1 + the highest current ID
        /// </summary>
        internal long NewID
        {
            get
            {
                return this.LastID + 1;
            }
        }

        public long FirstID
        {
            get 
            {
                if (this.Count == 0) return -1;
                return this._Refs.Min(OFFSET_ID).INT;
            }
        }

        public long LastID
        {
            get
            {
                if (this.ExtentCount == 0) return -1;
                return this._Refs.Max(OFFSET_ID).INT;
            }
        }

        public override string Name
        {
            get { return this.Header.Name; }
        }

        public override string Directory
        {
            get { return this.Header.Directory; }
        }

        public override IEnumerable<RecordSet> Extents
        {
            get 
            {
                return new SetEnumerable(this); 
            }
        }

        public override long CellCount
        {
            get { return this._RecordCount * this._Columns.Count; }
        }

        // IO Methods //
        internal Header GetHeader(long ID)
        {

            if (IDToIndex(ID) == -1)
                throw new Exception(string.Format("ID '{0}' does not exist", ID));

            return this.RenderHeader(ID);
            
        }

        internal bool IDExists(long ID)
        {
            return this._Refs.Seek(new Cell(ID), OFFSET_ID) != -1;
        }

        internal Header RenderHeader(long ID)
        {
            return new Header(this.Header.Directory, this.Header.Name,ID, this.Columns.Count, 0, 0, this.MaxRecords, HeaderType.Fragment);
        }

        internal Header RenderHeaderAt(int Index)
        {
            long id = this._Refs[Index][OFFSET_ID].INT;
            return this.RenderHeader(id);
        }

        internal int IDToIndex(long ID)
        {
            return this._Refs.Seek(new Cell(ID), OFFSET_ID);
        }

        internal long IndexToID(int Index)
        {
            return this._Refs[Index][OFFSET_ID].INT;
        }

        internal long GetRecordCount(long ID)
        {
            return this.GetRecordCountAt(this.IDToIndex(ID));
        }

        internal long GetRecordCountAt(int Index)
        {
            return this._Refs[Index][OFFSET_COUNT].valueINT;
        }

        internal void SetRecordCount(long ID, int Value)
        {
            this.SetRecordCountAt(this.IDToIndex(ID), Value);
        }

        internal void SetRecordCountAt(int Index, long Value)
        {
            this._Refs[Index][OFFSET_COUNT] = new Cell(Value);
        }

        internal void AddNew(long ID, long RecordCount)
        {
            if (IDToIndex(ID) != -1) 
                throw new Exception(string.Format("Fragment with ID '{0}' already exists at '{1}'", ID, IDToIndex(ID)));
            this._Refs.Add(Record.Stitch(new Cell(ID), new Cell(RecordCount)));
            this._RecordCount += RecordCount;
        }

        internal RecordSet ReferenceTable
        {
            get { return this._Refs; }
        }

        /* ## Thread Safe ##*/
        public virtual RecordSet Pop(long ID)
        {

            lock (this._lock)
            {

                // Get the header //
                Header h = this.RenderHeader(ID);

                // Buffer //
                return BinarySerializer.BufferRecordSet(h.Path);
            }

        }

        /* ## Thread Safe ##*/
        public override RecordSet PopAt(int Index)
        {

            lock (this._lock)
            {

                if (this.ExtentCount <= Index)
                    throw new Exception(string.Format("Attempting to pop chunk {0} but this table only has {1} extents", Index, this.ExtentCount));

                // Get the header //
                Header h = this.RenderHeaderAt(Index);

                // Buffer //
                return BinarySerializer.BufferRecordSet(h.Path);
            }

        }

        /* ## Thread Safe ##*/
        /// <summary>
        /// Note: this method requires the following of the RecordSet:
        ///     -- It must be attached
        ///     -- It must have the same schema (names can be different) as the parent
        ///     -- It must have the same name as the parent
        ///     -- It must have an ID present in the parent
        /// </summary>
        /// <param name="DataSet"></param>
        public virtual void Push(RecordSet Data)
        {

            lock (this._lock)
            {

                // Check a few things: the table is attached, columns match, name matches, and the ID exists in the current table //
                if (!Data.IsAttached)
                    throw new Exception("The RecordSet passed is not attached; use the 'Union' method to add a disjoint RecordSet");
                if (Data.Columns.GetHashCode() != this.Columns.GetHashCode())
                    throw new Exception("The schema passed for the current record set does not match the Table");
                if (Data.Header.Name != this.Header.Name)
                    throw new Exception("The 'name' of this RecordSet does not match the parent; use the 'Union' method to add a disjoint RecordSet");
                if (!this.IDExists(Data.Header.ID))
                    throw new Exception("The ID of this RecordSet does not match the parent; use the 'Union' method to add a disjoint RecordSet");

                // Get the current record count //
                long Recs = this.GetRecordCount(Data.Header.ID);

                // Next, update the record count //
                this._RecordCount += (Data.Count - Recs);
                this.SetRecordCount(Data.ID, Data.Count);

                // FlushRecordUnion //
                BinarySerializer.FlushRecordSet(Data);

            }

        }

        /* ## Thread Safe ##*/
        /// <summary>
        /// Add non-existant data to the set 
        /// </summary>
        /// <param name="Data"></param>
        public override void Union(RecordSet Data)
        {

            lock (this._lock)
            {

                // Check that the schema match and that the data set is not too big //
                if (Data.Columns.GetHashCode() != this.Columns.GetHashCode())
                    throw new Exception("The schema passed for the current record set does not match the Table");
                if (Data.Count > this.MaxRecords)
                    throw new Exception("The data set passed is too large");

                // Need to create a new header //
                long id = this.NewID;
                Header h = new Header(this.Header.Directory, this.Header.Name, id, Data, HeaderType.Fragment);

                // Attach //
                Data.Attach(h);

                // Add and dump //
                this.AddNew(id, Data.Count);
                BinarySerializer.FlushRecordSet(Data);

            }

        }

        /* ## Thread Safe ##*/
        public virtual RecordSet Grow()
        {

            lock (this._lock)
            {

                // Build a recordset with our schema //
                RecordSet rs = new RecordSet(this.Columns);
                rs.MaxRecords = this.MaxRecords;
                
                // Get a new ID //
                long id = this.NewID;

                // Build a header //
                Header h = new Header(this.Header.Directory, this.Header.Name, id, rs, HeaderType.Fragment);
                
                // Attach to the record set //
                rs.Attach(h);
                
                // Add to our cache //
                this.AddNew(id, 0);

                // Dump //
                BinarySerializer.FlushRecordSet(rs);

                // Return //
                return rs;
            }

        }

        public virtual RecordSet PopFirst()
        {
            long id = this.FirstID;
            return this.Pop(id);
        }

        public virtual RecordSet PopLast()
        {
            long id = this.LastID;
            return this.Pop(id);
        }

        public virtual RecordSet PopFirstOrGrow()
        {
            long id = this.FirstID;
            if (id == -1) 
                return this.Grow();
            return this.Pop(id);
        }

        public virtual RecordSet PopLastOrGrow()
        {
            long id = this.LastID;
            if (id == -1) return this.Grow();
            return this.Pop(id);
        }

        internal override void Update()
        {

            // Get the correct record count //
            long l = this._Refs.Sum(OFFSET_COUNT).INT;
            this._RecordCount = l;
            this._Head.Update(this);
        }

        public void Import(Table Data)
        {

            for (int i = 0; i < Data.ExtentCount; i++)
            {
                RecordSet rs = Data.PopAt(i);
                this.Union(rs);
            }

        }

        internal virtual void CursorClose(RecordSet Data)
        {

            // Dont flush if zero records otherwise we may run into trouble when we buffer again //
            if (Data.Count == 0)
                return;
            this.Push(Data);
            BinarySerializer.FlushTable(this);

        }

        // Print meta data //
        public void PrintAbout()
        {

            Comm.WriteHeader(this.Header.Name);
            Comm.WriteLine("Directory: {0}", this.Header.Directory);
            Comm.WriteLine("Record Count: {0}", this.Count);
            Comm.WriteLine("RecordSet Count: {0}", this.ExtentCount);
            Comm.WriteLine("Hashcode: {0}", this.GetHashCode());
            Comm.WriteLine("Headers: ");
            foreach (Header h in this.Headers)
                Comm.WriteLine("ID {0} : Count {1}", h.ID, h.RecordCount);
            Comm.WriteLine("Schema:");
            this.Columns.Print();
            Comm.WriteFooter(this.Header.Name);

        }

        public override void Print(int Count)
        {
            if (Count < 0)
                Count = int.MaxValue;
            RecordReader rr = this.OpenReader();
            int ticker = 0;
            Comm.WriteLine("Name: {0}", this.Header.Name);
            Comm.WriteLine("Count: {0}", this.Count);
            Comm.WriteLine(this.Columns.ToNameString('\t'));
            while (!rr.EndOfData && ticker < Count)
            {
                Comm.WriteLine(rr.ReadNext().ToString());
                ticker++;
            }

        }

        public override void Print()
        {
            this.Print((int)this.Count);
        }

        // Sorts //
        public void Sort(Key K, bool Warn)
        {
            Table.SortBase(this, K, Warn);
        }

        public override void Sort(Key K)
        {
            this.Sort(K, false);
        }

        public override void SortDistinct(Key K)
        {

            // Remove duplicates to speed up sort time //
            this.Distinct();

            // Sort //
            this.Sort(K);

        }

        public override void Distinct()
        {

            //Table t = new Table(this.Directory, TableHeader.TempName(), this.Columns);
            //RecordWriter w = t.OpenWriter();
            //QueryFunctions.BaseSelect(w, this, this.Columns.ToLeafNodeSet(), Predicate.TrueForAll, new QuarterHorse.Reducers.ReduceSet(), this.Directory);
            //w.Close();

            //this.Clear();

            //foreach (RecordSet s in t.Extents)
            //    this.Union(s);

            //BinarySerializer.Flush(this);

            //DataSetManager.DropTable(t);

        }

        // Sort support //
        private static void SortEach(Table Data, Key OrderBy)
        {

            foreach (Header h in Data.Headers)
            {

                // Buffer record set //
                RecordSet rs = BinarySerializer.BufferRecordSet(h);

                // Check if it is sorted //
                if (!Key.EqualsStrict(rs.SortBy, OrderBy))
                    rs.Sort(OrderBy);

                // FlushRecordUnion //
                BinarySerializer.FlushRecordSet(rs);

            }

        }

        private static void SortMerge(RecordSet A, RecordSet B, Key Columns)
        {

            // Variables //
            RecordSet x = new RecordSet(A.Columns);
            RecordSet y = new RecordSet(B.Columns);
            x.MaxRecords = A.MaxRecords;
            y.MaxRecords = B.MaxRecords;
            int CompareResult = 0;
            RecordReader c1 = new RecordReader(A);
            RecordReader c2 = new RecordReader(B);

            // Main record loop //
            while (!c1.EndOfData && !c2.EndOfData)
            {

                // Compare results //
                CompareResult = Record.Compare(c1.Read(), c2.Read(), Columns);

                if (CompareResult <= 0 && x.Count < A.Count)
                {
                    x.Add(c1.ReadNext());
                }
                else if (CompareResult > 0 && x.Count < A.Count)
                {
                    x.Add(c2.ReadNext());
                }
                else if (CompareResult <= 0 && x.Count >= A.Count)
                {
                    y.Add(c1.ReadNext());
                }
                else if (CompareResult > 0 && x.Count >= A.Count)
                {
                    y.Add(c2.ReadNext());
                }

            }

            // Clean up first shard //
            while (!c1.EndOfData)
            {
                if (x.Count < A.Count)
                {
                    x.Add(c1.ReadNext());
                }
                else
                {
                    y.Add(c1.ReadNext());
                }
            }

            // Clean up second shard //
            while (!c2.EndOfData)
            {
                if (x.Count < A.Count)
                {
                    x.Add(c2.ReadNext());
                }
                else
                {
                    y.Add(c2.ReadNext());
                }
            }

            // Set the heaps //
            A._Cache = x._Cache;
            B._Cache = y._Cache;

        }

        private static void SortBase(Table Data, Key OrderBy, bool Warn)
        {

            // Variables //
            int ptr_FirstShard = 0;
            int ptr_SecondShard = 0;
            int SortCount = 0;
            int n = (Data.ExtentCount) * (Data.ExtentCount - 1) / 2;
            Stopwatch sw = new Stopwatch();

            // Start time tracking //
            sw.Start();

            // Step one: sort all shards //
            Table.SortEach(Data, OrderBy);

            // Warn //
            if (Warn == true) Comm.WriteLine("Anthology - Sort: Individual shard sort complete - {0} - {1}", Data.ExtentCount, sw.Elapsed);

            // if only one shard, break //
            if (Data.ExtentCount == 1) return;

            // Step two: do cartesian sort n x (n - 1) //
            while (ptr_FirstShard < Data.ExtentCount)
            {

                // Secondary loop //
                ptr_SecondShard = ptr_FirstShard + 1;
                while (ptr_SecondShard < Data.ExtentCount)
                {

                    // Open shards //
                    RecordSet t1 = Data.PopAt(ptr_FirstShard);
                    RecordSet t2 = Data.PopAt(ptr_SecondShard);

                    // Sort merge both shards //
                    Table.SortMerge(t1, t2, OrderBy);

                    // Close both shards //
                    Data.Push(t1);
                    Data.Push(t2);

                    if (Warn == true) 
                        Comm.WriteLine("Anthology - Sort: Merge sort {0} of {1} complete: {2} : {3} x {4}", SortCount, n, sw.Elapsed, ptr_FirstShard + 1, ptr_SecondShard + 1);

                    // Increment //
                    ptr_SecondShard++;
                    SortCount++;

                }

                // Increment //
                ptr_FirstShard++;

            }

            // Tag the anthology's sort key //
            Data._OrderBy = OrderBy;

            // FlushRecordUnion //
            BinarySerializer.FlushTable(Data);

            // End time //
            sw.Stop();
            if (Warn == true) 
                Comm.WriteLine("Sort Time {0}", sw.Elapsed);

        }

        // Shuffle //
        public override void Shuffle(int Seed)
        {
            foreach (RecordSet rs in this.Extents)
            {
                rs.Shuffle(Seed);
                BinarySerializer.FlushRecordSet(rs);
            }
        }

        // Reverse //
        public override void Reverse()
        {
            int i = this.ExtentCount;
            foreach (Header h in this.Headers)
            {
                i--;
                RecordSet rs = BinarySerializer.BufferRecordSet(h);
                rs.Reverse();
                h.ID = i;
                rs.Header.ID = i;
                BinarySerializer.FlushRecordSet(rs);
            }
            this._Refs.Reverse();
            BinarySerializer.FlushTable(this);
        }

        // Clear //
        public override void Clear()
        {
            DataSetManager.DropRecordSet(this.Headers);
            this._Refs.Clear();
            this._RecordCount = 0;
            BinarySerializer.FlushTable(this);
        }

        // Statics //
        public static void Union(Table T1, Table T2)
        {
            foreach (Header h in T2.Headers)
                T1.Union(BinarySerializer.BufferRecordSet(h));
            BinarySerializer.FlushTable(T1);
        }

        // Override //
        public override int GetHashCode()
        {
            int t = 0;
            foreach (Header h in this.Headers)
            {
                RecordSet rs = BinarySerializer.BufferRecordSet(h);
                t += rs.GetHashCode();
            }
            return t;
        }

        public override string ToString()
        {
            return this.Header.Path;
        }

        public override RecordReader OpenReader()
        {
            return new TableReader(this);
        }

        public override RecordReader OpenReader(Predicate P)
        {
            return new TableReader(this, P);
        }

        public override RecordWriter OpenWriter()
        {
            return new TableWriter(this);
        }

        public override bool IsBig
        {
            get { return true; }
        }

        public override RecordSet ToRecordSet
        {
            get { return this.PopFirstOrGrow(); }
        }

        public override Table ToBigRecordSet
        {
            get { return this; }
        }
        
        public override string About
        {
            get { return string.Format("Table | Name: {0} : Records {1} : Extents {2} : Max {3} : CheckSum {4}", this.Name, this.Count, this.ExtentCount, this.MaxRecords, this.GetHashCode()); }
        }

        // Private classes //
        private sealed class SetEnumerator : IEnumerator<RecordSet>, IEnumerator, IDisposable
        {

            private int _idx = -1;
            private Table _t;

            public SetEnumerator(Table Data)
            {
                this._t = Data;
            }

            public bool MoveNext()
            {
                this._idx++;
                return this._idx < this._t.ExtentCount;
            }

            public void Reset()
            {
                this._idx = -1;
            }

            RecordSet IEnumerator<RecordSet>.Current
            {
                get 
                { 
                    return BinarySerializer.BufferRecordSet(this._t.RenderHeaderAt(this._idx)); 
                }
            }

            Object IEnumerator.Current
            {
                get
                {
                    return BinarySerializer.BufferRecordSet(this._t.RenderHeaderAt(this._idx)); 
                }
            }

            public void Dispose()
            { 
            }

        }

        private sealed class SetEnumerable : IEnumerable<RecordSet>, IEnumerable
        {

            private SetEnumerator _e;

            public SetEnumerable(Table Data)
            {
                _e = new SetEnumerator(Data);
            }

            public IEnumerator<RecordSet> GetEnumerator()
            {
                return _e; 
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _e;
            }

        }

        private sealed class HeaderEnumerator : IEnumerator<Header>, IEnumerator, IDisposable
        {

            private int _idx = -1;
            private Table _ref;

            public HeaderEnumerator(Table Data)
            {
                this._ref = Data;
            }

            public bool MoveNext()
            {
                this._idx++;
                return this._idx < this._ref.ExtentCount;
            }

            public void Reset()
            {
                this._idx = -1;
            }

            Header IEnumerator<Header>.Current
            {
                get
                {
                    return this._ref.RenderHeaderAt(this._idx);
                }
            }

            Object IEnumerator.Current
            {
                get
                {
                    return this._ref.RenderHeaderAt(this._idx);
                }
            }

            public void Dispose()
            {
            }

        }

        private sealed class HeaderEnumerable : IEnumerable<Header>, IEnumerable
        {

            private HeaderEnumerator _e;

            public HeaderEnumerable(Table Data)
            {
                _e = new HeaderEnumerator(Data);
            }

            public IEnumerator<Header> GetEnumerator()
            {
                return _e; 
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _e;
            }

        }

    }

}
