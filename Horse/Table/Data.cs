using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Shire;
using Equus.Calabrese;

namespace Equus.Horse
{

    public abstract class DataSet 
    {

        // Properties //
        public abstract Schema Columns { get; }

        public abstract Key SortBy { get; }

        public abstract bool IsSorted { get; }

        public abstract long MaxRecords { get; set; }

        public abstract string Name { get; }

        public abstract string Directory { get; }

        public abstract IEnumerable<RecordSet> Extents { get; }

        public abstract int ExtentCount { get; }

        public abstract RecordSet PopAt(int Index);

        public abstract bool IsEmpty { get; }

        // Methods //
        public abstract void Sort(Key K);

        public abstract void SortDistinct(Key K);

        public abstract void Distinct();

        public virtual void Shuffle()
        {
            this.Shuffle(Environment.TickCount);
        }

        public abstract void Shuffle(int Seed);

        public abstract void Reverse();

        public abstract void Clear();

        internal abstract void Update();

        public abstract void Union(RecordSet Data);

        // Prints //
        public abstract void Print();

        public abstract void Print(int N);

        public abstract string About { get; }

        // Query //
        public abstract RecordReader OpenReader();

        public abstract RecordReader OpenReader(Predicate P);

        public abstract RecordWriter OpenWriter();

        // Internals //
        public abstract bool IsBig { get; }

        public abstract RecordSet ToRecordSet { get; }

        public abstract Table ToBigRecordSet { get; }

        public abstract long CellCount
        {
            get;
        }

        // Statics //
        public static DataSet CreateOfType(DataSet Basis, string Dir, string Name, Schema Columns, long MaxSize)
        {

            if (Basis.IsBig)
                return new Table(Dir, Name, Columns, MaxSize);
            else if (Basis.ToRecordSet.IsAttached)
                return new RecordSet(Dir, Name, Columns, MaxSize);
            else
                return new RecordSet(Columns);

        }

        public static DataSet CreateOfType(DataSet Basis, Schema Columns)
        {
            return CreateOfType(Basis, Basis.Directory, Header.TempName(), Columns, Basis.MaxRecords);
        }

    }


}
