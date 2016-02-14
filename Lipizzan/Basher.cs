using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Gidran;

namespace Equus.Lipizzan
{
    
    public static class Basher
    {

        public static Cell Bash(Cell C)
        {

            byte[] b = new byte[C.DataSize];
            b[0] = (byte)C.AFFINITY;
            b[1] = C.NULL;
            if (C.NULL == 1)
                return new Cell(b);

            if (C.AFFINITY == CellAffinity.BOOL)
            {
                b[2] = (byte)(C.BOOL ? 1 : 0);
            }
            else if (C.AFFINITY == CellAffinity.STRING)
            {
                Array.Copy(System.Text.UnicodeEncoding.Unicode.GetBytes(C.STRING.ToCharArray()), 0, b, 2, C.STRING.Length * 2);
            }
            else if (C.AFFINITY == CellAffinity.BLOB)
            {
                Array.Copy(C.BLOB, 0, b, 2, C.BLOB.Length);
            }
            else
            {

                b[2] = C.B0;
                b[3] = C.B1;
                b[4] = C.B2;
                b[5] = C.B3;
                b[6] = C.B4;
                b[7] = C.B5;
                b[8] = C.B6;
                b[8] = C.B7;

            }

            return new Cell(b);

        }


    }

}
