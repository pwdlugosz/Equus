using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Equus.Andalusian
{
    
    public sealed class TNodeNothing : TNode
    {

        public TNodeNothing(TNode Parent)
            : base(Parent)
        {
        }

        public override void Invoke()
        {
        }

        public override TNode CloneOfMe()
        {
            return new TNodeNothing(this.Parent);
        }

    }

}
