using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Calabrese;

namespace Equus.Nokota
{

    // designed to support: average, varp, stdevp, vars, stdevs
    public abstract class AggregateStat : Aggregate
    {

        protected FNode _MapX;
        protected FNode _MapW;

        public AggregateStat(FNode X, FNode W, Predicate F)
            : base(X.ReturnAffinity())
        {
            this._MapX = X;
            this._MapW = W;
            this._F = F;
            this._Sig = 3;
        }

        public AggregateStat(FNode X, FNode W)
            : this(X, W, PredicateFactory.IsNotNull(X))
        {
        }

        public AggregateStat(FNode X)
            : this(X, new FNodeValue(null, Cell.OneValue(X.ReturnAffinity())))
        {
        }

        public AggregateStat(FNode X, Predicate F)
            : this(X, new FNodeValue(null, Cell.OneValue(X.ReturnAffinity())), F)
        {
        }

        public override List<int> FieldRefs
        {

            get
            {
                List<int> refs = new List<int>();
                refs.AddRange(FNodeAnalysis.AllFieldRefs(this._F.Node));
                refs.AddRange(FNodeAnalysis.AllFieldRefs(this._MapX));
                refs.AddRange(FNodeAnalysis.AllFieldRefs(this._MapW));
                return refs;
            }

        }

        public override Record Initialize()
        {
            return Record.Stitch
            (
                Cell.ZeroValue(this._MapW.ReturnAffinity()), Cell.ZeroValue(this._MapX.ReturnAffinity()), Cell.ZeroValue(this._MapX.ReturnAffinity())
            );
        }

        public override void Accumulate(Record WorkData)
        {

            if (!this._F.Render()) return;

            Cell a = this._MapW.Evaluate();
            Cell b = WorkData[0];
            Cell c = this._MapX.Evaluate();
            Cell d = WorkData[1];
            Cell e = WorkData[2];

            if (!a.IsNull && !b.IsNull && !c.IsNull && !d.IsNull)
            {
                WorkData[0] += a;
                WorkData[1] += a * c;
                WorkData[2] += a * c * c;
            }
            else if (b.IsNull)
            {
                WorkData[0] = a;
                WorkData[1] = a * c;
                WorkData[2] = a * c * c;
            }

        }

        public override void Merge(Record WorkData, Record MergeIntoWorkData)
        {

            Cell a = WorkData[0];
            Cell b = MergeIntoWorkData[0];
            Cell c = WorkData[1];
            Cell d = MergeIntoWorkData[1];
            
            if (!a.IsNull && !b.IsNull && !c.IsNull && !d.IsNull)
            {
                MergeIntoWorkData[0] += WorkData[0];
                MergeIntoWorkData[1] += WorkData[1];
                MergeIntoWorkData[2] += WorkData[2];
            }
            else if (!a.IsNull && !c.IsNull && b.IsNull)
            {
                MergeIntoWorkData[0] = WorkData[0];
                MergeIntoWorkData[1] = WorkData[1];
                MergeIntoWorkData[2] = WorkData[2];
            }

        }
        
        public override void AssignRegister(Shire.Register Memory)
        {
            base.AssignRegister(Memory);
            this._MapX.AssignRegister(Memory);
            this._MapW.AssignRegister(Memory);
        }

        public override int Size()
        {
            return this._MapX.DataSize();
        }

    }

}




