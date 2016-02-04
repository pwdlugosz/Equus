using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Calabrese;

namespace Equus.Nokota
{

    public sealed class AggregateCountNull : AggregateCount
    {

        public AggregateCountNull(FNode M, Predicate F)
            : base(M, F)
        {
        }

        public AggregateCountNull(FNode M)
            : base(M)
        {
        }

        public override void Accumulate(Record WorkData)
        {

            if (!this._F.Render()) return;

            Cell a = this._Map.Evaluate();
            Cell b = WorkData[0];
            if (a.IsNull && !b.IsNull)
            {
                WorkData[0]++;
            }
            else if (b.IsNull && a.IsNull)
            {
                WorkData[0] = Cell.OneValue(this.ReturnAffinity);
            }

        }

        public override void AssignRegister(Shire.Register Memory)
        {
            base.AssignRegister(Memory);
            this._Map.AssignRegister(Memory);
        }

        public override Aggregate CloneOfMe()
        {
            return new AggregateCountNull(this._Map.CloneOfMe(), this._F.CloneOfMe());
        }

    }

}




