using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Calabrese;
using Equus.Shire;
using Equus.Gidran;
using Equus.Nokota;
using Equus.Andalusian;

namespace Equus.QuarterHorse
{

    public sealed class UpdatePlan : CommandPlan
    {

        private DataSet _data;
        private Predicate _where;
        private Key _keys;
        private FNodeSet _values;

        public UpdatePlan(DataSet Data, Key K, FNodeSet Fields, Predicate BaseDataFilter)
            : base()
        {

            this._data = Data;
            this._keys = K;
            this._values = Fields;
            this._where = BaseDataFilter;
            this.Name = "UPDATE";

        }

        public override void Execute()
        {

            this._timer = System.Diagnostics.Stopwatch.StartNew();
            this._reads = Update(this._data, this._keys, this._values, this._where);
            this._timer.Stop();
            this._writes = this._reads;
            this.Message.AppendLine("Reads: " + this._reads.ToString());
            this.Message.AppendLine("Writes: " + this._writes.ToString());

        }

        private static void Update(Record Data, Key K, FNodeSet Fields)
        {
            int idx = 0;
            for (int i = 0; i < K.Count; i++)
            {
                idx = K[i];
                Data[idx] = Fields[i].Evaluate();
            }
        }

        public static long Update(DataSet Data, Key K, FNodeSet Fields, Predicate BaseDataFilter)
        {

            // Check that the field indicies and the maps have the same length //
            if (K.Count != Fields.Count)
                throw new Exception(string.Format("Field collection passed [{0}] has fewer elements than the map collection passed [{0}]", K.Count, Fields.Count));

            // Create the total append count //
            long CountOf = 0;

            // Loop through each extent //
            foreach (RecordSet rs in Data.Extents)
            {

                // Open a stream //
                RecordReader rr = new RecordReader(rs, BaseDataFilter);

                // Create a register //
                Register mem = new StreamRegister(rr);

                // Assign the register to the fields //
                Fields.AssignRegister(mem);

                // Update the data //
                while (!rr.EndOfData)
                {
                    Update(rr.Read(), K, Fields);
                    CountOf++;
                    rr.Advance();
                }

                // 
                if (rs.IsAttached)
                    BinarySerializer.Flush(rs);

            }

            // No need to flush the data set //

            return CountOf;

        }


    }

}
