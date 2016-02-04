using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Calabrese;

namespace Equus.Nokota
{

    public abstract class AggregateStatCo : Aggregate
    {

        protected FNode _MapX;
        protected FNode _MapY;
        protected FNode _MapW;

        public AggregateStatCo(FNode X, FNode Y, FNode W, Predicate F)
            : base(X.ReturnAffinity())
        {
            this._MapX = X;
            this._MapY = Y;
            this._MapW = W;
            this._F = F;
            this._Sig = 6;
        }

        public AggregateStatCo(FNode X, FNode Y, Predicate F)
            : this(X, Y, new FNodeValue(null, Cell.OneValue(X.ReturnAffinity())), PredicateFactory.IsNotNull(X))
        {
        }

        public AggregateStatCo(FNode X, FNode Y, FNode W)
            : this(X, Y, W, PredicateFactory.IsNotNull(X))
        {
        }

        public AggregateStatCo(FNode X, FNode Y)
            : this(X, Y, new FNodeValue(null, Cell.OneValue(X.ReturnAffinity())), PredicateFactory.IsNotNull(X))
        {
        }

        public override List<int> FieldRefs
        {

            get
            {
                List<int> refs = new List<int>();
                refs.AddRange(FNodeAnalysis.AllFieldRefs(this._F.Node));
                refs.AddRange(FNodeAnalysis.AllFieldRefs(this._MapX));
                refs.AddRange(FNodeAnalysis.AllFieldRefs(this._MapY));
                refs.AddRange(FNodeAnalysis.AllFieldRefs(this._MapW));
                return refs;
            }

        }

        public override Record Initialize()
        {
            /*
             * 0: weight sum
             * 1: x sum
             * 2: x sum2
             * 3: y sum
             * 4: y sum2
             * 5: x * y
             * 
             */
            return Record.Stitch
            (
                Cell.ZeroValue(this._MapW.ReturnAffinity()), // weight
                Cell.ZeroValue(this._MapX.ReturnAffinity()), // avg x
                Cell.ZeroValue(this._MapX.ReturnAffinity()), // avg x^2
                Cell.ZeroValue(this._MapY.ReturnAffinity()), // avg y
                Cell.ZeroValue(this._MapY.ReturnAffinity()), // avg y^2
                Cell.ZeroValue(this._MapY.ReturnAffinity())  // avg xy
            );
        }

        public override void Accumulate(Record WorkData)
        {

            if (!this._F.Render()) return;

            Cell a = this._MapW.Evaluate();
            Cell b = WorkData[0];
            Cell c = this._MapX.Evaluate();
            Cell d = WorkData[1];
            Cell e = this._MapY.Evaluate();
            Cell f = WorkData[3];

            if (!a.IsNull && !b.IsNull && !c.IsNull && !d.IsNull && !e.IsNull && !f.IsNull)
            {
                WorkData[0] += a;
                WorkData[1] += a * c;
                WorkData[2] += a * c * c;
                WorkData[3] += a * e;
                WorkData[4] += a * e * e;
                WorkData[5] += a * c * e;
            }
            else if (b.IsNull)
            {
                WorkData[0] = a;
                WorkData[1] = a * c;
                WorkData[2] = a * c * c;
                WorkData[3] = a * e;
                WorkData[4] = a * e * e;
                WorkData[5] = a * c * e;
            }

        }

        public override void Merge(Record WorkData, Record MergeIntoWorkData)
        {

            Cell a = WorkData[0];
            Cell b = MergeIntoWorkData[0];
            Cell c = WorkData[1];
            Cell d = MergeIntoWorkData[1];
            Cell e = WorkData[3];
            Cell f = MergeIntoWorkData[3];

            if (!a.IsNull && !b.IsNull && !c.IsNull && !d.IsNull && !e.IsNull && !f.IsNull)
            {
                MergeIntoWorkData[0] += WorkData[0];
                MergeIntoWorkData[1] += WorkData[1];
                MergeIntoWorkData[2] += WorkData[2];
                MergeIntoWorkData[3] += WorkData[3];
                MergeIntoWorkData[4] += WorkData[4];
                MergeIntoWorkData[5] += WorkData[5];
            }
            else if (!a.IsNull && !c.IsNull && !e.IsNull && b.IsNull)
            {
                MergeIntoWorkData[0] = WorkData[0];
                MergeIntoWorkData[1] = WorkData[1];
                MergeIntoWorkData[2] = WorkData[2];
                MergeIntoWorkData[3] = WorkData[3];
                MergeIntoWorkData[4] = WorkData[4];
                MergeIntoWorkData[5] = WorkData[5];
            }

        }

        public override void AssignRegister(Shire.Register Memory)
        {
            base.AssignRegister(Memory);
            this._MapX.AssignRegister(Memory);
            this._MapY.AssignRegister(Memory);
            this._MapW.AssignRegister(Memory);
        }

        public override int Size()
        {
            return this._MapX.DataSize();
        }

    }

}




