using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Equus.Horse
{

    public abstract class Communicator
    {

        protected string _Header = "<:.:.:.:.:.:.:.:.:.:.:.:.:.:.:.:.:.:.:.:.:.:.:.:.:.:.:.:.:>";
        protected List<Record> _RecordBuffer;
        protected List<string> _StringBuffer;
        protected object _Locker;
        //protected ConcurrentBag<Record> _RecordBuffer;
        //protected ConcurrentBag<string> _StringBuffer;

        public Communicator()
        {
            this._RecordBuffer = new List<Record>();
            this._StringBuffer = new List<string>();
            this._Locker = new object();
        }

        public abstract void Communicate(Record Data);

        public abstract void Communicate(string Text, params object[] Values);

        public virtual void Communicate()
        {
            this.Communicate(this._Header);
        }

        public virtual void AppendBuffer(Record Data)
        {
        
            lock (this._Locker)
            {
                this._RecordBuffer.Add(Data);
            }
        
        }

        public virtual void AppendBuffer(string Text, params object[] Values)
        {

            lock (this._Locker)
            {
                this._StringBuffer.Add(string.Format(Text, Values));
            }

        }

        public virtual void AppendBuffer()
        {
            this.AppendBuffer(this._Header);
        }

        public virtual void FlushRecordBuffer()
        {

            lock (this._Locker)
            {
                foreach (Record r in this._RecordBuffer)
                    Communicate(r);
                //this._RecordBuffer = new ConcurrentBag<Record>();
                this._RecordBuffer.Clear();
            }
        }

        public virtual void FlushStringBuffer()
        {

            lock (this._Locker)
            {
                foreach (string s in this._StringBuffer)
                    Communicate(s);
                //this._StringBuffer = new ConcurrentBag<string>();
                this._StringBuffer.Clear();
            }

        }

    }

    public sealed class ConsoleCommunicator : Communicator
    {

        public ConsoleCommunicator()
            : base()
        {
        }

        public override void Communicate(Record Data)
        {
            Console.WriteLine(Data.ToString());
        }

        public override void Communicate(string Text, params object[] Values)
        {
            Console.WriteLine(Text, Values);
        }

    }

}
