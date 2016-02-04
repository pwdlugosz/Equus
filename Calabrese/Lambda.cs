using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;

namespace Equus.Calabrese
{

    public sealed class Lambda
    {

        private FNode _Expression;
        private string _Name;
        private List<string> _Pointers;

        // Constructor //
        public Lambda(string Name, FNode Expression, List<string> Parameters)
        {
            
            this._Expression = Expression;
            this._Name = Name;
            this._Pointers = Parameters;
            
        }

        public Lambda(string Name, FNode Expression)
            : this(Name, Expression, FNodeAnalysis.AllPointersRefs(Expression).Distinct().ToList())
        {
        }

        public Lambda(string Name, FNode Expression, string Parameter)
            : this(Name, Expression, new List<string>() { Parameter })
        {
        }

        // Properties //
        public FNode InnerNode
        {
            get { return this._Expression; }
        }

        public string Name
        {
            get { return this._Name; }
        }

        public List<string> Pointers
        {
            get { return this._Pointers; }
        }

        // Methods //
        public FNode Bind(List<FNode> Bindings)
        {

            FNode node = this._Expression.CloneOfMe();
            List<FNodePointer> refs = FNodeAnalysis.AllPointers(node);

            Dictionary<string, int> idx = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int i = 0;
            foreach (string s in this._Pointers)
            {
                idx.Add(s, i);
                i++;
            }

            foreach (FNodePointer n in refs)
            {
                int node_ref = idx[n.Name];
                FNode t = Bindings[node_ref];
                FNodeAnalysis.ReplaceNode(n, t);
            }

            return node; 

        }

        public Lambda Gradient(string Name, string PointerName)
        {

            FNode node = FNodeGradient.Gradient(this._Expression, PointerName);
            return new Lambda(Name, node, this._Pointers);

        }

        public FNode Gradient(string PointerName)
        {
            return FNodeGradient.Gradient(this._Expression, PointerName);
        }

        public FNodeSet PartialGradients()
        {

            FNodeSet nodes = new FNodeSet();
            foreach (string ptr in this._Pointers)
                nodes.Add(ptr, this.Gradient(ptr));
            return nodes;

        }

        public bool IsDifferntiable(string PointerName)
        {

            try
            {
                Lambda dx = Gradient("test", PointerName);
                return true;
            }
            catch
            {
                return false;
            }

        }

        public string Unparse(Schema Columns)
        {
            return this._Expression.Unparse(Columns);
        }

    }

}
