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

    public sealed class GeneralizedLinearModel : RegressionModel
    {

        private Lambda _Link;

        public GeneralizedLinearModel(string Name, DataSet Data, Predicate Where, FNode Expected, FNodeSet Actual, FNode Weight, Lambda LinkFunction)
            : base(Name, Data, Where, Expected, Actual, Weight)
        {

            int val = IsCorrectLink(LinkFunction);
            if (val == -1)
                throw new Exception("Link function must have exactly one argument");
            else if (val == -2)
                throw new Exception("Link function is not differentiable");
            this._Link = LinkFunction;
 
        }

        public GeneralizedLinearModel(string Name, DataSet Data, Predicate Where, FNode Expected, FNodeSet Actual, Lambda LinkFunction)
            : this(Name, Data, Where, Expected, Actual, FNodeFactory.Value(1D), LinkFunction)
        {
        }

        public FNode LinkValue
        {
            get
            {
                return this._Link.Bind(new List<FNode>() { this.LinearPredictor });
            }
        }

        public FNode LinkGradientValue
        {

            get
            {
                Lambda dx = this._Link.Gradient("dx", this._Link.Pointers.First());
                return dx.Bind(new List<FNode>() { this.LinearPredictor });
            }

        }

        public override RecordSet ModelData
        {
            get
            {
                RecordSet rs = base.ModelData;
                rs.AddData("Itterations", (double)this._ActualIterations);
                return rs;
            }
        }

        // Overrides //
        /// <summary>
        /// Fits a linear model
        /// </summary>
        /// <param name="Data">The data to calibrate the model with</param>
        /// <param name="Where">The filter to apply to the calibration</param>
        public override void Render()
        {

            // Set up support variables //
            bool converge = false;

            // Get an interim beta //
            CellVector interim_beta = this.OrdinaryLeastSquares();
            
            // Main Loop //
            for (int i = 0; i < this.MaximumIterations; i++)
            {

                // Accumulate the beta //
                this._Beta = interim_beta;

                // Get the function, gradent, weight //
                FNode nu = this.LinearPredictor.CloneOfMe();
                FNode fx = this.LinkValue.CloneOfMe();
                FNode dx = this.LinkGradientValue.CloneOfMe();
                FNode wx = this.Weight.CloneOfMe();
                FNode ys = this._YValue.CloneOfMe();
                FNodeSet xs = this._XValue.CloneOfMe();

                // Set up a memory register and assign each node to this register //
                StaticRegister memory = new StaticRegister(null);
                nu.AssignRegister(memory);
                fx.AssignRegister(memory);
                dx.AssignRegister(memory);
                wx.AssignRegister(memory);
                ys.AssignRegister(memory);
                xs.AssignRegister(memory);

                // Set up the support vector //
                this._Support = new CellVector(this.ParameterCount, CellValues.ZERO_DOUBLE);

                // Loop through all records in the table //
                RecordReader rr = this._data.OpenReader(this._where);
                while (!rr.EndOfData)
                {

                    // Read Record //
                    memory.Assign(rr.ReadNext());

                    // Get nu //
                    Cell nu_value = nu.Evaluate();

                    // Get F(nu) //
                    Cell f_of_nu = fx.Evaluate();
                    
                    // Get F'(nu) //
                    Cell f_prime_of_nu = dx.Evaluate();

                    // Get the weight //
                    Cell weight = wx.Evaluate();

                    // Get the actual //
                    Cell actual = ys.Evaluate();

                    // Get the error //
                    Cell error = actual - f_of_nu;

                    // Get each linear component of nu //
                    Record linear_vector = xs.Evaluate();

                    // Do scoring //
                    Cell Newton =
                        Math.Abs(f_prime_of_nu.DOUBLE) > this._Epsilon ?
                        nu_value + error / f_prime_of_nu :
                        nu_value;
                    
                    // Calculate support //
                    for (int l = 0; l < linear_vector.Count; l++)
                        this._Support[l, 0] += linear_vector[l] * Newton * weight;

                }

                // Now calculate the beta //
                interim_beta = ((!this._Design) ^ this._Support).ToVector;

                // Check square error //
                //Console.WriteLine("-- Itterations {0} --", i);
                //Console.WriteLine(this._Beta);
                if (CellVector.DotProduct(interim_beta - this._Beta, interim_beta - this._Beta).DOUBLE <= this._ChangeThreshold)
                {
                    this._Beta = interim_beta;
                    this._ActualIterations = i;
                    converge = true;
                    break;
                }

            }

            // We did not converge //
            if (!converge)
                this._ActualIterations = this.MaximumIterations + 1;

            // Set up the SSE //
            this.BuildSS(this._data, this._where);

        }

        public override void PartitionedRender(int Partitions)
        {
            throw new NotImplementedException();
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

            return this._Link.Bind(new List<FNode>() { n });

        }

        /// <summary>
        /// Returns a string a summarizing the key statitics of the model
        /// </summary>
        /// <returns>A string summarizing the model</returns>
        public override string Statistics()
        {

            /*
             * Meta Data:
             * ---------------------------------------------
             * Unparsed expected value
             * Unparsed actual value
             * Model type (affinity)
             * Model fit algorithm
             * Itterations
             * SSE
             * AIC
             * BIC
             * ? if the model is linear, then rsq
             * Parameter Data:
             * ---------------------------------------------
             * Name
             * Value
             * Std Error
             * T-Value
             * P-Value
             * Lower 95%
             * Upper 95%
             * 
             */

            // Build string-builder //
            StringBuilder sb = new StringBuilder();

            // Build Meta-Data //
            sb.AppendLine("Type : Generalized Linear Model");
            sb.AppendLine("YValue : " + this._YValue.Unparse(null));
            sb.AppendLine("SSE : " + Math.Round(this.SSE, 3).ToString());
            sb.AppendLine("AIC : " + Math.Round(this.AIC, 3).ToString());
            sb.AppendLine("BIC : " + Math.Round(this.BIC, 3).ToString());
            sb.AppendLine("R^2 : " + Math.Round(this.RSQ, 3).ToString());
            sb.AppendLine("Itterations : " + this._ActualIterations.ToString());
            sb.AppendLine("Link : " + this._Link.Unparse(null));
            sb.AppendLine("Gradient : " + this._Link.Gradient("dx", this._Link.Pointers.First()).Unparse(null));
            sb.AppendLine("Linear Vector : " + this._XValue.Unparse(null));

            // Paramter data //
            CellVector means = this._Beta;
            CellVector stderror = this.ParameterStandardError;
            CellVector tstat = this.TStatistic;
            CellVector pvalue = this.PValue;
            CellVector l95 = this.LowerBound95;
            CellVector u95 = this.UpperBound95;

            // Build Paramter Data //
            sb.AppendLine("Name\t Value\t Stdev\t TStat\t PValue\t L95%\t U95%");
            for (int i = 0; i < this._Beta.RowCount; i++)
            {

                double d_mean = Math.Round(means[i].DOUBLE, 4);
                double d_std = Math.Round(stderror[i].DOUBLE, 4);
                double d_tstat = Math.Round(tstat[i].DOUBLE, 4);
                double d_pvalue = Math.Round(pvalue[i].DOUBLE, 4);
                double d_l95 = Math.Round(l95[i].DOUBLE, 4);
                double d_u95 = Math.Round(u95[i].DOUBLE, 4);

                string t = string.Format("{0}\t {1}\t {2}\t {3}\t {4}\t {5}\t {6}",
                    this._XValue.Alias(i),
                    d_mean,
                    d_std,
                    (Math.Abs(d_tstat) > 10) ? ">10" : d_tstat.ToString(),
                    d_pvalue,
                    d_l95,
                    d_u95);

                sb.AppendLine(t);
            }

            return sb.ToString();

        }

        /// <summary>
        /// Builds the sum of square errors for a linear model
        /// </summary>
        /// <param name="Data">The data set to build the SS over</param>
        /// <param name="Where">Any filter that needs to be applied to the data</param>
        /// <param name="X_Variables">The 'expected' node tree</param>
        /// <param name="Y_Variables">The 'actual' node tree to be compared to the 'expected'</param>
        protected override void BuildSS(DataSet Data, Predicate Where)
        {

            /* * * * * * * * * * * * * * * * * * * * * * * * *
             * 
             *  SSTO = (y - avg(y)) ^ 2
             *  SSE = (y - model(y)) ^ 2
             *  SSR = (model(y) - avg(y)) ^ 2
             * 
             * * * * * * * * * * * * * * * * * * * * * * * * */

            // Create variable indicies //
            double ExpectedValue = 0;
            double ActualValue = 0;
            double WeightValue = 0;
            double InverseWeight = 0;

            // Nodes //
            FNode actual = this._YValue.CloneOfMe();
            FNode expected = this.LinkValue.CloneOfMe();
            FNode weight = this._WValue.CloneOfMe();
            FNode weight2 = this.LinkGradientValue.CloneOfMe();

            // Build a register //
            StaticRegister memory = new StaticRegister(null);
            actual.AssignRegister(memory);
            expected.AssignRegister(memory);
            weight.AssignRegister(memory);
            weight2.AssignRegister(memory);

            // Reallocate cursor //
            RecordReader rc = Data.OpenReader(Where);

            // ERROR matricies //
            this._SSTO = 0;
            this._SSR = 0;
            this._SSE = 0;
            double x = 0D;
            double x2 = 0D;
            double wi = 0D;

            // Main Loop - Cycle through all observations //
            while (!rc.EndOfData)
            {

                // Read record //
                memory.Assign(rc.ReadNext());
                ExpectedValue = expected.Evaluate().DOUBLE;
                ActualValue = actual.Evaluate().DOUBLE;
                WeightValue = weight.Evaluate().DOUBLE;
                InverseWeight = weight2.Evaluate().DOUBLE;

                // Set errors //
                wi += WeightValue / InverseWeight;
                x += ActualValue * WeightValue;
                x2 += ActualValue * ActualValue * WeightValue;
                this._SSTO += Math.Pow(this._Mean - ActualValue, 2) * WeightValue / InverseWeight;
                this._SSR += Math.Pow(this._Mean - ExpectedValue, 2) * WeightValue / InverseWeight;
                this._SSE += Math.Pow(ExpectedValue - ActualValue, 2) * WeightValue / InverseWeight;
                this._WeightSum += WeightValue;
                this._WeightSum2 += WeightValue * WeightValue;

            }
            // end main loop //

            // Set the means //
            this._Mean = x / this._WeightSum;
            this._Variance = x2 / this._WeightSum - (this._Mean * this._Mean);
            this._SSTO = (this.IsCorrected ? x2 : this._Variance * wi);
            this._SSR = this._SSTO - this._SSE;
                
            // Build the parameter variance //
            Cell mse = new Cell((1 - this._WeightSum2 / (this._WeightSum * this._WeightSum)) * (this._SSE / this._WeightSum));
            for (int i = 0; i < this.ParameterCount; i++)
            {
                this._ParameterVariance[i] = Cell.CheckDivide(mse, this._Design[i, i]);
            }

        }

        // Statics //
        /// <summary>
        /// Tests a lambda to determine if it is a valid link function
        /// (1). The lambda must have exactly one pointer node
        /// (2). The lambda must be differentiable
        /// </summary>
        /// <param name="Link">A lambda that represents a link function</param>
        /// <returns></returns>
        public static int IsCorrectLink(Lambda Link)
        {

            // Check for one DISTINCT pointer //
            if (Link.Pointers.Count != 1)
                return -1;

            // Check that the link function is differntiable //
            string pointer = Link.Pointers.First();
            if (!Link.IsDifferntiable(pointer))
                return -2;

            // Return //
            return 0;

        }
        
    }

}
