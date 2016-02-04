using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Gidran;
using Equus.Horse;
using Equus.Shire;
using Equus.Calabrese;

namespace Equus.Fjord
{
    public sealed class MNodeDivideScalar : MNode
    {

        private int _Association = 0; // 0 == left (A * B[]), 1 == right (B[] * A)
        private FNode _expression;

        public MNodeDivideScalar(MNode Parent, FNode Expression, int Association)
            : base(Parent)
        {
            this._Association = Association;
            this._expression = Expression;
        }

        public override CellMatrix Evaluate()
        {
            if (this._Association == 0)
                return this._expression.Evaluate() / this[0].Evaluate();
            else
                return this[0].Evaluate() / this._expression.Evaluate();
        }

        public override CellAffinity ReturnAffinity()
        {
            if (this._Association == 0)
                return this._expression.ReturnAffinity();
            else
                return this[0].ReturnAffinity();
        }

        public override MNode CloneOfMe()
        {
            MNode node = new MNodeDivideScalar(this.ParentNode, this._expression.CloneOfMe(), this._Association);
            foreach (MNode m in this._Cache)
                node.AddChildNode(m.CloneOfMe());
            return node;
        }

    }

}
