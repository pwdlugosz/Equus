using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Calabrese;
using Equus.Shire;
using Equus.Clydesdale;
using Equus.HScript.Parameters;

namespace Equus.Andalusian
{
    
    public sealed class TNodeProcedure : TNode
    {

        private Procedure _proc;
        HParameterSet _parameters;

        public TNodeProcedure(TNode Parent, Procedure Proc, HParameterSet Parameters)
            : base(Parent)
        {
            this._proc = Proc;
            this._parameters = Parameters;
        }

        public override void Invoke()
        {
            this._proc.Invoke();
        }

        public override string Message()
        {
            return string.Format("Execute procedure '{0}'", this._proc.Name);
        }

        public override TNode CloneOfMe()
        {
            // Note: procedures are immutable, so no need to clone
            return new TNodeProcedure(this.Parent, this._proc, this._parameters);
        }

    }

}
