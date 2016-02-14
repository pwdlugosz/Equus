using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Equus.Thoroughbred.ManOWar
{

    public sealed class NodeLink
    {

        public double WEIGHT = 0;
        public double WEIGHT_LAG = 0;
        public double WEIGHT_CHANGE = 0.01;
        public double GRADIENT = 0;
        public double GRADIENT_LAG = 0;
        private double GRADIENT_WORKING = 0;
        public double DELTA = 0.1;
        public double DELTA_LAG = 0.1;

        public NeuralNode Child;
        public NeuralNode Parent;

        public NodeLink(NeuralNode Child, NeuralNode Parent, double Weight)
        {
            if (Child.Affinity == NeuralNodeAffinity.Prediction)
                throw new Exception("A Prediction node cannot be linked as a child");
            if (Parent.Affinity == NeuralNodeAffinity.Static)
                throw new Exception("A static node cannot be linked as a parent");
            if (Parent.Affinity == NeuralNodeAffinity.Reference)
                throw new Exception("A reference node cannot be linked as a parent");
            this.Child = Child;
            this.Parent = Parent;
            this.WEIGHT = Weight;
        }

        public NodeLink(NeuralNode Child, NeuralNode Parent)
            : this(Child, Parent, 0)
        {
        }

        /*
         * Function: F(G(H(I(z))))
         * GRADIENT: F'(G o H o I) * G'(H o I) * H'(I) * I'
         * 
         * GRADIENT for the weight update:
         *      Parent GRADIENT x Parent Reduction Terminal Chain Rule
         *  
         * GRADIENT for the backprop:
         *      Parent GRADIENT x Parent Reduction Interim Chain Rule
         * 
         * 
         */
        public void RenderGradient()
        {

            double dx = 0;

            // If parent terminal //
            if (this.Parent.IsMaster)
            {

                dx = this.Parent.PARTIAL_GRADIENT * this.Parent.ERROR;
                this.GRADIENT += this.Parent.Reduction.TerminalChainRule(dx, this.WEIGHT, this.Child.MEAN);
                this.GRADIENT_WORKING = this.Parent.Reduction.InterimChainRule(dx, this.WEIGHT, this.Child.MEAN);

            }
            else
            {

                foreach (NodeLink l in this.Parent.Parents)
                    dx += this.Parent.PARTIAL_GRADIENT * l.GRADIENT_WORKING;
                this.GRADIENT += this.Parent.Reduction.TerminalChainRule(dx, this.WEIGHT, this.Child.MEAN);
                this.GRADIENT_WORKING = this.Parent.Reduction.InterimChainRule(dx, this.WEIGHT, this.Child.MEAN);

            }

        }

        public void Reset()
        {
            this.GRADIENT_LAG = this.GRADIENT;
            this.GRADIENT = 0;
            //this.DELTA = this.DELTA_LAG;
            //this.DELTA = 0;
        }

        public void Update(NeuralRule Rule)
        {
            this.WEIGHT_CHANGE = Rule.WeightChange(this);
            this.WEIGHT_LAG = this.WEIGHT;
            this.WEIGHT += this.WEIGHT_CHANGE;
        }

    }

    public sealed class NodeLinkMaster
    {

        private List<NodeLink> _Links;

        public NodeLinkMaster()
        {
            this._Links = new List<NodeLink>();
        }

        public List<NodeLink> Links
        {
            get { return this._Links; }
        }

        public void AddLink(NodeLink Link)
        {
            this._Links.Add(Link);
        }

        public void Randomize(Random Randomizer)
        {
            foreach (NodeLink n in this._Links)
                n.WEIGHT = Randomizer.NextDouble() * 2 - 1;
        }

        public void Randomize(int Seed)
        {
            this.Randomize(new Random(Seed));
        }

        public void Reset()
        {
            foreach (NodeLink n in this._Links)
                n.Reset();
        }

        public void WeightUpdate(NeuralRule Rule)
        {
            foreach (NodeLink n in this._Links)
                n.Update(Rule);
        }

        public string TreeString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (NodeLink l in this._Links)
                sb.AppendLine(string.Format("Parent {0} -> Child {1}", l.Parent.Name, l.Child.Name));
            return sb.ToString();
        }

    }

}
