using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;

namespace Equus.Mustang
{


    /// <summary>
    /// Splitter for a single extent
    /// </summary>
    public sealed class SmallTablePartitioner : TablePartitioner
    {

        private RecordSet[] _extents;
        private bool[] _can_request;

        public SmallTablePartitioner(RecordSet Data, int PartitionCount)
            : base(Data, PartitionCount)
        {

            this._can_request = new bool[PartitionCount];
            for (int i = 0; i < PartitionCount; i++)
                this._can_request[i] = true;
            this._extents = TablePartitioner.Split(Data, PartitionCount);

        }

        public override bool CanRequest(int Partition)
        {
            return this._can_request[Partition];
        }

        public override int Elements(int Partition)
        {
            return 1;
        }

        public override RecordSet Request(int Partition)
        {
            return this._extents[Partition];
        }

        public override RecordSet RequestNext(int Partition)
        {
            RecordSet rs = Request(Partition);
            this._can_request[Partition] = false;
            return rs;
        }

    }


}
