using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Equus.Andalusian
{

    /// <summary>
    /// Breaks out of a read statement
    /// </summary>
    public sealed class TNodeEscapeRead : TNode
    {

        public TNodeEscapeRead(TNode Parent)
            : base(Parent)
        {
        }

        public override void Invoke()
        {
            // Raise a break read state //
            this.RaiseUp(2);
        }

        public override string Message()
        {
            return "Bread Read";
        }

        public override TNode CloneOfMe()
        {
            return new TNodeEscapeRead(this.Parent);
        }

    }

}
