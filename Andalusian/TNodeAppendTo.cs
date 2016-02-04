using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Calabrese;
using Equus.Shire;
using Equus.Horse;

namespace Equus.Andalusian
{

    /// <summary>
    /// Append to class
    /// </summary>
    public sealed class TNodeAppendTo : TNode
    {

        private RecordWriter _writer;
        private FNodeSet _output;
        private long _writes = 0;

        public TNodeAppendTo(TNode Parent, RecordWriter Writer, FNodeSet Output)
            : base(Parent)
        {

            // Check that the column count is the same; we dont care about the schema //
            if (Writer.SourceSchema.Count != Output.Count)
                throw new Exception("Attempting to write a different number of recors to a stream");

            this._writer = Writer;
            this._output = Output;

        }

        public override long Writes
        {
            get { return this._writes; }
            protected set { this._writes = value; }
        }

        public override void BeginInvoke()
        {
            this._writes = 0;
        }

        public override void Invoke()
        {
            Record r = this._output.Evaluate();
            this._writer.Insert(r);
            this._writes++;
        }

        public override void EndInvoke()
        {
            this._writer.Close();
            base.EndInvoke();
        }

        public override string Message()
        {
            return "Apend: Writes " + this.Writes.ToString();
        }

        public override TNode CloneOfMe()
        {
            return new TNodeAppendTo(this.Parent, this._writer, this._output.CloneOfMe());
        }


    }

}
