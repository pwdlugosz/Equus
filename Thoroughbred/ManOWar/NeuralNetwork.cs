using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Equus.Horse;
using Equus.Gidran;
using Equus.Calabrese;
using Equus.Shire;

namespace Equus.Thoroughbred.ManOWar
{

    /// <summary>
    /// Represents a feed-forward neural network
    /// </summary>
    public sealed class NeuralNetwork : Model
    {

        internal const long MAX_IN_MEM_CELL_COUNT = 8388608; // 1024 x 1024 x 8 = maxes out at 64mb for the matrix

        // Model parameters //
        private int _MaxEpochs = 5000;
        private int _ActualEpochs = -1;
        private double _ExistSSE = 0.005;
        private int _RandomizerSeed = 128;

        // Model Structures //
        private NodeLinkMaster _Links;
        private NodeSet _Nodes;
        private ResponseNodeSet _Responses;
        private NeuralRule _Rule;
        private Matrix _Data;

        // Metadata //
        private TimeSpan _RunTime;
        private string _name = "FF_NN";

        // Constructor //
        public NeuralNetwork(string Name, NodeLinkMaster Links, NodeSet Nodes, ResponseNodeSet Responses, NeuralRule Rule, Matrix Data)
        {

            // Set values //
            this._Links = Links;
            this._Nodes = Nodes;
            this._Responses = Responses;
            this._Rule = Rule;
            this._Data = Data;
            this._name = Name;

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

        public override string Name
        {
            get
            {
                return this._name;
            }
            protected set
            {
                this._name = value;
            }
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
        public override void Render()
        {

            // Start stopwatch //
            Stopwatch sw = Stopwatch.StartNew();

            // All EPOCHS //
            for (int current_epoch = 0; current_epoch < this._MaxEpochs; current_epoch++)
            {

                // Reset the MSE //
                this._Responses.ResetSSE();

                // All data points //
                for (int matrix_row = 0; matrix_row < this._Data.RowCount; matrix_row++)
                {

                    // Render Nodes //
                    this._Nodes.Render(this._Data[matrix_row]);

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
                this._Links.WeightUpdate(this._Rule);

                //Console.WriteLine("TREE : " + this._Responses.MSE);
                //for (int i = 0; i < this.Links.Links.Count; i++)
                //{
                //    Console.WriteLine(this.Links.Links[i].ToString() + " : " + this.Links.Links[i].GRADIENT.ToString());
                //}

                // Reset the gradients //
                this._Links.Reset();

                //if (current_epoch % 100 == 0)
                //    Console.WriteLine("MSE {0}", this._Responses.MSE);

            }

            // Done running //
            sw.Stop();
            this._RunTime = sw.Elapsed;

            // Failed to converge //
            if (this._ActualEpochs == -1)
                this._ActualEpochs = this._MaxEpochs;

        }

        public void Render(NeuralRule Rule)
        {
            this._Rule = Rule;
            this.Render();
        }

        public void Render(Matrix Data)
        {
            this._Data = Data;
            this.Render();
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

        public void PrintPredictions()
        {
            this.PrintPredictions(this._Data);
        }

        public void PrintPredictions(DataSet Data, FNodeSet Fields, Predicate Filter)
        {

            Matrix m = this.DataToMatrix(Data, Fields, Filter);
            this.PrintPredictions(m);

        }

        public override void Extend(Shire.RecordWriter Output, DataSet Data, FNodeSet Inputs, FNodeSet OtherKeepValues, Predicate Where)
        {

            // Combine the keep variables and the expected nodes //
            FNodeSet nodes = FNodeSet.Union(OtherKeepValues.CloneOfMe(), this.Responses.Expected);

            // Open the reader //
            RecordReader rr = Data.OpenReader(Where);

            // Create a memory structure //
            StaticRegister mem = new StaticRegister(Data.Columns);

            // Assign both the input set and output set to the memory structure //
            nodes.AssignRegister(mem);
            Inputs.AssignRegister(mem);

            // Run through each record //
            while (rr.EndOfData == false)
            {
                    
                // Assign memory //
                mem.Assign(rr.ReadNext());

                // Get the array of doubles for the network //
                double[] d = Record.ToDouble(Inputs.Evaluate());

                // Render each node //
                this._Nodes.Render(d);

                // Output //
                Record t = nodes.Evaluate();
                Output.Insert(t);

            }

        }

        public override RecordSet Extend(DataSet Data, FNodeSet Inputs, FNodeSet OtherKeepValues, Predicate Where)
        {

            // Combine the keep variables and the expected nodes //
            FNodeSet nodes = FNodeSet.Union(OtherKeepValues.CloneOfMe(), this.Responses.Expected);

            // Open the reader //
            RecordReader rr = Data.OpenReader(Where);

            // Create the output table and stream //
            RecordSet rs = new RecordSet(nodes.Columns);
            RecordWriter Output = rs.OpenWriter();

            // Create a memory structure //
            StaticRegister mem = new StaticRegister(Data.Columns);

            // Assign both the input set and output set to the memory structure //
            nodes.AssignRegister(mem);
            Inputs.AssignRegister(mem);

            // Run through each record //
            while (rr.EndOfData == false)
            {

                // Assign memory //
                mem.Assign(rr.ReadNext());

                // Get the array of doubles for the network //
                double[] d = Record.ToDouble(Inputs.Evaluate());

                // Render each node //
                this._Nodes.Render(d);

                // Output //
                Record t = nodes.Evaluate();
                Output.Insert(t);

            }

            Output.Close();

            return rs;

        }

        public override Table Extend(string Dir, string Name, DataSet Data, FNodeSet Inputs, FNodeSet OtherKeepValues, Predicate Where)
        {

            // Combine the keep variables and the expected nodes //
            FNodeSet nodes = FNodeSet.Union(OtherKeepValues.CloneOfMe(), this.Responses.Expected);

            // Open the reader //
            RecordReader rr = Data.OpenReader(Where);

            // Create the output table and stream //
            Table q = new Table(Dir, Name, nodes.Columns);
            RecordWriter Output = q.OpenWriter();

            // Create a memory structure //
            StaticRegister mem = new StaticRegister(Data.Columns);

            // Assign both the input set and output set to the memory structure //
            nodes.AssignRegister(mem);
            Inputs.AssignRegister(mem);

            // Run through each record //
            while (rr.EndOfData == false)
            {

                // Assign memory //
                mem.Assign(rr.ReadNext());

                // Get the array of doubles for the network //
                double[] d = Record.ToDouble(Inputs.Evaluate());

                // Render each node //
                this._Nodes.Render(d);

                // Output //
                Record t = nodes.Evaluate();
                Output.Insert(t);

            }

            Output.Close();

            return q;
        
        }

        // Strings //
        public string ShortStatistics()
        {
            return string.Format("NN : Predictions {0} : Nodes {1} : Epochs {2} : MSE {3} : Runtime : {4}",
                this._Responses.Responses.Count, this._Nodes.Nodes.Count, this._ActualEpochs, Math.Round(this._Responses.MSE, 4), this._RunTime);
        }

        public override string Statistics()
        {

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("-- Neural Network --");
            sb.AppendLine(string.Format("Prediction Nodes: {0}", this._Responses.Count));
            sb.AppendLine(string.Format("Maximum Epochs: {0}", this._MaxEpochs));
            sb.AppendLine(string.Format("Actual Epochs: {0}", this._ActualEpochs));
            sb.AppendLine(string.Format("Random Seed: {0}", this._RandomizerSeed));
            sb.AppendLine(string.Format("Exit SSE: {0}", Math.Round(this._ExistSSE, 5)));
            sb.AppendLine(string.Format("Actual SSE: {0}", Math.Round(this._Responses.MSE, 5)));
            sb.AppendLine(string.Format("Runtime: {0}", this._RunTime));
            sb.AppendLine(string.Format("Reference Nodes: {0}", this._Nodes.ReferenceNodeCount));
            sb.AppendLine(string.Format("Static Nodes: {0}", this._Nodes.StaticNodeCount));
            sb.AppendLine(string.Format("Dynamic Nodes: {0}", this._Nodes.DynamicNodeCount));
            sb.AppendLine(string.Format("Prediction Nodes: {0}", this._Nodes.PredictionNodeCount));
            sb.AppendLine(string.Format("Training Rule: {0}", this._Rule.ToString()));

            return sb.ToString();

        }

    }




}
