using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;

namespace Equus.Gidran
{

    /// <summary>
    /// Create an N x 1 matrix
    /// </summary>
    public sealed class CellVector : CellMatrix
    {

        private const int COLUMN_COUNT = 1;
        private const int COLUMN_INDEX = 0;

        public CellVector(int RowCount, Cell Value)
            : base(RowCount, COLUMN_COUNT, Value)
        {
        }

        public CellVector(int RowCount, CellAffinity Affinity)
            : base(RowCount, COLUMN_COUNT, Affinity)
        {
        }

        public CellVector(CellVector Value)
            : base(Value)
        {
        }

        public Cell this[int Row]
        {

            get
            {
                return this._Data[Row, COLUMN_INDEX];
            }
            set
            {
                if (value.AFFINITY == this._Affinity)
                    this._Data[Row, COLUMN_INDEX] = value;
                else
                    this._Data[Row, COLUMN_INDEX] = Cell.Cast(value, this._Affinity);
            }

        }

        public int Count
        {
            get { return this._Rows; }
        }

        // Statics //
        public static CellVector operator +(CellVector A, Cell B)
        {
            CellVector C = new CellVector(A.Count, A.Affinity);
            for (int i = 0; i < A.Count; i++)
                C[i] = A[i] + B;
            return C;
        }

        public static CellVector operator +(Cell B, CellVector A)
        {
            CellVector C = new CellVector(A.Count, A.Affinity);
            for (int i = 0; i < A.Count; i++)
                C[i] = B + A[i];
            return C;
        }

        public static CellVector operator +(CellVector A, CellVector B)
        {
            CellVector C = new CellVector(A.Count, A.Affinity);
            for (int i = 0; i < A.Count; i++)
                C[i] = A[i] + B[i];
            return C;
        }
        
        public static CellVector operator -(CellVector A, Cell B)
        {
            CellVector C = new CellVector(A.Count, A.Affinity);
            for (int i = 0; i < A.Count; i++)
                C[i] = A[i] - B;
            return C;
        }

        public static CellVector operator -(Cell B, CellVector A)
        {
            CellVector C = new CellVector(A.Count, B.Affinity);
            for (int i = 0; i < A.Count; i++)
                C[i] = B - A[i];
            return C;
        }

        public static CellVector operator -(CellVector A, CellVector B)
        {
            CellVector C = new CellVector(A.Count, A.Affinity);
            for (int i = 0; i < A.Count; i++)
                C[i] = A[i] - B[i];
            return C;
        }

        public static CellVector operator *(CellVector A, Cell B)
        {
            CellVector C = new CellVector(A.Count, A.Affinity);
            for (int i = 0; i < A.Count; i++)
                C[i] = A[i] * B;
            return C;
        }

        public static CellVector operator *(Cell B, CellVector A)
        {
            CellVector C = new CellVector(A.Count, B.Affinity);
            for (int i = 0; i < A.Count; i++)
                C[i] = B * A[i];
            return C;
        }

        public static CellVector operator *(CellVector A, CellVector B)
        {
            CellVector C = new CellVector(A.Count, A.Affinity);
            for (int i = 0; i < A.Count; i++)
                C[i] = A[i] * B[i];
            return C;
        }

        public static CellVector operator /(CellVector A, Cell B)
        {
            CellVector C = new CellVector(A.Count, A.Affinity);
            for (int i = 0; i < A.Count; i++)
                C[i] = A[i] / B;
            return C;
        }

        public static CellVector operator /(Cell B, CellVector A)
        {
            CellVector C = new CellVector(A.Count, B.Affinity);
            for (int i = 0; i < A.Count; i++)
                C[i] = B / A[i];
            return C;
        }

        public static CellVector operator /(CellVector A, CellVector B)
        {
            CellVector C = new CellVector(A.Count, A.Affinity);
            for (int i = 0; i < A.Count; i++)
            {
                C[i] = A[i] / B[i];
            }
            return C;
        }

        public static CellVector CheckDivide(CellVector A, Cell B)
        {
            if (B.IsZero)
                return new CellVector(A.Count, Cell.ZeroValue(A.Affinity));
            return A / B;
        }

        public static CellVector CheckDivide(Cell B, CellVector A)
        {
            CellVector C = new CellVector(A.Count, B.Affinity);
            Cell zero = new Cell(B.AFFINITY);
            for (int i = 0; i < A.Count; i++)
                C[i] = (A[i].IsZero ? B / A[i] : zero);
            return C;
        }

        public static CellVector CheckDivide(CellVector A, CellVector B)
        {
            CellVector C = new CellVector(A.Count, B.Affinity);
            Cell zero = new Cell(B.Affinity);
            for (int i = 0; i < A.Count; i++)
                C[i] = (B[i].IsZero ? A[i] / B[i] : zero);
            return C;
        }

        public static CellVector ForEach(CellVector A, Func<Cell, Cell> Delegate, CellAffinity ReturnType)
        {
            
            CellVector B = new CellVector(A.Count, ReturnType);
            for (int i = 0; i < A.Count; i++)
            {
                B[i] = Delegate(A[i]);
            }

            return B;

        }

        public static CellVector Parse(string Text, CellAffinity Affinity)
        {

            string[] values = Text.Split(',');
            CellVector v = new CellVector(values.Length, Affinity);
            for (int i = 0; i < values.Length; i++)
            {
                v[i] = Cell.Parse(values[i], Affinity);
            }

            return v;

        }

        public static Cell DotProduct(CellVector A, CellVector B)
        {

            if (A.Count != B.Count)
                throw new ArgumentException("Both vectors passed must have the same lengh");

            Cell c = Cell.ZeroValue(A.Affinity);

            for (int i = 0; i < A.Count; i++)
                c += A[i] * B[i];

            return c;

        }

    }

}
