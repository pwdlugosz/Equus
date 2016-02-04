using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Equus.Horse
{

    public class IndexSet : RecordSet
    {

        private const int OFFSET_HASH = 0;
        private const int OFFSET_ROW_ID = 1;
        private const string SCHEMA_SQL = "HASH INT, ROW_ID INT";

        // Constructor //
        public IndexSet(RecordSet Data, Key K)
            : base(new Schema(SCHEMA_SQL))
        {

            // Main loop //
            for (int i = 0; i < Data.Count; i++)
            {
                Record r = Record.Stitch(new Cell(Data[i].GetHashCode(K)), new Cell(i));
                this.Add(r);
            }

            // Sort table //
            this.Sort(new Key(0));

        }

        // Index Methods //
        public long IDXRowID(int Index)
        {
            return this[Index][OFFSET_ROW_ID].INT;
        }

        public long IDXHash(int Index)
        {
            return this[Index][OFFSET_HASH].INT;
        }

    }

}
