using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Fjord;

namespace Equus.Andalusian
{
    
    class TNodePrintMatrix : TNode 
    {

        private MNode _expression;

        public TNodePrintMatrix(TNode Parent, MNode Expression)
            : base(Parent)
        {
            this._expression = Expression;
        }

        public override void Invoke()
        {

            if (this._expression == null)
                return;

            Horse.Comm.WriteLine(this._expression.Evaluate().ToString());

        }

        public override string Message()
        {
            return "PrintMatrix";
        }

        public override TNode CloneOfMe()
        {
            return new TNodePrintMatrix(this.Parent, this._expression.CloneOfMe());
        }

    }



}
