using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Equus.Horse;
using Equus.Gidran;
using Equus.Calabrese;

namespace Equus.Thoroughbred.ManOWar
{

    public sealed class NeuralNetwork
    {

        internal const long MAX_IN_MEM_CELL_COUNT = 8388608; // 1024 x 1024 x 8 = maxes out at 64mb for the matrix

        // Model parameters //
        private int _MaxEpochs = 5000;
        private int _ActualEpochs = -1;
        private double _ExistSSE = 0.005;
        private int _RandomizerSeed = 127;

        // Model Structures //
        private NodeLinkMaster _Links;
        private NodeSet _Nodes;
        private ResponseNodeSet _Responses;

        // Metadata //
        private TimeSpan _RunTime;

        // Constructor //
        public NeuralNetwork(NodeLinkMaster Links, NodeSet Nodes, ResponseNodeSet Responses)
        {

            // Set values //
            this._Links = Links;
            this._Nodes = Nodes;
            this._Responses = Responses;

            // Initialize //
            this.Initialize();

        }

        // Properties //
        public int Epochs
        {
            get { return this._MaxEpochs; }
            set { this._MaxEpochs = value; }
        }

        public int Seed
        {
            get { return this._RandomizerSeed; }
            set 
            { 
                this._RandomizerSeed = value;
                this._Links.Randomize(this._RandomizerSeed);
            }
        }

        public double ExitSSE
        {
            get { return this._ExistSSE; }
            set { this._ExistSSE = value; }
        }

        public bool IsRendered
        {
            get { return this._ActualEpochs != -1; }
        }

        public TimeSpan RunTime
        {
            get { return this._RunTime; }
        }

        public NodeLinkMaster Links
        {
            get { return this._Links; }
        }

        public NodeSet Nodes
        {
            get { return this._Nodes; }
        }

        public ResponseNodeSet Responses
        {
            get { return this._Responses; }
        }

        // Set up methods //
        internal void Initialize()
        {

            // Check for circular refs //
            if (this._Responses.IsCircular)
                throw new InvalidOperationException("Neural network contains at least one circular reference");

            // Reset all the weights //
            this._Links.Reset();

            // Randomeize //
            this._Links.Randomize(this._RandomizerSeed);

        }

        internal Matrix DataToMatrix(DataSet Data, FNodeSet Fields, Predicate Where)
        {

            // Estimate the size //
            long CountOf = 0;
            if (Data.IsBig)
                CountOf = Data.ToBigRecordSet.Count;
            else
                CountOf = Data.ToRecordSet.Count;

            // Calculate the max size //
            long MaxSize = MAX_IN_MEM_CELL_COUNT / ((long)Fields.Count);

            // Build a record set //
            RecordSet rs = Dressage.QueryFactory.SELECT(Data, Fields, Where);

            // Convert to a matrix //
            Matrix m = Matrix.ToMatrix(rs);

            // Return the matrix //
            return m;

        }

        // Train Methods //
        public void Render(Matrix Data, NeuralRule Rule)
        {

            // Start stopwatch //
            Stopwatch sw = Stopwatch.StartNew();

            // All EPOCHS //
            for (int current_epoch = 0; current_epoch < this._MaxEpochs; current_epoch++)
            {

                // All data points //
                for (int matrix_row = 0; matrix_row < Data.RowCount; matrix_row++)
                {

                    // Render Nodes //
                    this._Nodes.Render(Data[matrix_row]);

                    // Render Gradients //
                    this._Responses.RenderGradients();

                }

                // After this epoch, check the exit condition //
                if (this._Responses.MSE < this._ExistSSE)
                {
                    this._ActualEpochs = current_epoch;
                    break;
                }

                // Update the weights //
                this._Links.WeightUpdate(Rule);

                //Console.WriteLine("TREE : " + this._Responses.MSE);
                //for (int i = 0; i < this.Links.Links.Count; i++)
                //{
                //    Console.WriteLine(this.Links.Links[i].ToString() + " : " + this.Links.Links[i].GRADIENT.ToString());
                //}

                // Reset the gradients //
                this._Links.Reset();
                
                // Reset the MSE //
                this._Responses.ResetSSE();

            }

            // Done running //
            sw.Stop();
            this._RunTime = sw.Elapsed;

            // Failed to converge //
            if (this._ActualEpochs == -1)
                this._ActualEpochs = this._MaxEpochs;

        }

        public void Render(DataSet Data, FNodeSet Fields, Predicate Filter, NeuralRule Rule)
        {

            Matrix m = this.DataToMatrix(Data, Fields, Filter);
            this.Render(m, Rule);

        }

        // Prints //
        public void PrintPredictions(Matrix Data)
        {

            // Check if rendered //
            if (!this.IsRendered)
            {
                Comm.WriteLine("Network not rendered");
                return;
            }

            // Print if rendered //
            for (int i = 0; i < Data.RowCount; i++)
            {

                // Render each node //
                this._Nodes.Render(Data[i]);

                // Write the string //
                Comm.WriteLine(this._Responses.AEString);

            }


        }

        public void PrintPredictions(DataSet Data, FNodeSet Fields, Predicate Filter, NeuralRule Rule)
        {

            Matrix m = this.DataToMatrix(Data, Fields, Filter);
            this.PrintPredictions(m);

        }

        // Strings //
        public string Statistics()
        {
            return string.Format("NN : Predictions {0} : Nodes {1} : Epochs {2} : MSE {3} : Runtime : {4}",
                this._Responses.Responses.Count, this._Nodes.Nodes.Count, this._ActualEpochs, Math.Round(this._Responses.MSE, 4), this._RunTime);
        }

    }


}
