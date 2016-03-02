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

    public sealed class NonlinearRegressionModel : RegressionModel
    {

        private FNode _equation;
        private Dictionary<string, int> _map;
        private Cell _scale = new Cell(10D);
        private double _down_sclale = 0.50;
        private double _up_scale = 1.20;

        public NonlinearRegressionModel(string Name, DataSet Data, Predicate Where, FNode YValue, FNode Equation, FNode Weight)
            : base(Name, Data, Where, YValue, null, Weight)
        {
            
            // Save the equation //
            this._equation = Equation;

            // Create the parameter map //
            this._map = NonlinearRegressionModel.MapParameters(this._equation);

            // Set the X nodes equal to the partial gradients //
            this._XValue = NonlinearRegressionModel.Gradients(this._equation, this._map);

            // Need to initialize the matricies and vectors since the base ctor would not because XValues were passed as null //
            this.InitializeMatricies(this._XValue.Count);

            // Set the model to not strict //
            this.IsStrict = false;

            // Allow for more itterations //
            this._MaximumIterations = 20;

        }

        public NonlinearRegressionModel(string Name, DataSet Data, Predicate Where, FNode YValue, FNode Equation)
            : this(Name, Data, Where, YValue, Equation, FNodeFactory.Value(1D))
        {
        }

        /// <summary>
        /// Returns an equation with pointer refs in place of the regression parameters
        /// </summary>
        public FNode RawEquation
        {
            get
            {
                return this._equation;
            }
        }

        /// <summary>
        /// Returns an equation bound to the current regression paramter vector
        /// </summary>
        public FNode BoundEquation
        {
            get
            {
                return NonlinearRegressionModel.BindNode(this._equation, this._Beta, this._map);
            }
        }

        /// <summary>
        /// Returns a node that represents the error term: Y - YHat, where YHat is bound to the current parameter vector
        /// </summary>
        public FNode Error
        {
            get
            {
                return this.Expected - this.BoundEquation;
            }
        }

        /// <summary>
        /// If True, the offsetting term is Trace(Design) * Scale, otherwise it is I * Scale; by default the model is not strict
        /// </summary>
        public bool IsStrict
        {
            get;
            set;
        }

        /// <summary>
        /// The scaling variable
        /// </summary>
        public Cell Scale
        {
            get { return this._scale; }
            set { this._scale = value; }
        }

        public override RecordSet ModelData
        {
            get
            {
                RecordSet rs = base.ModelData;
                rs.AddData("Itterations", (double)this._ActualIterations);
                rs.AddData("Ending Scale", this._scale.DOUBLE);
                return rs;
            }
        }

        public override void Render()
        {

            // Check that beta has been initialized //
            if (CellMatrix.SumSquare(this._Beta) == Cell.ZeroValue(this._Beta.Affinity))
                this.InitializeParameters(20161105);

            // Create a convergence check variable //
            bool Converged = false;

            // Main Loop //
            for (int i = 0; i < this.MaximumIterations; i++)
            {

                // Bind the current parameters to the partial gradient vector //
                FNodeSet x = NonlinearRegressionModel.BindNodes(this._XValue, this._Beta, this._map);

                // Create the error node //
                FNode y = this.Error;

                // Hold the current beta because the OLS method will erase the current beta vector //
                CellVector hold_beta = this._Beta;

                // Capture the current sse to use to adjust the Lambda //
                double lag_sse = this._SSE;

                // Construct the design matrix //
                this._NonNullObservations
                    = RegressionModel.BuildDesignSupport(this._data.OpenReader(this._where), x, y, this._WValue, this._Design, this._Support, this.BoundEquation, out this._SSE);
                
                // Create lambda x I or Trace(Design) * lambda //
                CellMatrix ILambda;
                if (this.IsStrict)
                    ILambda = CellMatrix.Trace(this._Design) * this._scale;
                else
                    ILambda = CellMatrix.Identity(this.ParameterCount, CellAffinity.DOUBLE) * this._scale;

                // Create the beta with the ridge penalty (Xt x W x X + Lambda x I)^-1 Xt x Y //
                CellVector interim_beta = (CellMatrix.Invert(this._Design + ILambda) ^ this._Support).ToVector;

                // Update the beta to be our old beta + the beta weight change //
                this._Beta = interim_beta + hold_beta;

                // Check square error //
                if (CellVector.DotProduct(interim_beta, interim_beta).DOUBLE <= this._ChangeThreshold)
                {
                    this._ActualIterations = i;
                    Converged = true;
                    break;
                }

                /*
                 * Check the SSE to see if we need to adjust the lambda:
                 * If SSE < lag_sse: decmrent Lambda by discounting 50%
                 * If SSE > lag_sse: increment Lambda by inflating by 120%
                 * 
                 */
                if (this._SSE < lag_sse)
                    this._scale.DOUBLE *= this._down_sclale;
                else
                    this._scale.DOUBLE *= this._up_scale;

                Console.WriteLine("Dev : SSE {0} | Itterations {1} | Lambda {2}", Math.Round(this._SSE, 3), i, this._scale);

            }

            // We did not converge //
            if (!Converged)
                this._ActualIterations = this.MaximumIterations + 1;

            // Calculate the SSE //
            this.BuildSS(this._data, this._where);

        }

        public override void PartitionedRender(int Partitions)
        {
            throw new NotImplementedException();
        }

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
            
            // Nodes //
            FNode actual = this._YValue.CloneOfMe();
            FNode expected = this.BoundEquation;
            FNode weight = this._WValue.CloneOfMe();
            
            // Build a register //
            StaticRegister memory = new StaticRegister(null);
            actual.AssignRegister(memory);
            expected.AssignRegister(memory);
            weight.AssignRegister(memory);

            // Reallocate cursor //
            RecordReader rc = Data.OpenReader(Where);

            // ERROR matricies //
            this._SSTO = 0;
            this._SSR = 0;
            this._SSE = 0;
            double x = 0D;
            double x2 = 0D;

            // Main Loop - Cycle through all observations //
            while (!rc.EndOfData)
            {

                // Read record //
                memory.Assign(rc.ReadNext());
                ExpectedValue = expected.Evaluate().DOUBLE;
                ActualValue = actual.Evaluate().DOUBLE;
                WeightValue = weight.Evaluate().DOUBLE;
                
                // Set errors //
                x += ActualValue * WeightValue;
                x2 += ActualValue * ActualValue * WeightValue;
                this._SSE += Math.Pow(ExpectedValue - ActualValue, 2) * WeightValue;
                this._WeightSum += WeightValue;
                this._WeightSum2 += WeightValue * WeightValue;

            }
            // end main loop //

            // Set the means //
            this._Mean = x / this._WeightSum;
            this._Variance = x2 / this._WeightSum - (this._Mean * this._Mean);
            this._SSTO = (this.IsCorrected ? x2 : this._Variance * this._WeightSum);
            this._SSR = this._SSTO - this._SSE;
                
            // Build the parameter variance //
            Cell mse = new Cell((1 - this._WeightSum2 / (this._WeightSum * this._WeightSum)) * (this._SSE / this._WeightSum));
            for (int i = 0; i < this.ParameterCount; i++)
            {
                this._ParameterVariance[i] = Cell.CheckDivide(mse, this._Design[i, i]);
            }

        }

        public override FNode ModelExpected(FNodeSet Inputs)
        {
            return ModelExpected(Inputs.Nodes.First());
        }

        public FNode ModelExpected(FNode Equation)
        {

            return NonlinearRegressionModel.BindNode(Equation, this._Beta, this._map);

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
            sb.AppendLine("Equation : " + this._equation.Unparse(null));

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

        // Static Support //
        public static Dictionary<string, int> MapParameters(FNode Equation)
        {

            // Get a list of all the pointers //
            List<string> pointers = FNodeAnalysis.AllPointersRefs(Equation).Distinct().ToList();

            // Build the mapping //
            Dictionary<string, int> mapping = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < pointers.Count; i++)
                mapping.Add(pointers[i], i);

            return mapping;

        }

        public static FNodeSet Gradients(FNode Equation, Dictionary<string,int> Map)
        {

            FNodeSet nodes = new FNodeSet();
            foreach (KeyValuePair<string, int> kv in Map)
            {
                FNode dx = FNodeGradient.Gradient(Equation, kv.Key);
                nodes.Add(kv.Key, dx);
            }
            return nodes;

        }

        public static FNode BindNode(FNode Equation, CellVector Bindings, Dictionary<string, int> Map)
        {

            FNode t = Equation.CloneOfMe();
            foreach(KeyValuePair<string,int> kv in Map)
            {
                string name = kv.Key;
                int idx = kv.Value;
                FNode b = new FNodeValue(null, Bindings[idx]);
                t = FNodeCompacter.Bind(t, b, name);
            }

            return t;

        }

        public static FNodeSet BindNodes(FNodeSet Gradients, CellVector Parameters, Dictionary<string, int> Map)
        {

            if (Gradients.Count != Parameters.Count)
                throw new ArgumentException("The node collection and parameter vector must be the same size");

            FNodeSet nodes = new FNodeSet();
            for (int i = 0; i < Gradients.Count; i++)
            {
                FNode t = NonlinearRegressionModel.BindNode(Gradients[i], Parameters, Map);
                nodes.Add(Gradients.Alias(i), t);
            }

            return nodes;

        }

    }

}
