using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Equus.HScript
{

    public sealed class CommandReturn
    {

        public CommandReturn(string UseName)
        {
            this.Name = UseName;
            this.Message = "Command: " + this.Name;
            this.CompileTimer = new Stopwatch();
            this.RunTimer = new Stopwatch();
        }

        public string Name
        {
            get;
            set;
        }

        public string Message
        {
            get;
            set;
        }

        public Stopwatch CompileTimer
        {
            get;
            set;
        }

        public Stopwatch RunTimer
        {
            get;
            set;
        }


    }

}
