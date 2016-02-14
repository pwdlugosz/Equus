using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Equus.Numerics
{

    public enum ScalarFunctionType
    {

        BinarySigmoid,
        BinaryHyperTangent,
        BinarySine,
        BinaryCosine,
        BinaryQuadratic,
        BinaryGaussian,
        DipolSigmoid,
        DipolHyperTangent,
        DipolSine,
        DipolCosine,
        DipolQuadratic,
        DipolGaussian,
        RealLinear,
        RealLogrithmicRectifier,
        RealHyperbolicRectifier,
        RealQuadradicRectifier,
        RealPossion,
        RealInverse,
        RealInverseSquare,
        RealSquare,
        RealSquareRoot

    }

    public abstract class ScalarFunction
    {

        protected const double E = 0.0001;

        public string Name
        {
            get { return this.GetType().ToString().Split('.').Last(); }
        }

        public abstract double Evaluate(double x);

        public virtual double Gradient(double x)
        {
            return (Evaluate(x + E) - Evaluate(x + E)) / (2 * E);
        }

        public virtual double Gradient2(double x)
        {
            return (Gradient(x + E) - Gradient(x - E)) / (2 * E);
        }

    }

    public class BinarySigmoid : ScalarFunction
    {

        public override double Evaluate(double x)
        {
            return 1D / (1D + Math.Exp(-x));
        }

        public override double Gradient(double x)
        {
            double p = 1D / (1D + Math.Exp(-x));
            return (p) * (1D - p);
        }

    }

    public class BinaryHyperTangent : ScalarFunction
    {

        public override double Evaluate(double x)
        {
            return Math.Tanh(x) * 0.50 + 0.50;
        }

        public override double Gradient(double x)
        {
            double d = Math.Tanh(x);
            return (1 - d * d) * 0.50;
        }

    }

    public class BinarySine : ScalarFunction
    {

        public override double Evaluate(double x)
        {
            return Math.Sin(x) * 0.50 + 0.50;
        }

        public override double Gradient(double x)
        {
            return Math.Cos(x) * 0.50;
        }

    }

    public class BinaryCosine : ScalarFunction
    {

        public override double Evaluate(double x)
        {
            return Math.Cos(x) * 0.50 + 0.50;
        }

        public override double Gradient(double x)
        {
            return -Math.Sin(x) * 0.50;
        }


    }

    public class BinaryQuadratic : ScalarFunction
    {

        public override double Evaluate(double x)
        {
            return 1 / (1 + x * x);
        }

        public override double Gradient(double x)
        {
            double dx = -2 * x / Math.Pow(1 + x * x, 2);
            return dx;
        }

    }

    public class BinaryGaussian : ScalarFunction
    {

        public override double Evaluate(double x)
        {
            return SpecialFunctions.NormalCDF(x);
        }

        public override double Gradient(double x)
        {
            return SpecialFunctions.NormalPDF(x);
        }

    }

    public class DipolSigmoid : ScalarFunction
    {

        public override double Evaluate(double x)
        {
            return 1 / (1 + Math.Exp(-x)) * 2 - 1;
        }

        public override double Gradient(double x)
        {
            double d = 1 / (1 + Math.Exp(-x));
            return d * (1 - d) * 2;
        }

    }

    public class DipolHyperTangent : ScalarFunction
    {

        public override double Evaluate(double x)
        {
            return Math.Tanh(x);
        }

        public override double Gradient(double x)
        {
            double d = Math.Tanh(x);
            return 1 - d * d;
        }

    }

    public class DipolSine : ScalarFunction
    {

        public override double Evaluate(double x)
        {
            return Math.Sin(x);
        }

        public override double Gradient(double x)
        {
            return Math.Cos(x);
        }

    }

    public class DipolCosine : ScalarFunction
    {

        public override double Evaluate(double x)
        {
            return Math.Cos(x);
        }

        public override double Gradient(double x)
        {
            return -Math.Sin(x);
        }

    }

    public class DipolQuadratic : ScalarFunction
    {

        public override double Evaluate(double x)
        {
            return 1 / (1 + x * x) * 2 - 1;
        }

        public override double Gradient(double x)
        {
            double dx = -2 * x / Math.Pow(1 + x * x, 2) * 2;
            return dx;
        }

    }

    public class DipolGaussian : ScalarFunction
    {

        public override double Evaluate(double x)
        {
            return SpecialFunctions.NormalCDF(x) * 2 - 1;
        }

        public override double Gradient(double x)
        {
            return SpecialFunctions.NormalPDF(x) * 2;
        }

    }

    public class RealLinear : ScalarFunction
    {

        public override double Evaluate(double x)
        {
            return x;
        }

        public override double Gradient(double x)
        {
            return 1;
        }


    }

    public class RealLogrithmicRectifier : ScalarFunction
    {

        private double _Bias = 1;

        public override double Evaluate(double x)
        {
            return Math.Log(_Bias + Math.Exp(x));
        }

        public override double Gradient(double x)
        {
            return 1 / (Math.Exp(-x) + 1);
        }

    }

    public class RealQuadradicRectifier : ScalarFunction
    {

        public override double Evaluate(double x)
        {
            return 0.5 * (Math.Sqrt(1 + x * x) + x);
        }

        public override double Gradient(double x)
        {
            return 0.5 + 0.5 * x / Math.Sqrt(1 + x * x);
        }

    }

    public class RealHyperbolicRectifier : ScalarFunction
    {

        public const double LOG_BASE_E_2 = 0.693147180559945;

        public override double Evaluate(double x)
        {
            return (Math.Log(Math.Cosh(x)) + x + LOG_BASE_E_2) / 2;
        }

        public override double Gradient(double x)
        {
            return Math.Tanh(x);
        }

    }

    public class RealPossion : ScalarFunction
    {

        public override double Evaluate(double x)
        {
            return Math.Exp(x);
        }

        public override double Gradient(double x)
        {
            return this.Evaluate(x);
        }

    }

    public class RealInverse : ScalarFunction
    {

        public override double Evaluate(double x)
        {
            return 1 / x;
        }

        public override double Gradient(double x)
        {
            return -1 / (x * x);
        }

    }

    public class RealInverseSquare : ScalarFunction
    {

        public override double Evaluate(double x)
        {
            return 1 / Math.Sqrt(x);
        }

        public override double Gradient(double x)
        {
            return -0.5 / Math.Pow(x, 1.5);
        }

    }

    public class RealSquare : ScalarFunction
    {

        public override double Evaluate(double x)
        {
            return x * x;
        }

        public override double Gradient(double x)
        {
            return 2 * x;
        }

    }

    public class RealSquareRoot : ScalarFunction
    {

        public override double Evaluate(double x)
        {
            return Math.Sqrt(x);
        }

        public override double Gradient(double x)
        {
            return -1 / Math.Sqrt(x);
        }

    }

    public static class ScalarFunctionFactory
    {

        public static ScalarFunction Select(ScalarFunctionType Type)
        {

            switch (Type)
            {

                case ScalarFunctionType.BinaryCosine: return new BinaryCosine();
                case ScalarFunctionType.BinaryGaussian: return new BinaryGaussian();
                case ScalarFunctionType.BinaryHyperTangent: return new BinaryHyperTangent();
                case ScalarFunctionType.BinaryQuadratic: return new BinaryQuadratic();
                case ScalarFunctionType.BinarySigmoid: return new BinarySigmoid();
                case ScalarFunctionType.BinarySine: return new BinarySine();

                case ScalarFunctionType.DipolCosine: return new BinaryCosine();
                case ScalarFunctionType.DipolGaussian: return new BinaryGaussian();
                case ScalarFunctionType.DipolHyperTangent: return new BinaryHyperTangent();
                case ScalarFunctionType.DipolQuadratic: return new BinaryQuadratic();
                case ScalarFunctionType.DipolSigmoid: return new BinarySigmoid();
                case ScalarFunctionType.DipolSine: return new BinarySine();

                case ScalarFunctionType.RealInverse: return new RealInverse();
                case ScalarFunctionType.RealInverseSquare: return new RealInverseSquare();
                case ScalarFunctionType.RealLinear: return new RealLinear();
                case ScalarFunctionType.RealLogrithmicRectifier: return new RealLogrithmicRectifier();
                case ScalarFunctionType.RealHyperbolicRectifier: return new RealHyperbolicRectifier();
                case ScalarFunctionType.RealPossion: return new RealPossion();
                case ScalarFunctionType.RealQuadradicRectifier: return new RealQuadradicRectifier();
                case ScalarFunctionType.RealSquare: return new RealSquare();
                case ScalarFunctionType.RealSquareRoot: return new RealSquareRoot();

                default: return new RealLinear();

            }

        }

        public static ScalarFunction Select(string Text)
        {

            ScalarFunctionType t;
            bool exists = Enum.TryParse<ScalarFunctionType>(Text, true, out t);
            if (!exists)
                throw new Exception(string.Format("ScalarFunctionType '{0}' is invalid", Text));
            return Select(t);

        }

    }


}
