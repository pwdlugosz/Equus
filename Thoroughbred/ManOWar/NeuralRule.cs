using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Equus.Thoroughbred.ManOWar
{

    /// <summary>
    /// Represents the base class for propigation training methods
    /// </summary>
    public abstract class NeuralRule
    {

        public const double FLOOR = 0.0001;
        public const double CEILING = 2;
        public const double EPSILON = 0.000001;

        protected ResponseNodeSet _master;

        public NeuralRule(ResponseNodeSet Master)
        {
            this._master = Master;
        }

        public double Sign(double Value)
        {
            if (Value < EPSILON) return -1;
            if (Value > EPSILON) return 1;
            return 0;
        }

        public abstract double WeightChange(NodeLink Link);

        public double Bound(double Value, double Floor, double Ceiling)
        {
            if (double.IsInfinity(Value) || double.IsNaN(Value)) return Floor;
            return Math.Max(Floor, Math.Min(Ceiling, Math.Abs(Value))) * Math.Sign(Value);
        }

        public double Bound(double Value)
        {
            return Bound(Value, FLOOR, CEILING);
        }


    }

    /// <summary>
    /// Backpropigation weight update; w += dx * learning_rate + dx2 * momentum
    /// </summary>
    public sealed class BProp : NeuralRule
    {

        private double _LearningRate = 0;
        private double _Momentum = 0;

        public BProp(ResponseNodeSet Master, double LearningRate, double Momentum)
            : base(Master)
        {
            this._LearningRate = LearningRate;
            this._Momentum = Momentum;
        }

        public BProp(ResponseNodeSet Master)
            : this(Master, 0.25, 0.025)
        {
        }

        public override double WeightChange(NodeLink Link)
        {
            return Link.GRADIENT * this._LearningRate + Link.GRADIENT_LAG * this._Momentum;
        }

        public override string ToString()
        {
            return string.Format("BPROP;{0};{1}", this._LearningRate, this._Momentum);
        }

    }

    /// <summary>
    /// Resiliant propigation weight up; w += bound(d * sign(dx), 0.0001, 2.00), where d changes each round
    /// </summary>
    public class RProp : NeuralRule
    {

        protected double _UpAxon = 1.2;
        protected double _DownAxon = 0.5;

        public RProp(ResponseNodeSet Master, double Down, double Up)
            : base(Master)
        {
            this._DownAxon = Down;
            this._UpAxon = Up;
        }

        public RProp(ResponseNodeSet Master)
            : this(Master, 0.5, 1.2)
        {
        }

        public override double WeightChange(NodeLink Link)
        {

            // Variables //
            double s = 0;
            double w = 0;
            double d = 0;

            s = Math.Sign(Link.GRADIENT * Link.GRADIENT_LAG);
            d = Link.DELTA;

            if (s > 0)
            {
                d = Math.Min(CEILING, this._UpAxon * d);
                w = d * Math.Sign(Link.GRADIENT);
            }
            else if (s < 0)
            {
                d = Math.Max(FLOOR, this._DownAxon * d);
                Link.GRADIENT = 0;
            }
            else
                w = d * Math.Sign(Link.GRADIENT);

            Link.DELTA_LAG = Link.DELTA;
            Link.DELTA = d;
            return w;

        }

        public override string ToString()
        {
            return string.Format("RPROP;{0};{1}", this._DownAxon, this._UpAxon);
        }

    }

    /// <summary>
    /// Resiliant propigation PLUS weight up; w += bound(d * sign(dx), 0.0001, 2.00), where d changes each round
    /// </summary>
    public sealed class RPropPlus : RProp
    {

        public RPropPlus(ResponseNodeSet Master, double Down, double Up)
            : base(Master, Down, Up)
        {
        }

        public RPropPlus(ResponseNodeSet Master)
            : base(Master)
        {
        }

        public override double WeightChange(NodeLink Link)
        {

            // Variables //
            double s = 0;
            double w = 0;
            double d = 0;

            s = Math.Sign(Link.GRADIENT * Link.GRADIENT_LAG);
            d = Link.DELTA;

            if (s > 0)
            {
                d = Math.Min(CEILING, this._UpAxon * d);
                w = d * Math.Sign(Link.GRADIENT);
            }
            else if (s < 0)
            {
                d = Math.Max(FLOOR, this._DownAxon * d);
                w = -Link.WEIGHT_CHANGE;
                Link.GRADIENT = 0;
            }
            else
                w = d * Math.Sign(Link.GRADIENT);

            Link.DELTA_LAG = Link.DELTA;
            Link.DELTA = d;
            return w;

        }

        public override string ToString()
        {
            return string.Format("RPROP+;{0};{1}", this._DownAxon, this._UpAxon);
        }

    }

    /// <summary>
    /// Resiliant propigation MINUS weight up; w += bound(d * sign(dx), 0.0001, 2.00), where d changes each round
    /// </summary>
    public sealed class RPropMinus : RProp
    {

        public RPropMinus(ResponseNodeSet Master, double Down, double Up)
            : base(Master, Down, Up)
        {
        }

        public RPropMinus(ResponseNodeSet Master)
            : base(Master)
        {
        }

        public override double WeightChange(NodeLink Link)
        {

            // Variables //
            double s = 0;
            double w = 0;
            double d = 0;

            s = Math.Sign(Link.GRADIENT * Link.GRADIENT_LAG);
            d = Link.DELTA;

            if (s > 0)
                d = Math.Min(CEILING, this._UpAxon * d);
            else if (s < 0)
                d = Math.Max(FLOOR, this._DownAxon * d);
            w = d * Math.Sign(Link.GRADIENT);

            Link.DELTA_LAG = Link.DELTA;
            Link.DELTA = d;
            return w;

        }

        public override string ToString()
        {
            return string.Format("RPROP-;{0};{1}", this._DownAxon, this._UpAxon);
        }

    }

    /// <summary>
    /// Improved resiliant propigation PLUS weight up; w += bound(d * sign(dx), 0.0001, 2.00), where d changes each round
    /// </summary>
    public sealed class iRPropPlus : RProp
    {

        private double _LagSSE = double.MaxValue;

        public iRPropPlus(ResponseNodeSet Master, double Down, double Up)
            : base(Master, Down, Up)
        {
        }

        public iRPropPlus(ResponseNodeSet Master)
            : base(Master)
        {
        }

        public override double WeightChange(NodeLink Link)
        {

            // Variables //
            double s = 0;
            double w = 0;
            double d = 0;

            s = Math.Sign(Link.GRADIENT * Link.GRADIENT_LAG);
            d = Link.DELTA;

            if (s > 0)
            {
                d = Math.Min(CEILING, this._UpAxon * d);
                w = d * Math.Sign(Link.GRADIENT);
            }
            else if (s < 0)
            {
                d = Math.Max(FLOOR, this._DownAxon * d);
                if (this._master.MSE > this._LagSSE) w = -Link.WEIGHT_CHANGE;
                Link.GRADIENT = 0;
            }
            else
                w = d * Math.Sign(Link.GRADIENT);

            Link.DELTA_LAG = Link.DELTA;
            Link.DELTA = d;
            this._LagSSE = this._master.MSE;
            return w;

        }

        public override string ToString()
        {
            return string.Format("iRPROP+;{0};{1}", this._DownAxon, this._UpAxon);
        }

    }

    /// <summary>
    /// Improved resiliant propigation MINUS weight up; w += bound(d * sign(dx), 0.0001, 2.00), where d changes each round
    /// </summary>
    public sealed class iRPropMinus : RProp
    {

        public iRPropMinus(ResponseNodeSet Master, double Down, double Up)
            : base(Master, Down, Up)
        {
        }

        public iRPropMinus(ResponseNodeSet Master)
            : base(Master)
        {
        }

        public override double WeightChange(NodeLink Link)
        {

            // Variables //
            double s = 0;
            double w = 0;
            double d = 0;

            s = Math.Sign(Link.GRADIENT * Link.GRADIENT_LAG);
            d = Link.DELTA;

            if (s > 0)
            {
                d = Math.Min(CEILING, this._UpAxon * d);
                w = d * Math.Sign(Link.GRADIENT);
            }
            else if (s < 0)
            {
                d = Math.Max(FLOOR, this._DownAxon * d);
                w = d * Math.Sign(Link.GRADIENT);
                Link.GRADIENT = 0;
            }
            else
                w = d * Math.Sign(Link.GRADIENT);

            Link.DELTA_LAG = Link.DELTA;
            Link.DELTA = d;
            return w;

        }

        public override string ToString()
        {
            return string.Format("iRPROP-;{0};{1}", this._DownAxon, this._UpAxon);
        }

    }

    /// <summary>
    /// Manhattan propigation; w += sign(dx) * learning_rate + sign(dx2) * momentum 
    /// </summary>
    public sealed class MProp : NeuralRule
    {

        private double _GradientWeight = 0.01;
        private double _MomentumWeight = 0.005;

        public MProp(ResponseNodeSet Master, double Gradient, double Momentum)
            : base(Master)
        {
            this._GradientWeight = Gradient;
            this._MomentumWeight = Momentum;
        }

        public MProp(ResponseNodeSet Master)
            : base(Master)
        {
        }

        public override double WeightChange(NodeLink Link)
        {

            // Sign(dx) x LearningRate + Sign(dx2) x Momentum, dx2 = second derivative //
            // dx2 actually is (gradient - lag_gradient) / weight_change, since we dont care about the sign, just multiply //
            return this._GradientWeight * Math.Sign(Link.GRADIENT) + this._MomentumWeight * Math.Sign((Link.GRADIENT - Link.GRADIENT_LAG) * Link.WEIGHT_CHANGE);

        }

        public override string ToString()
        {
            return string.Format("MPROP;{0};{1}", this._GradientWeight, this._MomentumWeight);
        }

    }

    /// <summary>
    /// Quick propigation
    /// </summary>
    public sealed class QProp : NeuralRule
    {

        private double _Slope = 0.3;
        private double _Acceleration = 0.1;
        private double _Scale = 0.50;

        public QProp(ResponseNodeSet Master, double Slope, double Acceleration, double Scale)
            : base(Master)
        {
            this._Slope = Slope;
            this._Acceleration = Acceleration;
            this._Scale = Scale;
        }

        public QProp(ResponseNodeSet Master)
            : base(Master)
        {
        }

        public override double WeightChange(NodeLink Link)
        {

            double dx = Link.GRADIENT;
            double dx2 = (Link.GRADIENT - Link.GRADIENT_LAG) / Link.WEIGHT_CHANGE;
            double q = this.Bound(Link.GRADIENT / (Link.GRADIENT_LAG - Link.GRADIENT) * Link.WEIGHT_CHANGE, FLOOR, CEILING);

            if (Math.Abs(Link.GRADIENT) >= Math.Abs(Link.GRADIENT_LAG * this._Scale))
                return Link.GRADIENT * this._Slope + Link.GRADIENT_LAG * this._Acceleration;
            else
                return q;

        }

        public override string ToString()
        {
            return string.Format("QPROP;{0};{1};{2}", this._Slope, this._Acceleration, this._Scale);
        }

    }

    public sealed class HProp : RProp
    {

        public HProp(ResponseNodeSet Master, double Down, double Up)
            : base(Master, Down, Up)
        {
        }

        public HProp(ResponseNodeSet Master)
            : base(Master)
        {
        }

        public override double WeightChange(NodeLink Link)
        {

            // Variables //
            double w = 0;
            double s = Math.Sign(Link.GRADIENT * Link.GRADIENT_LAG);
            double d = Link.DELTA;
            double p = 1 - Link.DELTA / (Link.DELTA + Link.DELTA_LAG);

            if (s > 0)
            {
                d = Math.Min(CEILING, this._UpAxon * d);
                w = d * Math.Sign(Link.GRADIENT) + Link.DELTA * Math.Sign(Link.GRADIENT_LAG);
            }
            else if (s < 0)
            {
                d = Math.Max(FLOOR, this._DownAxon * d);
                w = -Link.WEIGHT_CHANGE * 0.5;
                Link.GRADIENT = 0;
            }
            else
                w = d * Math.Sign(Link.GRADIENT) + Link.DELTA * Math.Sign(Link.GRADIENT_LAG);

            Link.DELTA_LAG = Link.DELTA;
            Link.DELTA = d;
            return w;

        }

    }

}
