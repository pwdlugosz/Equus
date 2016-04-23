using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Equus.Andalusian
{
    
    public sealed class TNodeGeneric : TNode
    {

        private Action _delegate;

        public TNodeGeneric(TNode Parent, Action Delegate)
            : base(Parent)
        {
            this._delegate = Delegate;
        }

        public override void Invoke()
        {
            this._delegate();
        }

        public override TNode CloneOfMe()
        {
            return new TNodeGeneric(this._Parent, this._delegate);
        }

    }

}
