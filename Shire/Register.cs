using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;

namespace Equus.Shire
{

    public abstract class Register
    {

        private Guid _UID;

        public Register()
        {
            this._UID = Guid.NewGuid();
        }

        public abstract Record Value();

        public abstract Schema Columns();

    }

    public sealed class StaticRegister : Register
    {

        private Record _Datum;
        private Schema _Columns;

        public StaticRegister(Schema Columns)
            : base()
        {
            this._Datum = null;
            this._Columns = Columns;
        }

        public override Record Value()
        {
            return this._Datum;
        }

        public override Schema Columns()
        {
            return this._Columns;
        }

        public void Assign(Record Datum)
        {
            this._Datum = Datum;
        }

    }

    public sealed class StreamRegister : Register
    {

        private RecordReader _stream;
        
        public StreamRegister(RecordReader Stream)
            : base()
        {
            this._stream = Stream;
        }

        public override Schema Columns()
        {
            return this._stream.SourceSchema;
        }

        public override Record Value()
        {
            return this._stream.Read();
        }

        public RecordReader BaseStream
        {
            get { return this._stream; }
            set { this._stream = value; }
        }

    }

}
