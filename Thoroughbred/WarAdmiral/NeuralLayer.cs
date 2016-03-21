using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Numerics;
using Equus.Horse;
using Equus.Gidran;
using Equus.Numerics;
using Equus.Calabrese;

namespace Equus.Thoroughbred.WarAdmiral
{

    // Layers //
    public enum NeuralLayerAffinity
    {
        Data,
        Dynamic,
        Output
    }

    public abstract class NeuralLayer
    {

        public double[] MEAN;
        public double[] GRADIENT;
        public double[] PARTIAL_GRADIENT;
        public double[] ERROR;
        public int SIZE;
        public bool BIAS = true;
        public double SSE;

        public NeuralLink BACKWARD_LINK; // the linking below this
        public NeuralLink FORWARD_LINK; // the linking above this

        public NeuralLayer(int Size, bool Bias)
        {
            this.MEAN = new double[Size];
            this.GRADIENT = new double[Size];
            this.PARTIAL_GRADIENT = new double[Size];
            this.ERROR = new double[Size];
            this.SIZE = Size;
            this.BIAS = Bias;
        }

        public abstract void Calculate(double[] Vector);

        public virtual void Reset()
        {
            
        }

        /// <summary>
        /// True if this is the final layer 
        /// </summary>
        public bool IsTerminal
        {
            get { return this.FORWARD_LINK == null; }
        }

        /// <summary>
        /// True if this is the first layer
        /// </summary>
        public bool IsOrigin
        {
            get { return this.BACKWARD_LINK == null; }
        }

        private string AsString(double[] Values, string Delim)
        {
            
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < Values.Length; i++)
            {
                sb.Append(Math.Round(Values[i], 3));
                if (i != Values.Length - 1)
                    sb.Append(Delim);
            }

            return sb.ToString();

        }

        public string MeanString()
        {
            return this.AsString(this.MEAN, ",");
        }

        public string GradientString()
        {
            return this.AsString(this.GRADIENT, ",");
        }

        public string PartialGradientString()
        {
            return this.AsString(this.PARTIAL_GRADIENT, ",");
        }

        public string ErrorString()
        {
            return this.AsString(this.ERROR, ",");
        }

        public NeuralLayerAffinity Affinity
        {
            get;
            protected set;
        }

    }

    public sealed class DataLayer : NeuralLayer
    {

        private int[] OFFSETS;

        public DataLayer(int[] Offsets, bool Bias)
            :base(Offsets.Length + (Bias ? 1 : 0), Bias)
        {
            this.OFFSETS = Offsets;
            this.Affinity = NeuralLayerAffinity.Data;
        }

        public override void Calculate(double[] Vector)
        {

            if (this.BIAS)
            {

                this.MEAN[0] = 1D;
                for (int i = 0; i < this.SIZE - 1; i++)
                {
                    this.MEAN[i + 1] = Vector[this.OFFSETS[i]];
                    this.PARTIAL_GRADIENT[i + 1] = 0;
                }

            }
            else
            {

                for (int i = 0; i < this.SIZE; i++)
                {
                    this.MEAN[i] = Vector[this.OFFSETS[i]];
                    this.PARTIAL_GRADIENT[i] = 0;
                }

            }

            if (this.FORWARD_LINK != null)
                this.FORWARD_LINK.FORWARD_LAYER.Calculate(Vector);

        }

    }

    public sealed class DynamicLinearLayer : NeuralLayer
    {

        public ScalarFunction ACT;

        public DynamicLinearLayer(int Size, bool Bias, ScalarFunction Activation)
            : base(Size + (Bias ? 1 : 0), Bias)
        {
            this.ACT = Activation;
            this.Affinity = NeuralLayerAffinity.Dynamic;
        }

        public override void Calculate(double[] Vector)
        {

            if (BIAS)
            {

                this.MEAN[0] = 1D;
                this.PARTIAL_GRADIENT[0] = 0D;
                for (int i = 0; i < this.SIZE - 1; i++)
                {

                    double nu = 0D;

                    for (int j = 0; j < this.BACKWARD_LINK.BACKWARD_LAYER.SIZE; j++)
                    {
                        nu += this.BACKWARD_LINK.BACKWARD_LAYER.MEAN[j] * this.BACKWARD_LINK.WEIGHT[j, i];
                    }

                    this.MEAN[i + 1] = this.ACT.Evaluate(nu);
                    this.PARTIAL_GRADIENT[i + 1] = this.ACT.Gradient(nu);

                }

            }
            else
            {

                for (int i = 0; i < this.SIZE; i++)
                {

                    double nu = 0D;

                    for (int j = 0; j < this.BACKWARD_LINK.BACKWARD_LAYER.SIZE; j++)
                    {
                        nu += this.BACKWARD_LINK.BACKWARD_LAYER.MEAN[j] * this.BACKWARD_LINK.WEIGHT[j, i];
                    }

                    this.MEAN[i] = this.ACT.Evaluate(nu);
                    this.PARTIAL_GRADIENT[i] = this.ACT.Gradient(nu);

                }

            }

            if (this.FORWARD_LINK != null)
                this.FORWARD_LINK.FORWARD_LAYER.Calculate(Vector);

        }

    }

    public sealed class OutputLinearLayer : NeuralLayer
    {

        public ScalarFunction ACT;
        private int[] OFFSETS;
        
        public OutputLinearLayer(int[] Offsets, ScalarFunction Activation)
            : base(Offsets.Length, false)
        {
            this.OFFSETS = Offsets;
            this.ACT = Activation;
            this.Affinity = NeuralLayerAffinity.Output;
        }

        public override void Calculate(double[] Vector)
        {

            int back_data = this.BACKWARD_LINK.BACKWARD_LAYER.SIZE; // number of data points in prior layer
            int dims = this.SIZE; // size of this layer
            this.SSE = 0D;

            for (int i = 0; i < dims; i++)
            {

                double nu = 0D;

                for (int j = 0; j < back_data; j++)
                {
                    nu += this.BACKWARD_LINK.BACKWARD_LAYER.MEAN[j] * this.BACKWARD_LINK.WEIGHT[j, i];
                }

                this.MEAN[i] = this.ACT.Evaluate(nu);
                this.PARTIAL_GRADIENT[i] = this.ACT.Gradient(nu);
                this.ERROR[i] = (Vector[this.OFFSETS[i]] - this.MEAN[i]);
                this.SSE += this.ERROR[i] * this.ERROR[i];

            }

        }

    }

    // Links //
    public abstract class NeuralLink
    {

        public double[,] GRADIENT;
        public double[,] LAG_GRADIENT;
        public double[,] WORK_GRADIENT;

        public double[,] WEIGHT;
        public double[,] LAG_WEIGHT;

        public double[,] DELTA;
        public double[,] LAG_DELTA;

        public int BACKWARD_SIZE = 0;
        public int FORWARD_SIZE = 0;

        public NeuralLayer FORWARD_LAYER;
        public NeuralLayer BACKWARD_LAYER;

        public int ROW_COUNT = 0; // number of nodes in backward layer
        public int COL_COUNT = 0; // number of non-bias nodes in forward layer

        public double SSE = 0;

        public NeuralLink(NeuralLayer ForwardLayer, NeuralLayer BackwardLayer)
        {

            // Set this size //
            this.BACKWARD_SIZE = BackwardLayer.SIZE;
            this.FORWARD_SIZE = ForwardLayer.SIZE;

            // Set the counts //
            this.ROW_COUNT = BackwardLayer.SIZE;
            this.COL_COUNT = (ForwardLayer.BIAS ? ForwardLayer.SIZE - 1 : ForwardLayer.SIZE);

            // Bind nodes to this layer //
            this.BACKWARD_LAYER = BackwardLayer;
            this.FORWARD_LAYER = ForwardLayer;
            
            // Bind the layers to this node //
            this.FORWARD_LAYER.BACKWARD_LINK = this;
            this.BACKWARD_LAYER.FORWARD_LINK = this;

            this.GRADIENT = new double[this.ROW_COUNT, this.COL_COUNT];
            this.LAG_GRADIENT = new double[this.ROW_COUNT, this.COL_COUNT];
            this.WORK_GRADIENT = new double[this.ROW_COUNT, this.COL_COUNT];
            
            this.WEIGHT = new double[this.ROW_COUNT, this.COL_COUNT];
            this.LAG_WEIGHT = new double[this.ROW_COUNT, this.COL_COUNT];

            this.DELTA = new double[this.ROW_COUNT, this.COL_COUNT];
            this.LAG_DELTA = new double[this.ROW_COUNT, this.COL_COUNT];

        }

        public abstract void RenderGradient();

        public void Reset()
        {
            this.GRADIENT = new double[this.ROW_COUNT, this.COL_COUNT];
        }

        public string WeightString()
        {

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < this.ROW_COUNT; i++)
            {
                for (int j = 0; j < this.COL_COUNT; j++)
                {

                    sb.Append(this.WEIGHT[i, j]);
                    sb.Append(",");

                }
                sb.AppendLine();
            }
            return sb.ToString();

        }

        public string GradientString()
        {

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < this.ROW_COUNT; i++)
            {
                for (int j = 0; j < this.COL_COUNT; j++)
                {

                    sb.Append(this.GRADIENT[i, j]);
                    sb.Append(",");

                }
                sb.AppendLine();
            }
            return sb.ToString();

        }

    }

    public sealed class NeuralLinkLinear : NeuralLink
    {

        public NeuralLinkLinear(NeuralLayer ForwardLayer, NeuralLayer BackwardLayer)
            : base(ForwardLayer, BackwardLayer)
        {
        }

        public override void RenderGradient()
        {

            double dx = 0;
            int fwd_bias = (this.FORWARD_LAYER.BIAS ? 1 : 0);

            // If this is linking upstream to the final layer //
            if (this.FORWARD_LAYER.IsTerminal)
            {

                for (int bwd = 0; bwd < this.ROW_COUNT; bwd++)
                {

                    for (int fwd = 0; fwd < this.COL_COUNT; fwd++)
                    {

                        dx = this.FORWARD_LAYER.PARTIAL_GRADIENT[fwd] * this.FORWARD_LAYER.ERROR[fwd];
                        this.GRADIENT[bwd, fwd] += dx * this.BACKWARD_LAYER.MEAN[bwd];
                        this.WORK_GRADIENT[bwd, fwd] = dx * this.WEIGHT[bwd, fwd];
                        
                    }

                }

            }
            // Otherwise, this is a downstream link
            else
            {
                
                for (int bwd = 0; bwd < this.ROW_COUNT; bwd++)
                {

                    for (int fwd = 0; fwd < this.COL_COUNT; fwd++)
                    {

                        dx = 0;
                        for (int deep_fwd = 0; deep_fwd < this.FORWARD_LAYER.FORWARD_LINK.FORWARD_SIZE; deep_fwd++)
                        {
                            dx += this.FORWARD_LAYER.PARTIAL_GRADIENT[fwd + fwd_bias] * this.FORWARD_LAYER.FORWARD_LINK.WORK_GRADIENT[fwd + fwd_bias, deep_fwd];
                        }
                        this.GRADIENT[bwd, fwd] += dx * this.BACKWARD_LAYER.MEAN[bwd];
                        this.WORK_GRADIENT[bwd, fwd] = dx * this.WEIGHT[bwd, fwd];
                        
                    }

                }

            }
            
            // If the backward layer is not the origin, send a signal back to calculate the gradients in that layer //
            if (!this.BACKWARD_LAYER.IsOrigin)
                this.BACKWARD_LAYER.BACKWARD_LINK.RenderGradient();

        }

    }

    // Updaters //
    public abstract class LinkUpdate
    {

        public LinkUpdate()
        {
        }

        public abstract void Update(NeuralLink Link);

    }

    public sealed class LinkUpdateRandom : LinkUpdate
    {

        private Random _generator;

        public LinkUpdateRandom(Random Generator)
            : base()
        {
            this._generator = Generator;
        }

        public LinkUpdateRandom(int Seed)
            : this(new Random(Seed))
        {
        }

        public override void Update(NeuralLink Link)
        {
            for (int i = 0; i < Link.ROW_COUNT; i++)
            {
                for (int j = 0; j < Link.COL_COUNT; j++)
                {
                    Link.WEIGHT[i, j] = this._generator.NextDouble() * 2D - 1D;
                    Link.DELTA[i, j] = 0.1;
                    Link.LAG_DELTA[i, j] = 0.1;
                }
            }
        }

    }

    public sealed class LinkUpdateGradientDecent : LinkUpdate
    {

        private double _lr, _m;

        public LinkUpdateGradientDecent(double LearningRate, double Momentum)
            : base()
        {
            this._lr = LearningRate;
            this._m = Momentum;
        }

        public override void Update(NeuralLink Link)
        {

            for (int i = 0; i < Link.ROW_COUNT; i++)
            {
                for (int j = 0; j < Link.COL_COUNT; j++)
                {
                    double w = Link.WEIGHT[i, j];
                    Link.WEIGHT[i, j] += Link.GRADIENT[i, j] * this._lr + (Link.WEIGHT[i, j] - Link.LAG_WEIGHT[i, j]) * this._m;
                    Link.LAG_WEIGHT[i, j] = w;
                    Link.LAG_GRADIENT[i, j] = Link.GRADIENT[i, j];
                }
            }

        }

    }

    public sealed class LinkUpdateManhattan : LinkUpdate
    {

        private double _lr, _m;

        public LinkUpdateManhattan(double LearningRate, double Momentum)
            : base()
        {
            this._lr = LearningRate;
            this._m = Momentum;
        }

        public override void Update(NeuralLink Link)
        {

            for (int i = 0; i < Link.ROW_COUNT; i++)
            {
                for (int j = 0; j < Link.COL_COUNT; j++)
                {
                    double dx2 = (Link.GRADIENT[i, j] - Link.LAG_GRADIENT[i, j]) / (Link.WEIGHT[i, j] - Link.LAG_WEIGHT[i, j]);
                    double w = Link.WEIGHT[i, j];
                    Link.WEIGHT[i, j] += Math.Sign(Link.GRADIENT[i, j]) * this._lr + Math.Sign(dx2) * this._m;
                    Link.LAG_WEIGHT[i, j] = w;
                    Link.LAG_GRADIENT[i, j] = Link.GRADIENT[i, j];
                }
            }

        }

    }

    public class LinkUpdateResiliant : LinkUpdate
    {

        protected double _up = 1.2, _down = 0.5;
        protected const double CEILING = 2D, FLOOR = 0.0001;

        public LinkUpdateResiliant()
            : base()
        {
        }

        public override void Update(NeuralLink Link)
        {

            for (int i = 0; i < Link.ROW_COUNT; i++)
            {
                for (int j = 0; j < Link.COL_COUNT; j++)
                {

                    // Variables //
                    double s = Math.Sign(Link.GRADIENT[i, j] * Link.LAG_GRADIENT[i, j]);
                    double d = Link.DELTA[i,j];
                    double w = Link.WEIGHT[i, j];
                    //Console.WriteLine("i x j x d x w : {0} x {1} x {2} x {3}", i, j, d, w);

                    if (s > 0)
                    {
                        d = Math.Min(CEILING, this._up * d);
                        Link.WEIGHT[i, j] += d * Math.Sign(Link.GRADIENT[i, j]);
                    }
                    else if (s < 0)
                    {
                        d = Math.Max(FLOOR, this._down * d);
                        Link.GRADIENT[i, j] = 0D;
                    }
                    else
                    {
                        Link.WEIGHT[i, j] += d * Math.Sign(Link.GRADIENT[i, j]);
                    }

                    Link.LAG_DELTA[i, j] = Link.DELTA[i, j];
                    Link.LAG_WEIGHT[i, j] = w;
                    Link.LAG_GRADIENT[i, j] = Link.GRADIENT[i, j];

                    Link.DELTA[i, j] = d;

                }
            }

        }

    }

    public sealed class LinkUpdateResiliantPlus : LinkUpdateResiliant
    {

        public override void Update(NeuralLink Link)
        {

            for (int i = 0; i < Link.ROW_COUNT; i++)
            {
                for (int j = 0; j < Link.COL_COUNT; j++)
                {

                    // Variables //
                    double s = Math.Sign(Link.GRADIENT[i, j] * Link.LAG_GRADIENT[i, j]);
                    double d = Link.DELTA[i, j];
                    double w = Link.WEIGHT[i, j];

                    if (s > 0)
                    {
                        d = Math.Min(CEILING, this._up * d);
                        Link.WEIGHT[i, j] += d * Math.Sign(Link.GRADIENT[i, j]);
                    }
                    else if (s < 0)
                    {
                        d = Math.Max(FLOOR, this._down * d);
                        Link.GRADIENT[i, j] = 0D;
                        Link.WEIGHT[i, j] = Link.LAG_WEIGHT[i, j];
                    }
                    else
                    {
                        Link.WEIGHT[i, j] += d * Math.Sign(Link.GRADIENT[i, j]);
                    }

                    Link.LAG_DELTA[i, j] = Link.DELTA[i, j];
                    Link.LAG_WEIGHT[i, j] = w;
                    Link.LAG_GRADIENT[i, j] = Link.GRADIENT[i, j];

                    Link.DELTA[i, j] = d;
                    
                }
            }

        }

    }

    public sealed class LinkUpdateResiliantMinus : LinkUpdateResiliant
    {

        public override void Update(NeuralLink Link)
        {

            for (int i = 0; i < Link.ROW_COUNT; i++)
            {
                for (int j = 0; j < Link.COL_COUNT; j++)
                {

                    // Variables //
                    double s = Math.Sign(Link.GRADIENT[i, j] * Link.LAG_GRADIENT[i, j]);
                    double d = Link.DELTA[i, j];
                    double w = Link.WEIGHT[i, j];

                    if (s > 0)
                    {
                        d = Math.Min(CEILING, this._up * d);
                    }
                    else if (s < 0)
                    {
                        d = Math.Max(FLOOR, this._down * d);
                    }
                    Link.WEIGHT[i, j] += d * Math.Sign(Link.GRADIENT[i, j]);

                    Link.LAG_DELTA[i, j] = Link.DELTA[i, j];
                    Link.LAG_WEIGHT[i, j] = w;
                    Link.LAG_GRADIENT[i, j] = Link.GRADIENT[i, j];

                    Link.DELTA[i, j] = d;

                }
            }

        }

    }

    public sealed class LinkUpdateImprovedResiliantPlus : LinkUpdateResiliant
    {

        private double _sse = 0;

        public override void Update(NeuralLink Link)
        {

            for (int i = 0; i < Link.ROW_COUNT; i++)
            {
                for (int j = 0; j < Link.COL_COUNT; j++)
                {

                    // Variables //
                    double s = Math.Sign(Link.GRADIENT[i, j] * Link.LAG_GRADIENT[i, j]);
                    double d = Link.DELTA[i, j];
                    double w = Link.WEIGHT[i, j];

                    if (s > 0)
                    {
                        d = Math.Min(CEILING, this._up * d);
                        Link.WEIGHT[i, j] += d * Math.Sign(Link.GRADIENT[i, j]);
                    }
                    else if (s < 0)
                    {
                        d = Math.Max(FLOOR, this._down * d);
                        Link.GRADIENT[i, j] = 0D;
                        if (this._sse <= Link.SSE)
                            Link.WEIGHT[i, j] = Link.LAG_WEIGHT[i, j];
                    }
                    else
                    {
                        Link.WEIGHT[i, j] += d * Math.Sign(Link.GRADIENT[i, j]);
                    }

                    Link.LAG_DELTA[i, j] = Link.DELTA[i, j];
                    Link.LAG_WEIGHT[i, j] = w;
                    Link.LAG_GRADIENT[i, j] = Link.GRADIENT[i, j];

                    Link.DELTA[i, j] = d;
                    this._sse = Link.SSE;

                }
            }

        }

    }

    public sealed class LinkUpdateImprovedResiliantMinus : LinkUpdateResiliant
    {

        public override void Update(NeuralLink Link)
        {

            for (int i = 0; i < Link.ROW_COUNT; i++)
            {
                for (int j = 0; j < Link.COL_COUNT; j++)
                {

                    // Variables //
                    double s = Math.Sign(Link.GRADIENT[i, j] * Link.LAG_GRADIENT[i, j]);
                    double d = Link.DELTA[i, j];
                    double w = Link.WEIGHT[i, j];

                    if (s > 0)
                    {
                        d = Math.Min(CEILING, this._up * d);
                        Link.WEIGHT[i, j] += d * Math.Sign(Link.GRADIENT[i, j]);
                    }
                    else if (s < 0)
                    {
                        d = Math.Max(FLOOR, this._down * d);
                        Link.WEIGHT[i, j] += d * Math.Sign(Link.GRADIENT[i, j]);
                        Link.GRADIENT[i, j] = 0;
                    }
                    else
                    {
                        Link.WEIGHT[i, j] += d * Math.Sign(Link.GRADIENT[i, j]);
                    }
                    
                    Link.LAG_DELTA[i, j] = Link.DELTA[i, j];
                    Link.LAG_WEIGHT[i, j] = w;
                    Link.LAG_GRADIENT[i, j] = Link.GRADIENT[i, j];

                    Link.DELTA[i, j] = d;

                }
            }

        }

    }

    public sealed class LinkUpdateAccelerate : LinkUpdateResiliant
    {

        public override void Update(NeuralLink Link)
        {

            for (int i = 0; i < Link.ROW_COUNT; i++)
            {
                for (int j = 0; j < Link.COL_COUNT; j++)
                {

                    // Variables //
                    double s = Math.Sign(Link.GRADIENT[i, j] * Link.LAG_GRADIENT[i, j]);
                    double dxs = Math.Sign(Link.GRADIENT[i, j]);
                    double dx2s = Math.Sign((Link.GRADIENT[i, j] - Link.LAG_GRADIENT[i, j]) * (Link.WEIGHT[i, j] - Link.LAG_WEIGHT[i, j]));
                    double d = Link.DELTA[i, j];
                    double w = Link.WEIGHT[i, j];

                    if (s > 0)
                    {
                        d = Math.Min(CEILING, this._up * d);
                    }
                    else if (s < 0)
                    {
                        d = Math.Max(FLOOR, this._down * d);
                    }

                    Link.WEIGHT[i, j] += d * Math.Sign(Link.GRADIENT[i, j]) * 0.5 + 0.50 * Link.GRADIENT[i, j] * 0.30;

                    Link.LAG_DELTA[i, j] = Link.DELTA[i, j];
                    Link.LAG_WEIGHT[i, j] = w;
                    Link.LAG_GRADIENT[i, j] = Link.GRADIENT[i, j];

                    Link.DELTA[i, j] = d;

                }
            }

        }

    }

    public sealed class LinkUpdateTest : LinkUpdate
    {

        public LinkUpdateTest()
            : base()
        {
        }

        public override void Update(NeuralLink Link)
        {

            if (Link.BACKWARD_LAYER.IsOrigin)
            {
                Link.WEIGHT[0, 0] = 0.25;
                Link.WEIGHT[1, 0] = -0.25;
                Link.WEIGHT[2, 0] = 0.25;

                Link.WEIGHT[0, 1] = -0.25;
                Link.WEIGHT[1, 1] = 0.25;
                Link.WEIGHT[2, 1] = -0.25;
            }
            else
            {
                Link.WEIGHT[0, 0] = 0.25;
                Link.WEIGHT[1, 0] = -0.25;
                Link.WEIGHT[2, 0] = 0.25;
            }

        }

    }

    public static class LinkUpdateFactory
    {

        private static char _del = ';';

        public static LinkUpdate Generate(string Text)
        {

            string[] tokens = Text.Split(_del);
            string method = tokens[0];
            double p1 = (tokens.Length >= 2 ? double.Parse(tokens[1]) : -1D);
            double p2 = (tokens.Length >= 3 ? double.Parse(tokens[2]) : -1D);

            switch (method.ToLower())
            {

                case "bprop":
                case "gradient":
                case "decent":
                case "b":
                    return new LinkUpdateGradientDecent(p1 < 0 ? 0.3D : p1, p2 < 0 ? 0.05D : p2);

                case "mprop":
                case "manhattan":
                case "m":
                    return new LinkUpdateManhattan(p1 < 0 ? 0.01D : p1, p2 < 0 ? 0.0025D : p2);

                case "rprop":
                case "resiliant":
                case "r":
                    return new LinkUpdateResiliant();

                case "rprop+":
                case "rpropplus":
                case "r+":
                case "rplus":
                    return new LinkUpdateResiliantPlus();

                case "rprop-":
                case "rpropminus":
                case "r-":
                case "rminus":
                    return new LinkUpdateResiliantMinus();

                case "irprop+":
                case "irpropplus":
                case "ir+":
                case "irplus":
                    return new LinkUpdateImprovedResiliantPlus();

                case "irprop-":
                case "irpropminus":
                case "ir-":
                case "irminus":
                    return new LinkUpdateImprovedResiliantMinus();

                case "test":
                    return new LinkUpdateAccelerate();

            }

            throw new ArgumentException(string.Format("Update rule '{0}' is invalid", method));

        }

    }

    // Structures //
    public sealed class Network
    {

        private double[][] _data;

        private NeuralLayer _origin;
        private NeuralLayer _terminis;
        private List<NeuralLayer> _layer_tree;

        private List<NeuralLink> _link_tree;

        private LinkUpdate _update_rule;
        private LinkUpdate _initialization_rule;
        private int _max_epochs = 5000;
        private int _actual_epochs = 0;
        private double _current_sse = 0;
        private double _exit_sse = 0.0025;

        private int _phase = 0; // 0 = not constructed, 1 = constructed, not rendered, 2 = rendered

        public Network(LinkUpdate Rule, double[][] Data)
        {
            this._phase = 0;
            this._layer_tree = new List<NeuralLayer>();
            this._link_tree = new List<NeuralLink>();
            this._update_rule = Rule;
            this._data = Data;
            this._initialization_rule = new LinkUpdateRandom(129);
            //this._initialization_rule = new LinkUpdateTest();
        }

        // Construction Methods //
        public void AddDataLayer(int[] Offsets, bool Bias)
        {

            if (this._phase != 0)
                throw new InvalidOperationException("Cannot alter the state of the network after it has been constructed");
            this._origin = new DataLayer(Offsets, Bias);

        }

        public void AddHiddenLayer(int Size, bool Bias, ScalarFunction Activator)
        {

            if (this._phase != 0)
                throw new InvalidOperationException("Cannot alter the state of the network after it has been constructed");
            DynamicLinearLayer layer = new DynamicLinearLayer(Size, Bias, Activator);
            this._layer_tree.Add(layer);

        }

        public void AddOutputLayer(int[] Offsets, ScalarFunction Activator)
        {
            if (this._phase != 0)
                throw new InvalidOperationException("Cannot alter the state of the network after it has been constructed");
            this._terminis = new OutputLinearLayer(Offsets, Activator);
        }

        public void Construct()
        {

            if (this._phase != 0)
                throw new InvalidOperationException("Cannot alter the state of the network after it has been constructed");
            if (this._origin == null)
                throw new InvalidOperationException("Cannot construct a network; the origin layer must be non-null");
            if (this._terminis == null)
                throw new InvalidOperationException("Cannot construct a network; the terminal layer must be non-null");

            this._phase = 1;

            this._layer_tree.Insert(0, this._origin);
            this._layer_tree.Add(this._terminis);

            for (int i = 0; i < this._layer_tree.Count - 1; i++ )
            {

                NeuralLinkLinear link = new NeuralLinkLinear(this._layer_tree[i + 1], this._layer_tree[i]);
                this._initialization_rule.Update(link);
                this._link_tree.Add(link);

            }


        }

        // Rendering methods //
        public void Render()
        {

            // Cycle through each epoch //
            for (this._actual_epochs = 0; this._actual_epochs < this._max_epochs; this._actual_epochs++)
            {

                // Create a variable to save the sse //
                this._current_sse = 0D;

                // Calibrate the model //
                for (int i = 0; i < this._data.Length; i++)
                {

                    // Render the node means //
                    this._origin.Calculate(this._data[i]);

                    // Calculate the gradients //
                    this._terminis.BACKWARD_LINK.RenderGradient();

                    // Set the SSE //
                    this._current_sse += this._terminis.SSE;

                }

                //if (this._actual_epochs % 100 == 0)
                //{
                //    Console.WriteLine("EPOCH {0} : SSE {1}", this._actual_epochs, out_layer.SSE);
                //}

                // Check the mse //
                if (this._current_sse <= this._exit_sse)
                {
                    break;
                }

                // Update the weights //
                foreach (NeuralLink link in this._link_tree)
                {
                    link.SSE = this._current_sse;
                    this._update_rule.Update(link);
                    link.Reset();
                }

            }

        }

        public void PrintPredictions(double[][] Data)
        {

            // Print if rendered //
            for (int i = 0; i < Data.Length; i++)
            {

                // Render each node //
                this._origin.Calculate(Data[i]);

                // Write the string //
                Comm.WriteLine(this._terminis.MeanString());

            }


        }

        public string Statistics()
        {

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Neural Network: " + "NN");
            sb.AppendLine("Max Epochs: " + this._max_epochs.ToString());
            sb.AppendLine("Actual Epochs: " + this._actual_epochs.ToString());
            sb.AppendLine("Exit SSE: " + this._exit_sse);
            sb.AppendLine("Actual SSE: " + Math.Round(this._current_sse, 3));
            return sb.ToString();

        }

    }

}
