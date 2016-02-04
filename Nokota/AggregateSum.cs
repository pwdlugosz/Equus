using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Calabrese;

namespace Equus.Nokota
{

    public sealed class AggregateSum : Aggregate
    {

        private FNode _Map;

        public AggregateSum(FNode M, Predicate F)
            : base(M.ReturnAffinity())
        {
            this._Map = M;
            this._F = F;
            this._Sig = 1;
        }

        public AggregateSum(FNode M)
            : this(M, Predicate.TrueForAll)
        {
            this._Map = M;
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
            //return Record.Stitch(Cell.ZeroValue(this.ReturnAffinity));
            return Record.Stitch(new Cell(this.ReturnAffinity));
        }

        public override void Accumulate(Record WorkData)
        {

            if (!this._F.Render()) return;

            Cell a = this._Map.Evaluate();
            Cell b = WorkData[0];
            
            if (!a.IsNull && !b.IsNull)
            {
                WorkData[0] = a + b;
            }
            else if (b.IsNull)
            {
                WorkData[0] = a;
            }
            
        }

        public override void Merge(Record WorkData, Record MergeIntoWorkData)
        {
            Cell a = WorkData[0];
            Cell b = MergeIntoWorkData[0];
            if (!a.IsNull && !b.IsNull)
            {
                MergeIntoWorkData[0] += WorkData[0];
            }
            else if (!a.IsNull && b.IsNull)
            {
                MergeIntoWorkData[0] = WorkData[0];
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
            return new AggregateSum(this._Map.CloneOfMe(), this._F.CloneOfMe());
        }

    }

}




