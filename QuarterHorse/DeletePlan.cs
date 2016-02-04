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

    public sealed class DeletePlan : CommandPlan
    {

        private DataSet _data;
        private Predicate _where;

        public DeletePlan(DataSet Data, Predicate Where)
            : base()
        {

            this._data = Data;
            this._where = Where;
            this.Name = "DELETE";

        }

        public override void Execute()
        {

            this.Message.AppendLine(string.Format("Source: {0}", this._data.Name));
            this._timer = System.Diagnostics.Stopwatch.StartNew();
            Tuple<long, long> read_writes = Delete(_data, _where);
            this._timer.Stop();
            this._reads = read_writes.Item1;
            this.Message.AppendLine(string.Format("Reads: {0}", this._reads));
            this.Message.AppendLine(string.Format("Writes: {0}", this._writes));

        }

        public static long Delete(RecordSet Extent, Predicate Where)
        {

            long n = 0;
            StaticRegister mem = new StaticRegister(null);
            Where.AssignRegister(mem);
            for (int i = Extent.Count - 1; i >= 0; i--)
            {
                mem.Assign(Extent[i]);
                if (Where.Render())
                {
                    Extent.Remove(i);
                    n++;
                }

            }
            return n;

        }

        public static Tuple<long,long> Delete(DataSet Data, Predicate Where)
        {

            long CurrentCount = 0;
            long NewCount = 0;

            foreach (RecordSet rs in Data.Extents)
            {

                // Append the current record count //
                CurrentCount += (long)rs.Count;

                // Append the new count and delete //
                NewCount += Delete(rs, Where);

                // Flush //
                if (rs.IsAttached)
                    BinarySerializer.Flush(rs);

            }

            BinarySerializer.Flush(Data);

            // Return the delta //
            return new Tuple<long,long>(CurrentCount, NewCount);

        }


    }

}
