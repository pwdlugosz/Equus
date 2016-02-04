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
    public sealed class MNodeLiteral : MNode
    {

        private CellMatrix _value;

        public MNodeLiteral(MNode Parent, CellMatrix Value)
            : base(Parent)
        {
            this._value = Value;
        }

        public override CellMatrix Evaluate()
        {
            return this._value;
        }

        public override CellAffinity ReturnAffinity()
        {
            return this._value.Affinity;
        }

        public override MNode CloneOfMe()
        {
            return new MNodeLiteral(this.ParentNode, new CellMatrix(this._value));
        }

    }

}
