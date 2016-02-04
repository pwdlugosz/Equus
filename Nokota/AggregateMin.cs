using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Calabrese;

namespace Equus.Nokota
{

    public sealed class AggregateMin : Aggregate
    {

        private FNode _Map;

        public AggregateMin(FNode M, Predicate F)
            : base(M.ReturnAffinity())
        {
            this._Map = M;
            this._F = F;
            this._Sig = 1;
        }

        public AggregateMin(FNode M)
            : this(M, Predicate.TrueForAll)
        {
        }

        public override List<int> FieldRefs
        {
            get
            {
                List<int> refs = new List<int>();
                refs.AddRange(FNodeAnalysis.AllFieldRefs(this._F.Node));
                refs.AddRange(FNodeAnalysis.AllFieldRefs(this._Map));
                return refs;
            }
        }

        public override Record Initialize()
        {
            return Record.Stitch(new Cell(this.ReturnAffinity));
        }

        public override void Accumulate(Record WorkData)
        {

            if (!this._F.Render()) return;

            Cell c = this._Map.Evaluate();
            Cell d = WorkData[0];
            if (!c.IsNull && !d.IsNull) 
            {
                WorkData[0] = Cell.Min(c, d);
            }
            else if (d.IsNull)
            {
                WorkData[0] = c;
            }

        }

        public override void Merge(Record WorkData, Record MergeIntoWorkData)
        {

            Cell a = WorkData[0];
            Cell b = MergeIntoWorkData[0];
            if (!a.IsNull && !b.IsNull)
            {
                MergeIntoWorkData[0] = Cell.Min(a, b);
            }
            else if (!a.IsNull && b.IsNull)
            {
                MergeIntoWorkData[0] = a;
            }

        }

        public override Cell Evaluate(Record WorkData)
        {
            return WorkData[0];
        }

        public override void AssignRegister(Shire.Register Memory)
        {
            base.AssignRegister(Memory);
            this._Map.AssignRegister(Memory);
        }

        public override Aggregate CloneOfMe()
        {
            return new AggregateMin(this._Map.CloneOfMe(), this._F.CloneOfMe());
        }

    }

}




