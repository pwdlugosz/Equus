using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Calabrese;

namespace Equus.Nokota
{

    public sealed class AggregateFreq : Aggregate
    {

        private FNode _M;
        private Predicate _G;

        public AggregateFreq(FNode M, Predicate F, Predicate G)
            : base(CellAffinity.DOUBLE, F)
        {
            this._M = M;
            this._G = G;
            this._Sig = 2;
        }

        public AggregateFreq(FNode M, Predicate G)
            : this(M, Predicate.TrueForAll, G)
        {
        }

        public AggregateFreq(Predicate F, Predicate G)
            : this(new FNodeValue(null, Cell.OneValue(CellAffinity.DOUBLE)), F, G)
        {
        }

        public AggregateFreq(Predicate G)
            : this(Predicate.TrueForAll, G)
        {
        }

        public override List<int> FieldRefs
        {
            get
            {
                List<int> refs = new List<int>();
                refs.AddRange(FNodeAnalysis.AllFieldRefs(this._F.Node));
                refs.AddRange(FNodeAnalysis.AllFieldRefs(this._M));
                refs.AddRange(FNodeAnalysis.AllFieldRefs(this._G.Node));
                return refs;
            }
        }

        public override Record Initialize()
        {

            /*
             * 0: denominator accumulator
             * 1: numerator accumulator
             */
            return Record.Stitch
            (
                Cell.ZeroValue(CellAffinity.DOUBLE), Cell.ZeroValue(CellAffinity.DOUBLE)
            );
        }

        public override void Accumulate(Record WorkData)
        {

            if (!this._F.Render()) return;

            // denominator //
            WorkData[0]++;
            
            // numerator //
            if (this._G.Render())
                WorkData[1]++;

        }

        public override void Merge(Record WorkData, Record MergeIntoWorkData)
        {
            MergeIntoWorkData[0] += WorkData[0];
            MergeIntoWorkData[1] += WorkData[1];  
        }

        public override Cell Evaluate(Record WorkData)
        {
            // If 0 / 0 then null //
            if (WorkData[0] == Cell.ZeroValue(CellAffinity.DOUBLE))
                return new Cell(CellAffinity.DOUBLE);

            // return m / n //
            return WorkData[1] / WorkData[0];

        }

        public override void AssignRegister(Shire.Register Memory)
        {
            base.AssignRegister(Memory);
            this._M.AssignRegister(Memory);
            this._G.AssignRegister(Memory);
        }

        public override Aggregate CloneOfMe()
        {
            return new AggregateFreq(this._M.CloneOfMe(), this._F.CloneOfMe(), this._G.CloneOfMe());
        }

    }

}
