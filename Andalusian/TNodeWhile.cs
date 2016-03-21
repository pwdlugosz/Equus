using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Calabrese;
using Equus.Shire;
using Equus.Horse;

namespace Equus.Andalusian
{

    /// <summary>
    /// While
    /// </summary>
    public sealed class TNodeWhile : TNode
    {

        private FNode _control;

        public TNodeWhile(TNode Parent, FNode ControlFlow)
            : base(Parent)
        {
            this._control = ControlFlow;
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

            while(this._control.Evaluate().BOOL == true)
            {

                // Invoke children //
                foreach (TNode node in this._Children)
                {

                    // Invoke //
                    node.Invoke();

                    // Check for the raise state == 1 or 2//
                    if (node.Raise == 1 || node.Raise == 2)
                        return;

                }

            }

        }

        public override string Message()
        {

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("While-Loop");
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
            TNode node = new TNodeWhile(this.Parent, this._control);
            foreach (TNode t in this._Children)
                node.AddChild(t.CloneOfMe());
            return node;
        }

    }

}
