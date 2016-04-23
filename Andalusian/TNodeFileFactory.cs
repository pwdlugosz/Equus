using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Equus.Shire;
using Equus.Horse;

namespace Equus.Andalusian
{

    public sealed class TNodeFileReadString : TNode
    {

        private string _path;
        private StreamReader _stream;
        private CellHeap _heap;
        private int _ref;

        public TNodeFileReadString(TNode Parent, string Path, CellHeap Memory, int HeapRef)
            : base(Parent)
        {
            this._path = Path;
            this._stream = new StreamReader(Path);
            this._heap = Memory;
            this._ref = HeapRef;
            this.SupportsMapReduce = false;
        }

        public override void Invoke()
        {
            this._heap[this._ref] = new Cell(this._stream.ReadLine());
        }

        public override void EndInvoke()
        {
            this._stream.Close();
            this._stream.Dispose();
        }

        public override TNode CloneOfMe()
        {
            return new TNodeFileReadString(this.Parent, this._path, this._heap, this._ref);
        }

    }

    public sealed class TNodeFileReadBLOB : TNode
    {

        private string _path;
        private Stream _stream;
        private CellHeap _heap;
        private int _ref;
        private int _len;

        public TNodeFileReadBLOB(TNode Parent, string Path, CellHeap Memory, int HeapRef, int Len)
            : base(Parent)
        {
            this._path = Path;
            this._stream = File.Open(Path, FileMode.Open, FileAccess.Read);
            this._heap = Memory;
            this._ref = HeapRef;
            this._len = Len;
            this.SupportsMapReduce = false;
        }

        public override void Invoke()
        {
            int len = (int)Math.Min((long)this._len, (this._stream.Length - this._stream.Position));
            byte[] b = new byte[len];
            this._stream.Read(b, 0, len);
            this._heap[this._ref] = new Cell(b);
        }

        public override void EndInvoke()
        {
            this._stream.Close();
            this._stream.Dispose();
        }

        public override TNode CloneOfMe()
        {
            return new TNodeFileReadBLOB(this.Parent, this._path, this._heap, this._ref, this._len);
        }

    }

    public sealed class TNodeFileReadAllString : TNode
    {

        private string _path;
        private CellHeap _heap;
        private int _ref;

        public TNodeFileReadAllString(TNode Parent, string Path, CellHeap Memory, int HeapRef)
            : base(Parent)
        {
            this._path = Path;
            this._heap = Memory;
            this._ref = HeapRef;
            this.SupportsMapReduce = false;
        }

        public override void Invoke()
        {
            this._heap[this._ref] = new Cell(File.ReadAllText(this._path));
        }

        public override TNode CloneOfMe()
        {
            return new TNodeFileReadAllString(this.Parent, this._path, this._heap, this._ref);
        }

    }


}
