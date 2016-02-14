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

    /*
    * TerminalChainRule: dY/dW
    *      -- dY * e * H1
    * 
    * InterimChainRule: dY/dH
    *      -- dY * e * W1
    *      
    */
    public abstract class NodeReduction
    {

        public abstract double Render(double[] Data, NeuralNode Node);

        public abstract double TerminalChainRule(double Gradient, double Weight, double NodeValue);

        public abstract double InterimChainRule(double Gradient, double Weight, double NodeValue);

    }

    public sealed class NodeReductionLinear : NodeReduction
    {

        public override double Render(double[] Data, NeuralNode Node)
        {

            double d = 0;
            foreach (NodeLink n in Node.Children)
            {
                n.Child.Render(Data);
                d += n.WEIGHT * n.Child.MEAN;
            }
            return d;

        }

        public override double TerminalChainRule(double Gradient, double Weight, double NodeValue)
        {
            return Gradient * NodeValue;
        }

        public override double InterimChainRule(double Gradient, double Weight, double NodeValue)
        {
            return Gradient * Weight;
        }

    }

}
