using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Calabrese;

namespace Equus.Shire
{

    public class RecordWriter : RecordReader
    {

        private long _Ticks = 0;

        public RecordWriter(RecordSet Data, Predicate Having)
            : base(Data, Having)
        {
        }

        public RecordWriter(RecordSet Data) 
            :this(Data, Predicate.TrueForAll)
	    {
	    }

        public virtual Predicate Having
        {
            get { return this._Where; }
            set { this._Where = value; }
        }

        public long Ticks
        {
            get { return this._Ticks; }
        }

        public virtual void Insert(Record Data)
	    {
            if (this._Where.Render())
            {
                this._Ticks++;
                this._Data.Add(Data);
            }
	    }

        public virtual void Insert(DataSet Data)
        {
            RecordReader rr = Data.OpenReader();
            while (!rr.EndOfData)
                this.Insert(rr.ReadNext());
        }

        public virtual void Insert(DataSet Data, long Limit)
        {

            if (Limit < 0)
            {
                this.Insert(Data);
                return;
            }

            long Ticks = 0;
            RecordReader rr = Data.OpenReader();
            while (!rr.EndOfData && Ticks < Limit)
            {
                Ticks++;
                this.Insert(rr.ReadNext());
            }
        }

        public virtual void Set(Record Data)
        {
            this._Data[this._ptrRecord] = Data;
        }
        
        public virtual void SetNext(Record Data)
        {
            this.Set(Data);
            this.Advance();
        }

        public virtual void Close()
        {
            if (this._Data.IsAttached)
                BinarySerializer.FlushRecordSet(this._Data);
        }

    }

}
