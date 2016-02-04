using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Shire;

namespace Equus.Horse
{

    public sealed class BigIndexSet : Table
    {

        public BigIndexSet(string SinkDir, RecordReader Stream, Key K)
            : base(SinkDir, TableHeader.TempName(), BuildSchema(K, Stream.SourceSchema))
        {

            // Open readers/writers //
            RecordWriter rw = this.OpenWriter();
            
            // Main loop //
            while (!Stream.EndOfData)
            {

                // Need to pull the id and position here because read next will advance the stream
                int pos = Stream.Position;
                long id = Stream.SetID;
                Record q = Stream.ReadNext();
                Record r = Record.Stitch(new Cell(id), new Cell(pos), new Cell(q.GetHashCode(K)));
                r = Record.Join(r, Record.Split(q, K));
                rw.Insert(r);
            }

            rw.Close();

            // Sort table //
            Key sort = new Key(2);
            for (int i = 0; i < K.Count; i++)
                sort.Add(3 + i);
            this.Sort(sort);

        }

        public BigIndexSet(string SinkDir, DataSet Data, Key K)
            : this(SinkDir, Data.OpenReader(), K)
        {
        }

        private static Schema BuildSchema(Key K, Schema S)
        {
            Schema t = new Schema("set_id int, row_id int, hash int");
            return Schema.Join(t, Schema.Split(S, K));
        }

    }

    public sealed class SmallIndexSet : RecordSet
    {

        public SmallIndexSet(DataSet Data, Key K)
            : base(BuildSchema(K, Data.Columns))
        {

            // Open readers/writers //
            RecordWriter rw = this.OpenWriter();
            RecordReader rr = Data.OpenReader();

            // Main loop //
            while (!rr.EndOfData)
            {

                // Need to pull the id and position here because read next will advance the stream
                int pos = rr.Position;
                long id = rr.SetID;
                Record q = rr.ReadNext();
                Record r = Record.Stitch(new Cell(id), new Cell(pos), new Cell(q.GetHashCode(K)));
                r = Record.Join(r, Record.Split(q, K));
                rw.Insert(r);
            }

            rw.Close();

            // Sort table //
            Key sort = new Key(2);
            for (int i = 0; i < K.Count; i++)
                sort.Add(3 + i);
            this.Sort(sort);

        }

        public SmallIndexSet(string SinkDir, DataSet Data, Key K)
            : this(Data, K)
        {
            Header h = new Header(SinkDir, Header.TempName(), 0, this, HeaderType.Table);
            this.Attach(h);
        }

        private static Schema BuildSchema(Key K, Schema S)
        {
            Schema t = new Schema("set_id int, row_id int, hash int");
            return Schema.Join(t, Schema.Split(S, K));
        }

    }

    public static class IndexBuilder
    {

        public static DataSet Build(DataSet Data, Key K, string SinkDir)
        {
            if (!Data.IsBig) return new SmallIndexSet(Data, K);
            return new BigIndexSet(SinkDir, Data, K);
        }

    }

}
