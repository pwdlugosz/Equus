using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Calabrese;
using Equus.Gidran;
using Equus.Shire;

namespace Equus.Thoroughbred.ARizenTalent
{

    public sealed class LinearRegression : RegressionModel
    {

        public LinearRegression(string Name, DataSet Data, Predicate Where, FNode Expected, FNodeSet Actual, FNode Weight)
            : base(Name, Data, Where, Expected, Actual, Weight)
        {
        }

        public LinearRegression(string Name, DataSet Data, Predicate Where, FNode Expected, FNodeSet Actual)
            : base(Name,  Data, Where, Expected, Actual)
        {
        }

        public override void Render()
        {
            this._Beta = this.OrdinaryLeastSquares();
            this.BuildSS(this._data, this._where);
        }

        public override void PartitionedRender(int Partitions)
        {
            this._Beta = this.PartitionedOrdinaryLeastSquares(Partitions);
            this.BuildSS(this._data, this._where);
        }

        public override FNode ModelExpected(FNodeSet Inputs)
        {

            if (Inputs.Count != this._XValue.Count)
                throw new ArgumentException("The inputs passed are not the same size as the model inputs");

            FNode n = Inputs.Nodes.First().CloneOfMe() * FNodeFactory.Value(this.Beta[0]);

            for (int i = 1; i < Inputs.Count; i++)
            {

                n += Inputs[i].CloneOfMe() * FNodeFactory.Value(this.Beta[i]);

            }

            return n;

        }

    }


}
