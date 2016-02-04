using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Equus.QuarterHorse
{

    public sealed class ActionPlan : CommandPlan
    {

        private Andalusian.TNode _node;

        public ActionPlan(Andalusian.TNode Node)
            : base()
        {
            this._node = Node;
            this.Name = "Action";
        }

        public override void Execute()
        {
            this._timer = System.Diagnostics.Stopwatch.StartNew();
            this._node.BeginInvoke();
            this._node.Invoke();
            this._node.EndInvoke();
            this._timer.Stop();
            this.Message.AppendLine(this._node.Message());
        }


    }

}
