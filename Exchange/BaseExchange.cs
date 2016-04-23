using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;


namespace Equus.Exchange
{

    public abstract class BaseExchange<T>
    {

        public BaseExchange(Schema Columns)
        {
            this.Columns = Columns;
        }

        public Schema Columns
        {
            get;
            protected set;
        }

        public abstract Record Render(T Value);

        public abstract T Raze(Record Value);

    }

    public sealed class TextExchange : BaseExchange<string>
    {

        public TextExchange(Schema Columns, char Delim)
            : base(Columns)
        {
            this.Delim = Delim;
        }

        public TextExchange(Schema Columns)
            : this(Columns, ',')
        {
        }

        public char Delim
        {
            get;
            set;
        }

        public override Record Render(string Value)
        {
            return Record.Parse(this.Columns, Value, this.Delim);
        }

        public override string Raze(Record Value)
        {
            return Value.ToString(this.Delim);
        }

    }


}
