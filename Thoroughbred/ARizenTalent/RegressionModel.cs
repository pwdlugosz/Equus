using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Calabrese;
using Equus.Horse;
using Equus.Gidran;
using Equus.Shire;
using Equus.Mustang;

namespace Equus.Thoroughbred.ARizenTalent
{
    
        /// <summary>
        /// Represents an abstract class shell that houses the regression model
        /// </summary>
        public abstract class RegressionModel
        {

            // DataSet Set Variables //
            protected FNode _YValue;
            protected FNodeSet _XValue;
            protected FNode _WValue;

            // Other support //
            protected int _MaximumIterations = 10;
            protected int _ActualIterations = 0;
            protected double _ChangeThreshold = 0.001;
            protected double _Epsilon = 0.001;

            // Matrix Support //
            protected CellMatrix _Design;
            protected CellVector _Beta, _Support, _ParameterVariance;
            protected double _SSTO, _SSR, _SSE, _Mean, _Variance;
            protected double _WeightSum, _WeightSum2, _NonNullObservations;

            // Other helpers //
            protected static Cell _ZeroValue = new Cell(0D);
            protected static Cell _N95 = new Cell(1.96);
            
            // -- Constuctor -- //
            public RegressionModel(string Name, FNode YValue, FNodeSet XValue, FNode Weight)
            {

                // Set the inputs //
                this._YValue = YValue;
                this._XValue = XValue;
                this._WValue = Weight;
                this.Name = Name;

                // Initialize //
                if (this._XValue != null)
                    this.InitializeMatricies(this._XValue.Count);

            }

            public RegressionModel(string Name, FNode Expected, FNodeSet Actual)
                : this(Name, Expected, Actual, FNodeFactory.Value(1D))
            {
            }

            // Vectors //
            /// <summary>
            /// Returns the paramter matrix
            /// </summary>
            public CellVector Beta
            {
                get 
                { 
                    return this._Beta; 
                }
                set
                {
                    if (value.Count != this._Beta.Count)
                        throw new Exception(string.Format("Beta passed is not the correct size; Required {0}; Passed {1}", this._Beta.Count, value.Count));
                    this._Beta = value;
                }
            }

            /// <summary>
            /// Returns the design matrix Xt x W x X
            /// </summary>
            public CellMatrix Design
            {
                get { return this._Design; }
            }

            /// <summary>
            /// Returns the right side support Xt x W x Y
            /// </summary>
            public CellVector Support
            {
                get { return this._Support; }
            }

            /// <summary>
            /// Returns the MSE / Design[i,i]
            /// </summary>
            public CellVector ParameterVariance
            {
                get { return this._ParameterVariance; }
            }

            /// <summary>
            /// Returns a vector containing the standard error of each parameter
            /// </summary>
            public CellVector ParameterStandardError
            {
                get 
                { 
                    return CellVector.ForEach(this._ParameterVariance, Cell.Sqrt, CellAffinity.DOUBLE); 
                }
            }

            /// <summary>
            /// Returns a vector containing the T-Statitistic; note, HORSE currently uses the normal distribution not the T-distribution
            /// </summary>
            public CellVector TStatistic
            {
                get 
                {

                    CellVector v = new CellVector(this._ParameterVariance.Count, CellAffinity.DOUBLE);
                    Cell ubound = new Cell(10D);
                    Cell lbound = new Cell(-10D);
                    for (int i = 0; i < v.Count; i++)
                    {
                        if (this._ParameterVariance[i].DOUBLE == 0)
                            v[i] = (this._Beta[i].DOUBLE < 0D ? new Cell(-10D) : new Cell(10D));
                        else
                            v[i] = Cell.Max(Cell.Min(ubound, this._Beta[i] / this._ParameterVariance[i]), lbound);
                    }
                    return v;

                }
            }

            /// <summary>
            /// Returns a vector containing the results of a T-Test
            /// </summary>
            public CellVector PValue
            {
                get 
                {
                    
                    return CellVector.ForEach(this.TStatistic, 
                        (x) => 
                        {
                            return new Cell(Numerics.SpecialFunctions.NormalCDF(x.DOUBLE));
                        },
                        CellAffinity.DOUBLE
                        ); 

                }
            }

            /// <summary>
            /// Returns a vector containing the lower 95% CI for the paramters
            /// </summary>
            public CellVector LowerBound95
            {
                get { return this._Beta - this._ParameterVariance * _N95; }
            }

            /// <summary>
            /// Returns a vector containing the upper 95% CI for the paramters
            /// </summary>
            public CellVector UpperBound95
            {
                get { return this._Beta + this._ParameterVariance * _N95; }
            }

            /// <summary>
            /// Parameter summary data
            /// </summary>
            public RecordSet ParameterData
            {

                get
                {

                    RecordSet rs = new RecordSet("ID INT, NAME STRING, MEAN DOUBLE, STDEV DOUBLE, TVALUE DOUBLE, PVALUE DOUBLE, LOWER95 DOUBLE, UPPER95 DOUBLE");
                    rs.SetGhostName(this.Name + "_PARAMETERS");
                    for (int i = 0; i < this._Beta.Count; i++)
                    {

                        RecordBuilder rb = new RecordBuilder();
                        rb.Add(i);
                        rb.Add(this._XValue.Alias(i));
                        rb.Add(this.Beta[i]);
                        rb.Add(this.ParameterVariance[i]);
                        rb.Add(this.TStatistic[i]);
                        rb.Add(this.PValue[i]);
                        rb.Add(this.LowerBound95[i]);
                        rb.Add(this.UpperBound95[i]);
                        rs.Add(rb.ToRecord());
                    }

                    return rs;

                }

            }

            /// <summary>
            /// Returns a record set loaded with model statistics
            /// </summary>
            public virtual RecordSet ModelData
            {

                get
                {

                    RecordSet rs = new RecordSet("ELEMENT STRING, VALUE DOUBLE");
                    rs.SetGhostName(this.Name + "_MODEL");
                    rs.AddData("SSTO", this.SSTO);
                    rs.AddData("SSR", this.SSR);
                    rs.AddData("SSE", this.SSE);
                    rs.AddData("MEAN", this.ResponseMean);
                    rs.AddData("VARIANCE", this.ResponseVariance);
                    rs.AddData("RSQ", this.RSQ);
                    rs.AddData("AIC", this.AIC);
                    rs.AddData("BIC", this.BIC);
                    rs.AddData("NON_NULL_OBS", this._NonNullObservations);
                    return rs;

                }

            }

            // Others //
            /// <summary>
            /// If true, then the SSTO is SUMSQ(X - mean), otherwise SSTO = SUMSQ(X)
            /// </summary>
            public bool IsCorrected
            {
                get;
                protected set;
            }

            /// <summary>
            /// Returns the total sums of squares:
            /// If the 'IsCorrected' is true, then SSTO = SUM((X - mean) * (X - mean))
            /// otherwise, SSTO = SUM(X * X)
            /// </summary>
            public double SSTO
            {
                get { return this._SSTO; }
            }

            /// <summary>
            /// Returns the sums of squares regression, which is SSTO - SSE
            /// </summary>
            public double SSR
            {
                get { return this._SSR; }
            }

            /// <summary>
            /// Returns the sums of square errors, SUM(X - E(X))^2
            /// </summary>
            public double SSE
            {
                get { return this._SSE; }
            }

            /// <summary>
            /// Weighted response mean
            /// </summary>
            public double ResponseMean
            {
                get { return this._Mean; }
            }

            /// <summary>
            /// Weighted response variance
            /// </summary>
            public double ResponseVariance
            {
                get { return this._Variance; }
            }

            /// <summary>
            /// Returns the sum of all weights
            /// </summary>
            public double WeightSum
            {
                get { return this._WeightSum; }
            }

            /// <summary>
            /// Akike's Information Criteria: 2 * K - 2 * ln(SSE / n)
            /// </summary>
            public double AIC
            {
                get { return 2 * this.ParameterCount - 2 * Math.Log(this.SSE / this._WeightSum); }
            }

            /// <summary>
            /// Bayesian Information Criteria: - 2 * ln(SSE / n) + k * ln(n)
            /// </summary>
            public double BIC
            {
                get { return -2 * Math.Log(this.SSE / this._WeightSum) + this.ParameterCount * Math.Log(this._NonNullObservations); }
            }

            /// <summary>
            /// 1 - SSE / SSTO
            /// </summary>
            public double RSQ
            {
                get { return 1 - this._SSE / this._SSTO; }
            }

            /// <summary>
            /// Returns the count of beta parameters
            /// </summary>
            public int ParameterCount
            {
                get { return this._Beta.RowCount; }
            }

            /// <summary>
            /// Represents the equation being modeled
            /// </summary>
            public FNode Expected
            {
                get { return this._YValue; }
            }

            /// <summary>
            /// Represents the variable or equation being modeled
            /// </summary>
            public FNodeSet Actual
            {
                get { return this._XValue; }
            }

            /// <summary>
            /// Represents the weighting variable or equation to use in the fit procedure
            /// </summary>
            public FNode Weight
            {
                get { return this._WValue; }
            }

            /// <summary>
            /// Returns an FNode of the form Beta ** X
            /// </summary>
            public FNode LinearPredictor
            {

                get
                {

                    if (this._XValue.Count == 0)
                        return FNodeFactory.Value(CellAffinity.DOUBLE);
                    else if (this._XValue.Count == 1)
                        return this._XValue[0] * new FNodeValue(null, this._Beta[0]);

                    FNode t = this._XValue[0] * new FNodeValue(null, this._Beta[0]);
                    for (int i = 1; i < this._XValue.Count; i++)
                        t = t + this._XValue[i] * new FNodeValue(null, this._Beta[i]);

                    return t;

                }

            }

            /// <summary>
            /// Maximum number of itterations allowed
            /// </summary>
            public int MaximumIterations
            {
                get { return this._MaximumIterations; }
                set { this._MaximumIterations = value; }
            }

            /// <summary>
            /// The actual itterations performed
            /// </summary>
            public int ActualIterations
            {
                get { return this._ActualIterations; }
            }

            /// <summary>
            /// The name of the model
            /// </summary>
            public string Name
            {
                get;
                protected set;
            }

            // Build Support //
            /// <summary>
            /// Initializes the beta vector with random values;
            /// R = random value
            /// Beta[i] = R * Scale - Offset
            /// </summary>
            /// <param name="Seed">The seed value of the random number generator</param>
            /// <param name="Scale">Inflation factor</param>
            /// <param name="OffSet">Offseting factor</param>
            public void InitializeParameters(int Seed, double Scale, double OffSet)
            {
                Random r = new Random(Seed);
                this._Beta = CellVector.ForEach(this._Beta, (x) => { return new Cell(r.NextDouble() * Scale - OffSet); }, CellAffinity.DOUBLE);
            }

            /// <summary>
            /// Initializes the beta vector with random values;
            /// R = random value
            /// Beta[i] = R * Scale - Offset
            /// </summary>
            /// <param name="Scale">Inflation factor</param>
            /// <param name="OffSet">Offseting factor</param>
            public void InitializeParameters(double Scale, double OffSet)
            {
                this.InitializeParameters(Environment.TickCount, Scale, OffSet);
            }

            /// <summary>
            /// Initializes the beta vector with random values;
            /// R = random value
            /// Beta[i] = R * 2 - 1
            /// </summary>
            /// <param name="Seed">The seed value of the random number generator</param>
            public void InitializeParameters(int Seed)
            {
                this.InitializeParameters(Seed, 2D, 1D);
            }

            /// <summary>
            /// Initializes the beta vector with random values;
            /// R = random value
            /// Beta[i] = R * 2 - 1
            /// </summary>
            public void InitializeParameters()
            {
                this.InitializeParameters(Environment.TickCount, 2D, 1D);
            }

            /// <summary>
            /// Initializes the beta parameter with specific values
            /// </summary>
            /// <param name="Values">A dictiony of strings and doubles, where the string corrosponds to the parameter name and the double is the initial value</param>
            public void InitializeParameters(FNodeSet Values)
            {

                if (Values.Count != this._Beta.RowCount)
                    throw new ArgumentException("Expecting the values passed to match the number of beta parameters");

                for (int i = 0; i < Values.Count; i++)
                    this._Beta[i] = Values[i].Evaluate();

            }

            /// <summary>
            /// Sets up all matricies and vectors 
            /// </summary>
            /// <param name="Variables"></param>
            protected void InitializeMatricies(int Variables)
            {

                // Set matricies //
                /* 
                 * N = number of observations
                 * M = number of variables + intercept
                 * L = numner of responses
                 * 
                 * Design: M x M
                 * Support: M x L
                 * Beta: M x L
                 * 
                 */

                this._Design = new CellMatrix(Variables, Variables, _ZeroValue);
                this._Support = new CellVector(Variables, _ZeroValue);
                this._Beta = new CellVector(Variables, _ZeroValue);

                // ERROR matricies //
                this._SSTO = 0;
                this._SSR = 0;
                this._SSE = 0;
                this._ParameterVariance = new CellVector(Variables, _ZeroValue);

                // mean, variance and weight //
                this._Mean = 0;
                this._ParameterVariance = new CellVector(Variables, _ZeroValue);

            }

            /// <summary>
            /// Builds XtX and XtY in one loop over the table; this avoids the costly, and potentially memory prohibitive procedure of transposing and multiplying two
            /// matricies together.
            /// </summary>
            /// <param name="Stream">A record stream to read form</param>
            /// <param name="X_Variables">The set of all X variables</param>
            /// <param name="Y_Variables">The set of all Y variables</param>
            protected void BuildDesignSupport(RecordReader Stream)
            {

                this._NonNullObservations = RegressionModel.BuildDesignSupport(Stream, this._XValue, this._YValue, this._WValue, this._Design, this._Support);

            }

            /// <summary>
            /// Returns (Xt x W x X) ^-1 Xt x W x Y
            /// </summary>
            /// <param name="Data">The input data set</param>
            /// <param name="Where">Any filter needed to be applied to the data; if null, then defaults to the Predicate.TrueForAll</param>
            /// <param name="X_Variables">The input variables</param>
            /// <param name="Y_Variables">The response variable being modeled</param>
            /// <param name="Weight_Variable">The weight variable</param>
            /// <returns>Returns the OLS beta matrix</returns>
            protected CellVector OrdinaryLeastSquares(DataSet Data, Predicate Where)
            {

                // Finish the mean and variance calculations //
                this._Mean = this._Mean / this._WeightSum;
                this._Variance = this._Variance / this._WeightSum - this._Mean * this._Mean;

                // Build the design //
                this.BuildDesignSupport(Data.OpenReader(Where));

                // Invert design matrix and apply to beta = (XtX)^-1 XtY //
                CellVector v = (CellMatrix.Invert(this._Design) ^ this._Support).ToVector;

                return v;

            }

            protected CellVector PartitionedOrdinaryLeastSquares(DataSet Data, Predicate Where, int Partitions)
            {

                // Finish the mean and variance calculations //
                //this._Mean = this._Mean / this._WeightSum;
                //this._Variance = this._Variance / this._WeightSum - this._Mean * this._Mean;

                // Build the design //
                OLSReducer red = RegressionModel.BuildDesignSupport(Data, Where, this._XValue, this._YValue, this._WValue, this._Design, this._Support, Partitions);
                this._NonNullObservations = red.Ticks;
                this._Design = red.XtX;
                this._Support = red.XtY;

                // Invert design matrix and apply to beta = (XtX)^-1 XtY //
                CellVector v = (CellMatrix.Invert(this._Design) ^ this._Support).ToVector;

                return v;

            }

            // Abstracts //
            /// <summary>
            /// Fits the regression model
            /// </summary>
            /// <param name="Data">The dataset (table or recordset) to calibrate the model to</param>
            /// <param name="Where">The predicate to apply to reading the data</param>
            public abstract void Render(DataSet Data, Predicate Where);

            /// <summary>
            /// Fits the regression model using more than one thread
            /// </summary>
            /// <param name="Data">The dataset (table or recordset) to calibrate the model to</param>
            /// <param name="Where">The predicate to apply to reading the data</param>
            /// <param name="Partitions">The number of threads to use</param>
            public abstract void PartitionedRender(DataSet Data, Predicate Where, int Partitions);

            // Virtuals //
            /// <summary>
            /// Fits the regression model using all observations
            /// </summary>
            /// <param name="Data">The dataset (table or recordset) to calibrate the model to</param>
            public virtual void Render(DataSet Data)
            {
                this.Render(Data, Predicate.TrueForAll);
            }

            /// <summary>
            /// Fits the regression model
            /// </summary>
            /// <param name="Data">The dataset (table or recordset) to calibrate the model to</param>
            /// <param name="Partitions">The number of threads to use</param>
            public virtual void PartitionedRender(DataSet Data, int Partitions)
            {
                this.PartitionedRender(Data, Predicate.TrueForAll, Partitions);
            }

            /// <summary>
            /// Builds the sum of square errors for a linear model
            /// </summary>
            /// <param name="Data">The data set to build the SS over</param>
            /// <param name="Where">Any filter that needs to be applied to the data</param>
            /// <param name="X_Variables">The 'expected' node tree</param>
            /// <param name="Y_Variables">The 'actual' node tree to be compared to the 'expected'</param>
            protected virtual void BuildSS(DataSet Data, Predicate Where)
            {

                /* * * * * * * * * * * * * * * * * * * * * * * * *
                 * 
                 *  SSTO = (y - avg(y)) ^ 2
                 *  SSE = (y - model(y)) ^ 2
                 *  SSR = (model(y) - avg(y)) ^ 2
                 * 
                 * * * * * * * * * * * * * * * * * * * * * * * * */

                // Create variable indicies //
                double ExpectedValue;
                double ActualValue;
                double WeightValue = 0;

                // Nodes //
                FNode actual = this._YValue.CloneOfMe();
                FNode expected = this.LinearPredictor.CloneOfMe();
                FNode weight = this._WValue.CloneOfMe();
                Console.WriteLine(expected.Unparse(Data.Columns));

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

            // Overrides //
            /// <summary>
            /// Returns a string a summarizing the key statitics of the model
            /// </summary>
            /// <returns>A string summarizing the model</returns>
            public override string ToString()
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
                sb.AppendLine("Type : Linear Regression Model");
                sb.AppendLine("YValue : " + this._YValue.Unparse(null));
                sb.AppendLine("SSE : " + Math.Round(this.SSE, 3).ToString());
                sb.AppendLine("AIC : " + Math.Round(this.AIC, 3).ToString());
                sb.AppendLine("BIC : " + Math.Round(this.BIC, 3).ToString());
                sb.AppendLine("R^2 : " + Math.Round(this.RSQ, 3).ToString());

                // Paramter data //
                CellVector means = this._Beta;
                CellVector stderror = this.ParameterStandardError;
                CellVector tstat = this.TStatistic;
                CellVector pvalue = this.PValue;
                CellVector l95 = this.LowerBound95;
                CellVector u95 = this.UpperBound95;

                // Build Paramter Data //
                sb.AppendLine("Name\t Value\t Stdev\t TStat\t PValue\t L95%\t U95%");
                for(int i = 0; i < this._Beta.RowCount; i++)
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

            // Statics //
            /// <summary>
            /// Appends the design and support matricies (XtX and XtY respectively) with data from the stream passed; 
            /// note that this assumes both matricies passed are initialized with zero values, not null values; this is split into a static method to
            /// support map-reduce jobs
            /// </summary>
            /// <param name="Stream">The data source passed</param>
            /// <param name="X">The expression tree that yeilds the input variables</param>
            /// <param name="Y">The expression that yields the actual variable</param>
            /// <param name="W">The expression that yields the weight variable</param>
            /// <param name="XtX">The input design matrix</param>
            /// <param name="XtY">The input support matrix</param>
            /// <returns>An integer representing the total number of reads</returns>
            public static long BuildDesignSupport(RecordReader Stream, FNodeSet X, FNode Y, FNode W, CellMatrix XtX, CellVector XtY)
            {

                // Check the XtX is both square and matches X.RowCount //
                if (!XtX.IsSquare)
                    throw new Exception("The XtX matrix must be square");
                if (XtX.RowCount != X.Count)
                    throw new Exception("The XtX.RowCount must equal X.Count");
                if (XtX.RowCount != XtY.RowCount)
                    throw new Exception("The XtX.RowCount must equal XtY.RowCount");

                // Create a register //
                StaticRegister memory = new StaticRegister(null);

                // Tie the register to the nodes //
                X.AssignRegister(memory);
                Y.AssignRegister(memory);
                W.AssignRegister(memory);

                // Counter //
                long Traverses = 0;

                // Walk the stream //
                while (!Stream.EndOfData)
                {

                    // Read the next record into the register //
                    memory.Assign(Stream.ReadNext());

                    // Get the X vector, Y value and W value //
                    Record x_value = X.Evaluate();
                    Cell y_value = Y.Evaluate();
                    Cell w_value = W.Evaluate();

                    // Walk the rows //
                    for (int i = 0; i < XtX.RowCount; i++)
                    {

                        // Build XtX //
                        for (int j = 0; j < XtX.RowCount; j++)
                            XtX[i, j] += x_value[i] * x_value[j] * w_value;

                        // Build XtY //
                        XtY[i, 0] += x_value[i] * y_value * w_value;

                    }

                    // Increment //
                    Traverses++;

                }

                return Traverses;

            }

            public static long BuildDesignSupport(RecordReader Stream, FNodeSet X, FNode Y, FNode W, CellMatrix XtX, CellVector XtY, 
                FNode Expected, out double SSE)
            {

                // Check the XtX is both square and matches X.RowCount //
                if (!XtX.IsSquare)
                    throw new Exception("The XtX matrix must be square");
                if (XtX.RowCount != X.Count)
                    throw new Exception("The XtX.RowCount must equal X.Count");
                if (XtX.RowCount != XtY.RowCount)
                    throw new Exception("The XtX.RowCount must equal XtY.RowCount");

                // Clear sse //
                double sse = 0;

                // Create a register //
                StaticRegister memory = new StaticRegister(null);

                // Tie the register to the nodes //
                X.AssignRegister(memory);
                Y.AssignRegister(memory);
                W.AssignRegister(memory);
                Expected.AssignRegister(memory);

                // Counter //
                long Traverses = 0;

                // Walk the stream //
                while (!Stream.EndOfData)
                {

                    // Read the next record into the register //
                    memory.Assign(Stream.ReadNext());

                    // Get the X vector, Y value and W value //
                    Record x_value = X.Evaluate();
                    Cell y_value = Y.Evaluate();
                    Cell w_value = W.Evaluate();
                    Cell yhat_value = Expected.Evaluate();

                    // Set the SSE //
                    sse += Math.Pow(y_value.DOUBLE, 2D) * w_value.DOUBLE;

                    // Walk the rows //
                    for (int i = 0; i < XtX.RowCount; i++)
                    {

                        // Build XtX //
                        for (int j = 0; j < XtX.RowCount; j++)
                            XtX[i, j] += x_value[i] * x_value[j] * w_value;

                        // Build XtY //
                        XtY[i, 0] += x_value[i] * y_value * w_value;

                    }

                    // Increment //
                    Traverses++;

                }

                // Set sse //
                SSE = sse;
                
                return Traverses;

            }

            public static OLSReducer BuildDesignSupport(DataSet Data, Predicate Where, FNodeSet X, FNode Y, FNode W, CellMatrix XtX, CellVector XtY,
                int Partitions)
            {

                OLSMapFactory olsmf = new OLSMapFactory(X, Y, W, Where);
                OLSReducer olsr = new OLSReducer(X.Count);
                MRJob<OLSMapNode> olsj = new MRJob<OLSMapNode>(Data, olsr, olsmf, Partitions);

                olsj.ExecuteMapsConcurrently();
                olsj.ReduceMaps();

                return olsr;

            }

        }

        public sealed class OLSMapNode : MapNode
        {

            private CellMatrix _XtX;
            private CellVector _XtY;
            private Predicate _where;
            private FNodeSet _x;
            private FNode _y;
            private FNode _w;
            private long _ticks;

            public OLSMapNode(int ID, FNodeSet XValue, FNode YValue, FNode WValue, Predicate Filter)
                : base(ID)
            {
                this._XtX = new CellMatrix(XValue.Count, XValue.Count, CellValues.ZERO_DOUBLE);
                this._XtY = new CellVector(XValue.Count, CellValues.ZERO_DOUBLE);
                this._where = Filter;
                this._x = XValue;
                this._y = YValue;
                this._w = WValue;
            }

            public override void Execute(RecordSet Chunk)
            {

                this._ticks += RegressionModel.BuildDesignSupport(Chunk.OpenReader(this._where), this._x, this._y, this._w, this._XtX, this._XtY);
                
            }

            public CellMatrix XtX
            {
                get { return this._XtX; }
            }

            public CellVector XtY
            {
                get { return this._XtY; }
            }

            public long Ticks
            {
                get { return this._ticks; }
            }

        }

        public sealed class OLSMapFactory : MapFactory<OLSMapNode>
        {

            private Predicate _where;
            private FNodeSet _x;
            private FNode _y;
            private FNode _w;
            
            public OLSMapFactory(FNodeSet XValue, FNode YValue, FNode WValue, Predicate Filter)
                : base()
            {
                this._where = Filter;
                this._x = XValue;
                this._y = YValue;
                this._w = WValue;
            }

            public override OLSMapNode BuildNew(int PartitionID)
            {
                return new OLSMapNode(PartitionID, this._x.CloneOfMe(), this._y.CloneOfMe(), this._w.CloneOfMe(), this._where.CloneOfMe());
            }

        }

        public sealed class OLSReducer : Reducer<OLSMapNode>
        {

            private CellMatrix _XtX;
            private CellVector _XtY;
            private long _ticks;

            public OLSReducer(int XDimension)
                : base()
            {
                this._XtX = new CellMatrix(XDimension, XDimension, CellValues.ZERO_DOUBLE);
                this._XtY = new CellVector(XDimension, CellValues.ZERO_DOUBLE);
            }

            public override void Consume(OLSMapNode Node)
            {
                this._XtX += Node.XtX;
                this._XtY += Node.XtY;
                this._ticks += Node.Ticks;
            }

            public CellMatrix XtX
            {
                get { return this._XtX; }
            }

            public CellVector XtY
            {
                get { return this._XtY; }
            }

            public long Ticks
            {
                get { return this._ticks; }
            }

        }

}
