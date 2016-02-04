using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Calabrese;

namespace Equus.Nokota
{

    public sealed class AggregateAverage : AggregateStat
    {

        public AggregateAverage(FNode X, FNode W, Predicate F)
            : base(X, W, F)
        {
        }

        public AggregateAverage(FNode X, Predicate F)
            : base(X, F)
        {
        }

        public AggregateAverage(FNode X, FNode W)
            :base(X, W)
        {
        }

        public AggregateAverage(FNode X)
            : base(X)
        {
        }

        public override Cell Evaluate(Record WorkData)
        {
            if (WorkData[0].IsZero) return new Cell(this.ReturnAffinity);
            return WorkData[1] / WorkData[0];
        }
        
        public override Aggregate CloneOfMe()
        {
            return new AggregateAverage(this._MapX.CloneOfMe(), this._MapW.CloneOfMe(), this._F.CloneOfMe());
        }

    }

}
