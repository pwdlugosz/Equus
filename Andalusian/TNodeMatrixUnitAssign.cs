using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Calabrese;
using Equus.Shire;
using Equus.Horse;
using Equus.Gidran;

namespace Equus.Andalusian
{

    /// <summary>
    /// Assign ID: 0 == assign, 1 == increment, 2 == decrement, 3 == auto increment, 4 == auto decrement
    /// </summary>
    public sealed class TNodeMatrixUnitAssign : TNode
    {

        private FNode _Node;
        private CellMatrix _matrix;
        private FNode _row_id;
        private FNode _col_id;
        private int _AssignID; // 0 == assign, 1 == increment, 2 == decrement, 3 == auto increment, 4 == auto decrement

        public TNodeMatrixUnitAssign(TNode Parent, CellMatrix Data, FNode Node, FNode RowID, FNode ColumnID, int AssignID)
            : base(Parent)
        {
            this._matrix = Data;
            this._Node = Node;
            this._AssignID = AssignID;
            this._row_id = RowID;
            this._col_id = ColumnID;
        }

        public override void Invoke()
        {

            int r = (int)this._row_id.Evaluate().INT;
            int c = (int)this._col_id.Evaluate().INT;

            switch (this._AssignID)
            {
                case 0:
                    this._matrix[r, c] = this._Node.Evaluate();
                    break;
                case 1:
                    this._matrix[r, c] += this._Node.Evaluate();
                    break;
                case 2:
                    this._matrix[r, c] -= this._Node.Evaluate();
                    break;
                case 3:
                    this._matrix[r, c]++;
                    break;
                case 4:
                    this._matrix[r, c]--;
                    break;
            }

        }

        public override TNode CloneOfMe()
        {
            return new TNodeMatrixUnitAssign(this.Parent, this._matrix, this._Node.CloneOfMe(), this._row_id.CloneOfMe(), this._col_id.CloneOfMe(), this._AssignID);
        }

    }

}
