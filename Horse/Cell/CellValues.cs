using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Equus.Horse
{
    
    /// <summary>
    /// A collection of common cell values
    /// </summary>
    public static class CellValues
    {

        // Nulls //
        public static readonly Cell NULL_BOOL = new Cell(CellAffinity.BOOL);
        public static readonly Cell NULL_BLOB = new Cell(CellAffinity.BLOB);
        public static readonly Cell NULL_DATE = new Cell(CellAffinity.DATE_TIME);
        public static readonly Cell NULL_DOUBLE = new Cell(CellAffinity.DOUBLE);
        public static readonly Cell NULL_INT = new Cell(CellAffinity.INT);
        public static readonly Cell NULL_STRING = new Cell(CellAffinity.STRING);

        // Zero //
        public static readonly Cell ZERO_DOUBLE = new Cell(0D);
        public static readonly Cell ZERO_INT = new Cell(0);

        // One //
        public static readonly Cell ONE_DOUBLE = new Cell(1D);
        public static readonly Cell ONE_INT = new Cell(1);

        // Numerics //
        public static readonly Cell PI = new Cell(Math.PI);
        public static readonly Cell SQRT2PI = new Cell(Math.Sqrt(2D * Math.PI));
        public static readonly Cell EPSILON = new Cell(double.Epsilon);
        public static readonly Cell N95 = new Cell(1.9645);
    
    }

}
