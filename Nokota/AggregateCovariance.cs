using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Calabrese;

namespace Equus.Nokota
{

    public sealed class AggregateCovariance : AggregateStatCo
    {

        public AggregateCovariance(FNode X, FNode Y, FNode W, Predicate F)
            : base(X, Y, W, F)
        {
        }

        public AggregateCovariance(FNode X, FNode Y, Predicate F)
            : base(X, Y, F)
        {
        }

        public AggregateCovariance(FNode X, FNode Y, FNode W)
            : base(X, Y, W)
        {
        }

        public AggregateCovariance(FNode X, FNode Y)
            : base(X, Y)
        {
        }

        public override Cell Evaluate(Record WorkData)
        {

            if (WorkData[0].IsZero == true) return new Cell(this.ReturnAffinity);
            Cell avgx = WorkData[1] / WorkData[0];
            Cell avgy = WorkData[3] / WorkData[0];
            Cell avgxy = WorkData[5] / WorkData[0];
            return avgxy - avgx * avgy;

        }

        public override Aggregate CloneOfMe()
        {
            return new AggregateCovariance(this._MapX.CloneOfMe(), this._MapY.CloneOfMe(), this._MapW.CloneOfMe(), this._F.CloneOfMe());
        }

    }

}
