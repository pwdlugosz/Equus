using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;

namespace Equus.Mustang
{


    /// <summary>
    /// Assumes that the table passed has more extents than the partition count
    /// </summary>
    public sealed class LargeTablePartitioner : TablePartitioner
    {

        private int[] _begin;
        private int[] _count;
        private int[] _pointer;

        public LargeTablePartitioner(Table Data, int PartitionCount)
            : base(Data, PartitionCount)
        {

            if (Data.ExtentCount < PartitionCount)
                throw new Exception(string.Format("Data has too few extents"));

            // Build the mappings //
            this.LoadMapping();


        }

        public override int Elements(int Partition)
        {
            return this._count[Partition];
        }

        public override bool CanRequest(int Partition)
        {
            return (this._pointer[Partition] < this._count[Partition] + this._begin[Partition]);
        }

        public override RecordSet Request(int Partition)
        {
            return this._Data.PopAt(this._pointer[Partition]);
        }

        public override RecordSet RequestNext(int Partition)
        {

            lock (this._Lock)
            {
                RecordSet rs = this.Request(Partition);
                this._pointer[Partition]++;
                return rs;
            }

        }

        private void LoadMapping()
        {

            // Set up the arrays //
            this._begin = new int[this.PartitionCount];
            this._count = new int[this.PartitionCount];
            this._pointer = new int[this.PartitionCount];

            // Figure out how we want to split up the data //
            int[] BucketCount = new int[this.PartitionCount];
            int BaseBucketCount = this._Data.ExtentCount / this.PartitionCount;
            int Remainder = this._Data.ExtentCount % this.PartitionCount;
            int RemainderCounter = 0;

            // Figureout how many records we need in each partition //
            int BeginAt = 0;
            for (int i = 0; i < PartitionCount; i++)
            {

                int CountOf = BaseBucketCount + (RemainderCounter < Remainder ? 1 : 0);
                this._begin[i] = BeginAt;
                this._count[i] = CountOf;
                this._pointer[i] = BeginAt;
                //Console.WriteLine(string.Format("Begin {0} : Count {1}", BeginAt, CountOf));
                RemainderCounter++;
                BeginAt += CountOf;

            }


        }


    }


}
