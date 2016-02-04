using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Calabrese;
using Equus.Gidran;

namespace Equus.Thoroughbred.ARizenTalent
{

    public sealed class LinearRegression : RegressionModel
    {

        public LinearRegression(string Name, FNode Expected, FNodeSet Actual, FNode Weight)
            : base(Name, Expected, Actual, Weight)
        {
        }

        public LinearRegression(string Name, FNode Expected, FNodeSet Actual)
            : base(Name, Expected, Actual)
        {
        }

        public override void Render(Horse.DataSet Data, Predicate Where)
        {
            this._Beta = this.OrdinaryLeastSquares(Data, Where);
            //this.BuildSS(Data, Where);
        }

        public override void PartitionedRender(DataSet Data, Predicate Where, int Partitions)
        {
            this._Beta = this.PartitionedOrdinaryLeastSquares(Data, Where, Partitions);
            //this.BuildSS(Data, Where);
        }

    }


}
