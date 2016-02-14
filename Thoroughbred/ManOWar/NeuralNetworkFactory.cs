using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Gidran;
using Equus.Numerics;

namespace Equus.Thoroughbred.ManOWar
{

    public sealed class NeuralNetworkFactory
    {

        // Support //
        private NN_Layer _Data;
        private NN_Layer _Predictions;
        private List<NN_Layer> _Hidden;

        // Master //
        private NodeLinkMaster _Links;

        // Functions //
        private ScalarFunction _Activation;
        private NodeReduction _Reducer;

        // Constructors //
        public NeuralNetworkFactory(ScalarFunction Activator, NodeReduction Reduction)
        {
            this._Hidden = new List<NN_Layer>();
            this._Links = new NodeLinkMaster();
            this._Activation = Activator;
            this._Reducer = Reduction;
        }

        public NeuralNetworkFactory(ScalarFunction Activator)
            : this(Activator, new NodeReductionLinear())
        {
        }

        // Add Data Layer //
        public void AddDataLayer(bool Bias, Key Fields, Schema Columns)
        {
            this._Data = new NN_Layer(Bias, Fields, Columns);
        }

        public void AddDataLayer(bool Bias, Key Fields)
        {
            this._Data = new NN_Layer(Bias, Fields);
        }

        public void AddDataLayer(Key Fields, Schema Columns)
        {
            this.AddDataLayer(true, Fields, Columns);
        }

        public void AddDataLayer(Key Fields)
        {
            this.AddDataLayer(true, Fields);
        }

        // Hidden layer //
        public void AddHiddenLayer(bool Bias, int DynamicCount, ScalarFunction Activator)
        {
            this._Hidden.Add(new NN_Layer(Bias, this._Reducer, Activator, DynamicCount, this._Hidden.Count + 1));
        }

        public void AddHiddenLayer(bool Bias, int DynamicCount)
        {
            this.AddHiddenLayer(Bias, DynamicCount, this._Activation);
        }

        public void AddHiddenLayer(int TotalCount, ScalarFunction Activator)
        {
            this.AddHiddenLayer(true, TotalCount, Activator);
        }

        public void AddHiddenLayer(int TotalCount)
        {
            this.AddHiddenLayer(true, TotalCount, this._Activation);
        }

        // Prediction layer //
        public void AddPredictionLayer(Key Fields, Schema Columns)
        {
            this._Predictions = new NN_Layer(this._Reducer, this._Activation, Fields, Columns);
        }

        public void AddPredictionLayer(Key Fields)
        {
            this._Predictions = new NN_Layer(this._Reducer, this._Activation, Fields);
        }

        public NeuralNetwork Construct()
        {

            // Link predictions to last hidden //
            if (this._Hidden.Count == 0)
            {
                NN_Layer.LinkChildren(this._Links, this._Predictions, this._Data);
            }
            else
            {

                // Data -> First.Hidden
                NN_Layer.LinkChildren(this._Links, this._Hidden.First(), this._Data);

                // Last.Hidden -> Predictions //
                NN_Layer.LinkChildren(this._Links, this._Predictions, this._Hidden.Last());

                // Hidden -> Hidden //
                for (int i = 0; i < this._Hidden.Count - 1; i++)
                    NN_Layer.LinkChildren(this._Links, this._Hidden[i + 1], this._Hidden[i]);

            }

            // Build a nodeset //
            NodeSet nodes = new NodeSet(this._Links);

            // Build a response set //
            ResponseNodeSet responses = new ResponseNodeSet(this._Links);

            // Construct //
            return new NeuralNetwork(this._Links, nodes, responses);

        }

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


}
