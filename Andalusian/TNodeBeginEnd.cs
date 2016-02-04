using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Equus.Andalusian
{

    /// <summary>
    /// Represents set of actions not in a tree
    /// </summary>
    public sealed class TNodeBeginEnd : TNode
    {

        public TNodeBeginEnd(TNode Parent)
            : base(Parent)
        {
        }

        public override long Writes
        {
            get
            {
                return this._Children.Sum<TNode>((x) => { return x.Writes; });
            }
            protected set
            {
                base.Writes = value;
            }
        }

        public override void BeginInvoke()
        {
            base.BeginInvoke();
            this.BeginInvokeChildren();
        }

        public override void EndInvoke()
        {
            base.EndInvoke();
            this.EndInvokeChildren();
        }

        public override void Invoke()
        {
            this.InvokeChildren();
        }

        public override string Message()
        {

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Begin-End");
            for (int i = 0; i < this._Children.Count; i++)
            {

                if (i != this._Children.Count - 1)
                    sb.AppendLine('\t' + this._Children[i].Message());
                else
                    sb.Append('\t' + this._Children[i].Message());

            }
            return sb.ToString();

        }

        public override TNode CloneOfMe()
        {
            TNodeBeginEnd node = new TNodeBeginEnd(this.Parent);
            foreach (TNode t in this._Children)
                node.AddChild(t.CloneOfMe());
            return node;
        }

    }

}
