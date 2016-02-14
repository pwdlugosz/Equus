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

    public sealed class NodeSet
    {

        private List<NeuralNode> _Nodes;

        public NodeSet()
        {
            this._Nodes = new List<NeuralNode>();
        }

        public NodeSet(NodeLinkMaster Master)
            : this()
        {
            this.Add(Master);
        }

        public List<NeuralNode> Nodes
        {
            get { return this._Nodes; }
        }

        public void Add(NeuralNode Node)
        {
            if (this._Nodes.Contains(Node)) return;
            this._Nodes.Add(Node);
        }

        public void Add(NodeLink Link)
        {
            this.Add(Link.Parent);
            this.Add(Link.Child);
        }

        public void Add(NodeLinkMaster Link)
        {
            foreach (NodeLink l in Link.Links)
                this.Add(l);
        }

        public void Render(double[] Data)
        {
            foreach (NeuralNode n in this._Nodes)
                n.Render(Data);
        }

    }

    public sealed class ResponseNodeSet
    {

        private List<NeuralNodePrediction> _Responses;

        public ResponseNodeSet()
        {
            this._Responses = new List<NeuralNodePrediction>();
        }

        public ResponseNodeSet(NodeLinkMaster Master)
            : this()
        {
            this.Add(Master);
        }

        public ResponseNodeSet(NodeSet GenericNodes)
            : this()
        {
            foreach (NeuralNode n in GenericNodes.Nodes)
            {
                if (n.Affinity == NeuralNodeAffinity.Prediction)
                    this.Add(n as NeuralNodePrediction);
            }
        }

        // Properties //
        public double MSE
        {
            get
            {
                double n = 0;
                double x = 0;
                foreach (NeuralNodePrediction p in this._Responses)
                {
                    n++;
                    x += p.MSE;
                }
                return x;
            }
        }

        public List<NeuralNodePrediction> Responses
        {
            get { return this._Responses; }
        }

        public bool IsCircular
        {
            get
            {
                foreach (NeuralNode n in this._Responses)
                    if (NodeAnalytics.AnyCircular(n)) return true;
                return false;
            }
        }

        // Methods //
        public void Add(NeuralNodePrediction Node)
        {
            if (this._Responses.Contains(Node)) return;
            this._Responses.Add(Node);
        }

        public void Add(NodeLink Link)
        {
            if (Link.Parent.Affinity == NeuralNodeAffinity.Prediction)
                this.Add(Link.Parent as NeuralNodePrediction);
        }

        public void Add(NodeLinkMaster Master)
        {
            foreach (NodeLink n in Master.Links)
                this.Add(n);
        }

        public void RenderGradients()
        {
            foreach (NeuralNodePrediction n in this._Responses)
                n.ChainRenderGradient();
        }

        public void ResetSSE()
        {
            foreach (NeuralNodePrediction n in this._Responses)
                n.ResetSSE();
        }

        public string ActualString
        {

            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("[");
                for (int i = 0; i < this._Responses.Count; i++)
                {
                    sb.Append(Math.Round(this._Responses[i].ACTUAL, 4).ToString());
                    if (i != this._Responses.Count - 1) sb.Append(",");
                }
                sb.Append("]");
                return sb.ToString();
            }

        }

        public string PredictionString
        {

            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("[");
                for (int i = 0; i < this._Responses.Count; i++)
                {
                    sb.Append(Math.Round(this._Responses[i].MEAN, 4).ToString());
                    if (i != this._Responses.Count - 1) sb.Append(",");
                }
                sb.Append("]");
                return sb.ToString();
            }

        }

        public string AEString
        {
            get { return "A=" + this.ActualString + " | E=" + this.PredictionString; }
        }

    }

    public abstract class NeuralNode : IEquatable<NeuralNode>
    {

        protected string _Name;
        protected List<NodeLink> _ParentNodes;
        protected List<NodeLink> _ChildNodes;
        protected NeuralNodeAffinity _Affinity;
        protected NodeReduction _Reduction;
        protected Guid _UID;

        public double MEAN = 0;
        public double PARTIAL_GRADIENT = 0;
        public double GRADIENT = 0;
        public double ERROR = 0;

        public NeuralNode(string Name, NodeReduction Reduction, NeuralNodeAffinity Type)
        {
            this._Affinity = Type;
            this._ParentNodes = new List<NodeLink>();
            this._ChildNodes = new List<NodeLink>();
            this._Reduction = Reduction;
            this._Name = Name;
            this._UID = Guid.NewGuid();
        }

        // Properties //
        public List<NodeLink> Parents
        {
            get { return this._ParentNodes; }
        }

        public List<NodeLink> Children
        {
            get { return this._ChildNodes; }
        }

        public NodeReduction Reduction
        {
            get { return this._Reduction; }
        }

        public bool IsTerminal
        {
            get { return this._ChildNodes.Count == 0; }
        }

        public bool IsMaster
        {
            get { return this._ParentNodes.Count == 0; }
        }

        public NeuralNodeAffinity Affinity
        {
            get { return this._Affinity; }
        }

        public string Name
        {
            get { return this._Name; }
        }

        // Non-Abstract Methods //
        public void LinkChild(NodeLinkMaster Master, NeuralNode Node)
        {
            NodeLink link = new NodeLink(Node, this);
            Node._ParentNodes.Add(link);
            this._ChildNodes.Add(link);
            Master.AddLink(link);
        }

        public void LinkChildren(NodeLinkMaster Master, params NeuralNode[] Nodes)
        {
            foreach (NeuralNode n in Nodes)
                this.LinkChild(Master, n);
        }

        public void LinkChildren(NodeLinkMaster Master, List<NeuralNode> Nodes)
        {
            foreach (NeuralNode n in Nodes)
                this.LinkChild(Master, n);
        }

        public void LinkParent(NodeLinkMaster Master, NeuralNode Node)
        {
            NodeLink link = new NodeLink(this, Node);
            this._ParentNodes.Add(link);
            Node._ChildNodes.Add(link);
            Master.AddLink(link);
        }

        public void LinkParents(NodeLinkMaster Master, params NeuralNode[] Nodes)
        {
            foreach (NeuralNode n in Nodes)
                this.LinkParent(Master, n);
        }

        public void LinkParents(NodeLinkMaster Master, List<NeuralNode> Nodes)
        {
            foreach (NeuralNode n in Nodes)
                this.LinkParent(Master, n);
        }

        // Methods //
        public abstract void Render(double[] Data);

        public virtual void BaseReset()
        {
            this.GRADIENT = 0;
            this.PARTIAL_GRADIENT = 0;
            this.MEAN = 0;
        }

        public void ChainReset()
        {

            foreach (NodeLink l in this._ChildNodes)
                l.Reset();

            this.BaseReset();

            foreach (NodeLink c in this._ChildNodes)
                c.Child.ChainReset();

        }

        public void ChainRenderGradient()
        {

            foreach (NodeLink l in this._ChildNodes)
            {
                l.RenderGradient();
                l.Child.ChainRenderGradient();
            }

        }

        public void ChainWeightUpdate(NeuralRule Rule)
        {

            foreach (NodeLink l in this._ChildNodes)
            {
                l.Update(Rule);
                l.Child.ChainWeightUpdate(Rule);
            }

        }

        public void ChainRandomize(Random R)
        {

            foreach (NodeLink l in this._ChildNodes)
            {
                l.WEIGHT = R.NextDouble() * 2 - 1;
                l.Child.ChainRandomize(R);
            }

        }

        public void ChainPrintWeights()
        {

            foreach (NodeLink l in this._ChildNodes)
            {
                string x = string.Format("Parent {0} : Child {1} : WEIGHT {2} : GRADIENT {3}", l.Parent.Name, l.Child.Name, l.WEIGHT, l.GRADIENT);
                Comm.WriteLine(x);
                l.Child.ChainPrintWeights();
            }

        }

        // Overrides //
        public static bool operator ==(NeuralNode A, NeuralNode B)
        {
            return A._UID == B._UID;
        }

        public static bool operator !=(NeuralNode A, NeuralNode B)
        {
            return A._UID != B._UID;
        }

        public override int GetHashCode()
        {
            return this._UID.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is NeuralNode) return this == (obj as NeuralNode);
            return false;
        }

        public bool Equals(NeuralNode Node)
        {
            return this == Node;
        }

    }

    public sealed class NeuralNodeReference : NeuralNode
    {

        private int _Ref = -1;

        public NeuralNodeReference(string Name, int RefIndex)
            : base(Name, null, NeuralNodeAffinity.Reference)
        {
            this._Ref = RefIndex;
        }

        public override void Render(double[] Data)
        {
            this.MEAN = Data[this._Ref];
            this.PARTIAL_GRADIENT = 0;
        }

    }

    public sealed class NeuralNodeStatic : NeuralNode
    {

        public NeuralNodeStatic(string Name, double Value)
            : base(Name, null, NeuralNodeAffinity.Static)
        {
            this.MEAN = Value;
            this.GRADIENT = 0;
        }

        public override void Render(double[] Data)
        {
        }

        public override void BaseReset()
        {
            this.GRADIENT = 0;
            this.PARTIAL_GRADIENT = 0;
        }

    }

    public class NeuralNodeDynamic : NeuralNode
    {

        protected ScalarFunction _Activation;

        public NeuralNodeDynamic(string Name, ScalarFunction Activation, NodeReduction Reduction)
            : base(Name, Reduction, NeuralNodeAffinity.Dynamic)
        {
            this._Activation = Activation;
        }

        public override void Render(double[] Data)
        {
            double nu = this._Reduction.Render(Data, this);
            this.MEAN = this._Activation.Evaluate(nu);
            this.PARTIAL_GRADIENT = this._Activation.Gradient(nu);
        }

    }

    public sealed class NeuralNodePrediction : NeuralNodeDynamic
    {

        private int _ref = -1;
        public double ACTUAL = 0;
        public double SSE = 0;
        private double _Observations = 0;


        public NeuralNodePrediction(string Name, ScalarFunction Activation, NodeReduction Reduction, int VectorIndex)
            : base(Name, Activation, Reduction)
        {
            this._ref = VectorIndex;
            this._Affinity = NeuralNodeAffinity.Prediction;
        }

        // Properties //
        public double MSE
        {
            get { return this.SSE; }
        }

        // Methods //
        public override void Render(double[] Data)
        {
            base.Render(Data);
            this.ACTUAL = Data[this._ref];
            this.ERROR = this.ACTUAL - this.MEAN;
            this.SSE += this.ERROR * this.ERROR;
            this._Observations++;
        }

        public void ResetSSE()
        {
            this.SSE = 0;
            this._Observations = 0;
        }

    }


}
