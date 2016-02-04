using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;

namespace Equus.Mustang
{

    /// <summary>
    /// Helps to partition a table for multi-threaded reads 
    /// </summary>
    public abstract class TablePartitioner
    {

        protected int _Partitions = 0;
        protected DataSet _Data = null;
        protected object _Lock = new object();

        public TablePartitioner(DataSet Data, int PartitionCount)
        {
            this._Data = Data;
            this._Partitions = PartitionCount;
        }

        public int PartitionCount
        {
            get { return this._Partitions; }
        }

        public DataSet Data
        {
            get { return this._Data; }
        }

        // Abstracts //
        public abstract int Elements(int Partition);

        public abstract RecordSet Request(int Partition);

        public abstract RecordSet RequestNext(int Partition);

        public abstract bool CanRequest(int Partition);

        // statics //
        public static RecordSet[] Split(RecordSet Data, int PartitionCount)
        {

            RecordSet[] partitions = new RecordSet[PartitionCount];
            int[] partition_map = RecordMap(PartitionCount, Data.Count);

            int rec_ptr = 0;
            for (int i = 0; i < PartitionCount; i++)
            {

                partitions[i] = new RecordSet(Data.Columns);
                int max_count = partition_map[i];
                int local_count = 0;
                while (local_count < max_count)
                {

                    partitions[i].Add(Data[rec_ptr]);
                    rec_ptr++;
                    local_count++;

                }

            }

            return partitions;

        }

        private static int[] RecordMap(int PartitionCount, int RecordCount)
        {

            // Figure out how we want to split up the data //
            int[] BucketCount = new int[PartitionCount];
            int BaseBucketCount = RecordCount / PartitionCount;
            int Remainder = RecordCount % PartitionCount;
            int RemainderCounter = 0;

            // Figureout how many records we need in each partition //
            for (int i = 0; i < PartitionCount; i++)
            {

                BucketCount[i] = BaseBucketCount + (RemainderCounter < Remainder ? 1 : 0);
                RemainderCounter++;

            }

            return BucketCount;

        }

    }

}
