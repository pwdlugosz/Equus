using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Equus.QuarterHorse
{

    public abstract class CommandPlan
    {

        // reads and writes are protect variables b/c it optimizes the run w/o the overhead of properties //
        protected long _reads;
        protected long _writes;
        protected System.Diagnostics.Stopwatch _timer;

        public CommandPlan()
        {
            this.Message = new StringBuilder();
            this.Name = new string('?', DateTime.Now.Month);
            this._timer = new System.Diagnostics.Stopwatch();
        }

        public long Reads
        {
            get { return this._reads; }
        }

        public long Writes
        {
            get { return this._writes; }
        }

        public string Name
        {
            get;
            protected set;
        }

        public StringBuilder Message
        {
            get;
            protected set;
        }

        public TimeSpan ExecutionTime
        {
            get
            {
                return this._timer.Elapsed;
            }
        }

        public abstract void Execute();

        public string MessageText()
        {
            string text = this.Name + " : " + this._timer.Elapsed.ToString();
            text += '\n';
            text += this.Message.ToString();
            return text;
        }

    }

}
