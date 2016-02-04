using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Calabrese;
using Equus.Shire;
using Equus.Horse;
using Equus.HScript;

namespace Equus.Andalusian
{

    /// <summary>
    /// Prints
    /// </summary>
    public sealed class TNodePrint : TNode
    {

        private FNodeSet _expressions;
        private Workspace _home;

        public TNodePrint(TNode Parent, Workspace Home, FNodeSet Expressions)
            : base(Parent)
        {
            this._expressions = Expressions;
            this._home = Home;
        }

        public override void Invoke()
        {

            if (this._expressions == null)
                return;
            if (this._expressions.Count == 0)
                return;
            if (this._expressions.Count == 1)
                this._home.IO.AppendBuffer(this._expressions.Evaluate()[0].ToString());
            else
                this._home.IO.AppendBuffer(this._expressions.Evaluate().ToString('\t'));

        }

        public override string Message()
        {
            return "Print";
        }

        public override TNode CloneOfMe()
        {
            return new TNodePrint(this.Parent, this._home, this._expressions.CloneOfMe());
        }

    }

}
