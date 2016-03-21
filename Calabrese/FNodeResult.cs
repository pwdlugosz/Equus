using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;

namespace Equus.Calabrese
{

    public sealed class FNodeResult : FNode
    {

        private CellFunction _Func;

        public FNodeResult(FNode Parent, CellFunction Function)
            : base(Parent, FNodeAffinity.ResultNode)
        {
            this._Func = Function;
        }

        public void SetParameterCount(int Count)
        {
            this._Func.ParamCount = Count;
        }

        public CellFunction InnerFunction
        {
            get { return this._Func; }
        }

        public override Cell Evaluate()
        {
            return _Func.Evaluate(this.EvaluateChildren());
        }

        public override CellAffinity ReturnAffinity()
        {
            CellAffinity[] c = this.ReturnAffinityChildren();
            return _Func.ReturnAffinity(c);
        }

        public override string ToString()
        {
            return this._Func.NameSig;
        }

        public override int GetHashCode()
        {
            return this._Func.GetHashCode() ^ FNode.HashCode(this._Cache);
        }

        public override string Unparse(Schema S)
        {

            List<string> text = new List<string>();
            foreach (FNode ln in this.Children)
                text.Add(ln.Unparse(S));
            return this._Func.Unparse(text.ToArray(), S);

        }

        public override FNode CloneOfMe()
        {
            FNodeResult Dolly = new FNodeResult(this.ParentNode, this._Func);
            foreach (FNode n in this._Cache)
                Dolly.AddChildNode(n.CloneOfMe());
            return Dolly;
        }

        public override void AssignRegister(Shire.Register Memory)
        {
            foreach (FNode n in this._Cache)
                n.AssignRegister(Memory);
        }

        public static FNode Generate(FNode Parent, Func<Cell[], Cell> Delegate, int Parameters, CellAffinity ReturnAffinity, string ToString)
        {

            CellFunction f = new CellFunction(ToString, Parameters, Delegate, ReturnAffinity, (x,y) => { return ToString; });
            return new FNodeResult(Parent, f);

        }

    }

}
