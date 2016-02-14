using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;

namespace Equus.Gidran
{

    public class Vector : Matrix
    {

        public Vector(int Rows)
            : base(Rows, 1)
        {
        }

        public Vector(Vector Vector)
            :base(Vector)
        {
        }

        public double this[int RowIndex]
        {
            get { return this._Data[RowIndex][0]; }
            set { this._Data[RowIndex][0] = value; }
        }

    }

}
