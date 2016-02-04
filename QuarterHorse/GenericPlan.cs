using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Equus.QuarterHorse
{

    public sealed class GenericPlan : CommandPlan
    {

        public GenericPlan(string Name, Action Delegate)
            : base()
        {
            this.Name = Name;
        }

        public Action BaseDelegate
        {
            get;
            private set;
        }

        public override void Execute()
        {
            this._timer = System.Diagnostics.Stopwatch.StartNew();
            this.BaseDelegate.Invoke();
            this._timer.Stop();
        }

    }

}
