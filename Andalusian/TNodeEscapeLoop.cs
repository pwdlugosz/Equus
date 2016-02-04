using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Equus.Andalusian
{

    /// <summary>
    /// Represents an escape loop statement
    /// </summary>
    public sealed class TNodeEscapeLoop : TNode
    {

        public TNodeEscapeLoop(TNode Parent)
            : base(Parent)
        {
        }

        public override void Invoke()
        {

            // Raise a break loop state //
            this.RaiseUp(1);

        }

        public override string Message()
        {
            return "Break Loop";
        }

        public override TNode CloneOfMe()
        {
            return new TNodeEscapeLoop(this.Parent);
        }

    }


}
