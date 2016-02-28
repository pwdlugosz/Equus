using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Gidran;
using Equus.Numerics;
using Equus.Calabrese;

namespace Equus.Thoroughbred.ManOWar
{

    /// <summary>
    /// Provides support for building feed-forward neural netowkrs
    /// </summary>
    public sealed class NeuralNetworkFactory
    {

        // Support //
        private NN_Layer _DataLayer;
        private bool _DataBias;

        private List<NN_Layer> _HiddenLayers;

        private NN_Layer _PredictionLayer;
        private ScalarFunction _PredictionActivation;
        private NodeReduction _PredictionReducer;
        
        // Master //
        private NodeLinkMaster _Links;

        // Source Data //
        private DataSet _SourceData;
        private FNodeSet _XValues;
        private FNodeSet _YValues;
        private Predicate _Where;

        // Constructors //
        public NeuralNetworkFactory(DataSet Data, Predicate Where)
        {
            this._HiddenLayers = new List<NN_Layer>();
            this._Links = new NodeLinkMaster();
            this.DefaultRule = "irprop+";
            this.DefaultActivator = ScalarFunctionFactory.Select("BinarySigmoid");
            this.DefaultReduction = new NodeReductionLinear();
            this._SourceData = Data;
            this._Where = Where;
        }

        // Defaults //
        public ScalarFunction DefaultActivator
        {
            get;
            set;
        }

        public NodeReduction DefaultReduction
        {
            get;
            set;
        }

        public string DefaultRule
        {
            get;
            set;
        }

        // Add Data Layer //
        public void AddDataLayer(bool Bias, FNodeSet Fields)
        {
            this._DataBias = Bias;
            this._XValues = Fields;
        }

        // Hidden layer //
        public void AddHiddenLayer(bool Bias, int DynamicCount, ScalarFunction Activator, NodeReduction Reducer)
        {
            this._HiddenLayers.Add(new NN_Layer(Bias, Reducer, Activator, DynamicCount, this._HiddenLayers.Count + 1));
        }

        public void AddHiddenLayer(bool Bias, int DynamicCount, ScalarFunction Activator)
        {
            this.AddHiddenLayer(Bias, DynamicCount, Activator, this.DefaultReduction);
        }

        public void AddHiddenLayer(bool Bias, int DynamicCount)
        {
            this.AddHiddenLayer(Bias, DynamicCount, this.DefaultActivator, this.DefaultReduction);
        }

        // Prediction layer //
        public void AddPredictionLayer(FNodeSet Fields, ScalarFunction Activator, NodeReduction Reducer)
        {
            this._PredictionActivation = Activator;
            this._YValues = Fields;
            this._PredictionReducer = Reducer;
        }

        public void AddPredictionLayer(FNodeSet Fields, ScalarFunction Activator)
        {
            this.AddPredictionLayer(Fields, Activator, this.DefaultReduction);
        }

        public void AddPredictionLayer(FNodeSet Fields)
        {
            this.AddPredictionLayer(Fields, this.DefaultActivator, this.DefaultReduction);
        }

        // Render the network //
        public NeuralNetwork Construct()
        {

            // Create the scrubber //
            NeuralDataFactory scrubber = new NeuralDataFactory(this._SourceData, this._XValues, this._YValues, this._Where);

            // Create the data layer //
            this._DataLayer = new NN_Layer(this._DataBias, scrubber.InputKey, scrubber.Columns);

            // Create the prediction layer //
            this._PredictionLayer = new NN_Layer(this._PredictionReducer, this._PredictionActivation, scrubber.OutputKey);

            // Link predictions to last hidden //
            if (this._HiddenLayers.Count == 0)
            {
                NN_Layer.LinkChildren(this._Links, this._PredictionLayer, this._DataLayer);
            }
            else
            {

                // Data -> First.Hidden
                NN_Layer.LinkChildren(this._Links, this._HiddenLayers.First(), this._DataLayer);

                // Last.Hidden -> Predictions //
                NN_Layer.LinkChildren(this._Links, this._PredictionLayer, this._HiddenLayers.Last());

                // Hidden -> Hidden //
                for (int i = 0; i < this._HiddenLayers.Count - 1; i++)
                    NN_Layer.LinkChildren(this._Links, this._HiddenLayers[i + 1], this._HiddenLayers[i]);

            }

            // Build a nodeset //
            NodeSet nodes = new NodeSet(this._Links);

            // Build a response set //
            ResponseNodeSet responses = new ResponseNodeSet(this._Links);

            // Neural rule //
            NeuralRule rule = RuleFactory.Construct(this.DefaultRule, responses);

            // Construct //
            return new NeuralNetwork(this._Links, nodes, responses, rule, scrubber.ScrubbedData);

        }

        /// <summary>
        /// Used to support dynamic layer creation
        /// </summary>
        private sealed class NN_Layer
        {

            private List<NeuralNode> _Nodes;
            private bool _IsRendered = false;

            public NN_Layer()
            {
                this._Nodes = new List<NeuralNode>();
            }

            public NN_Layer(bool Bias, Key Fields, Schema Columns)
                : this()
            {

                // Check if rendered //
                if (this._IsRendered)
                    throw new Exception("Layer already rendered");

                // Add the bias node //
                if (Bias)
                    this._Nodes.Add(new NeuralNodeStatic("DATA_BIAS", 1));

                // Add the references //
                for (int i = 0; i < Fields.Count; i++)
                    this._Nodes.Add(new NeuralNodeReference(Columns.ColumnName(Fields[i]), Fields[i]));

                // Tag as rendered //
                this._IsRendered = true;

            }

            public NN_Layer(bool Bias, Key Fields)
                : this()
            {

                // Check if rendered //
                if (this._IsRendered)
                    throw new Exception("Layer already rendered");

                // Add the bias node //
                if (Bias)
                    this._Nodes.Add(new NeuralNodeStatic("DATA_BIAS", 1));

                // Add the references //
                for (int i = 0; i < Fields.Count; i++)
                    this._Nodes.Add(new NeuralNodeReference("X" + i.ToString(), Fields[i]));

                // Tag as rendered //
                this._IsRendered = true;

            }

            public NN_Layer(bool Bias, NodeReduction Connector, ScalarFunction Activator, int TotalCount, int Level)
                : this()
            {

                // Check if rendered //
                if (this._IsRendered)
                    throw new Exception("Layer already rendered");

                // Check the total count //
                if (TotalCount < 1)
                    throw new Exception("Cannot have fewer than one node");

                // Add the bias node //
                if (Bias)
                    this._Nodes.Add(new NeuralNodeStatic(string.Format("H{0}_0", Level), 1));

                // Add the references //
                for (int i = 0; i < TotalCount; i++)
                    this._Nodes.Add(new NeuralNodeDynamic(string.Format("H{0}_{1}", Level, i + 1), Activator, Connector));

                // Tag as rendered //
                this._IsRendered = true;

            }

            public NN_Layer(NodeReduction Connector, ScalarFunction Activator, Key Fields, Schema Columns)
                : this()
            {

                // Check if rendered //
                if (this._IsRendered)
                    throw new Exception("Layer already rendered");

                // Add the references //
                for (int i = 0; i < Fields.Count; i++)
                    this._Nodes.Add(new NeuralNodePrediction(Columns.ColumnName(Fields[i]), Activator, Connector, Fields[i]));

                // Tag as rendered //
                this._IsRendered = true;

            }

            public NN_Layer(NodeReduction Connector, ScalarFunction Activator, Key Fields)
                : this()
            {

                // Check if rendered //
                if (this._IsRendered)
                    throw new Exception("Layer already rendered");

                // Add the references //
                for (int i = 0; i < Fields.Count; i++)
                    this._Nodes.Add(new NeuralNodePrediction("Y" + i.ToString(), Activator, Connector, Fields[i]));

                // Tag as rendered //
                this._IsRendered = true;

            }

            internal static void LinkChildren(NodeLinkMaster Master, NN_Layer TopLayer, NN_Layer BottomLayer)
            {

                foreach (NeuralNode n in TopLayer._Nodes)
                {
                    if (n.Affinity != NeuralNodeAffinity.Static)
                        n.LinkChildren(Master, BottomLayer._Nodes);
                }

            }

        }

    }

    /// <summary>
    /// Provides support for scrubbing data for neural network training
    /// </summary>
    public sealed class NeuralDataFactory
    {

        private long _max_record_count = 100000;

        public NeuralDataFactory(DataSet Data, FNodeSet X_Values, FNodeSet Y_Values, Predicate Where)
        {

            this.Render(Data, X_Values, Y_Values, Where);
            this.Columns = Data.Columns;

        }

        private void Render(DataSet Data, FNodeSet X_Values, FNodeSet Y_Values, Predicate Where)
        {

            // Build the reader fnode-set //
            FNodeSet nodes = FNodeSet.Union(X_Values, Y_Values);

            // Construct the keys //
            this.InputKey = Key.Build(X_Values.Count);
            this.OutputKey = Key.Build(X_Values.Count, Y_Values.Count);

            // Build the data //
            DataSet rs = QuarterHorse.FastReadPlan.Render(Data, Where, nodes, this._max_record_count);
            this.ScrubbedData = Matrix.ToMatrix(rs.ToRecordSet);

        }

        public Matrix ScrubbedData
        {
            get;
            private set;
        }

        public Key InputKey
        {
            get;
            private set;
        }

        public Key OutputKey
        {
            get;
            private set;
        }

        public Schema Columns
        {
            get;
            private set;
        }

    }

    /// <summary>
    /// Provides support for building neural network rules
    /// </summary>
    public static class RuleFactory
    {

        public static NeuralRule Construct(string Token, ResponseNodeSet Master)
        {

            string[] toks = Token.ToLower().Split(',',';');
            for (int i = 0; i < toks.Length; i++)
            {
                toks[i] = toks[i].Trim();
            }

            switch (toks[0])
            {

                case "bprop":
                    double bp_lr = (toks.Length < 2 ? 0.35 : double.Parse(toks[1]));
                    double bp_m = (toks.Length < 3 ? 0.035 : double.Parse(toks[2]));
                    return new BProp(Master, bp_lr, bp_m);

                case "rprop":
                    double rp0_down = (toks.Length < 2 ? 0.5 : double.Parse(toks[1]));
                    double rp0_up = (toks.Length < 3 ? 1.2 : double.Parse(toks[2]));
                    return new RProp(Master, rp0_down, rp0_up);

                case "rprop+":
                    double rp1_down = (toks.Length < 2 ? 0.5 : double.Parse(toks[1]));
                    double rp1_up = (toks.Length < 3 ? 1.2 : double.Parse(toks[2]));
                    return new RPropPlus(Master, rp1_down, rp1_up);

                case "rprop-":
                    double rp2_down = (toks.Length < 2 ? 0.5 : double.Parse(toks[1]));
                    double rp2_up = (toks.Length < 3 ? 1.2 : double.Parse(toks[2]));
                    return new RPropMinus(Master, rp2_down, rp2_up);

                case "irprop+":
                    double rp3_down = (toks.Length < 2 ? 0.5 : double.Parse(toks[1]));
                    double rp3_up = (toks.Length < 3 ? 1.2 : double.Parse(toks[2]));
                    return new iRPropPlus(Master, rp3_down, rp3_up);

                case "irprop-":
                    double rp4_down = (toks.Length < 2 ? 0.5 : double.Parse(toks[1]));
                    double rp4_up = (toks.Length < 3 ? 1.2 : double.Parse(toks[2]));
                    return new iRPropMinus(Master, rp4_down, rp4_up);

                case "mprop":
                    double mp_lr = (toks.Length < 2 ? 0.01 : double.Parse(toks[1]));
                    double mp_m = (toks.Length < 3 ? 0.001 : double.Parse(toks[2]));
                    return new MProp(Master, mp_lr, mp_m);

                case "qprop":
                    return new QProp(Master);

                case "hprop":
                    double hp_down = (toks.Length < 2 ? 0.5 : double.Parse(toks[1]));
                    double hp_up = (toks.Length < 3 ? 1.2 : double.Parse(toks[2]));
                    return new HProp(Master, hp_down, hp_up);

            }

            throw new ArgumentException(string.Format("Rule '{0}' is invalid", Token));

        }

    }


}
