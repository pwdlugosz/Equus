using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Gidran;
using Equus.Fjord;
using Equus.Shire;

namespace Equus.Andalusian
{

    class TNodeMatrixAssign : TNode
    {

        private MNode _expression;
        private int _ref;
        private Heap<CellMatrix> _mat;

        public TNodeMatrixAssign(TNode Parent, Heap<CellMatrix> Heap, int Ref, MNode Expression)
            : base(Parent)
        {
            this._expression = Expression;
            this._ref = Ref;
            this._mat = Heap;
        }

        public override void Invoke()
        {
            this._mat[this._ref] = this._expression.Evaluate();
        }

        public override string Message()
        {
            return "Matrix Assign";
        }

        public override TNode CloneOfMe()
        {
            return new TNodeMatrixAssign(this.Parent, this._mat, this._ref, this._expression.CloneOfMe());
        }

    }

}
