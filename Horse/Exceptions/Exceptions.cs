using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Equus.Horse
{

    public sealed class HorseDataException : Exception
    {

        public HorseDataException(string Message, params object[] Args)
            : base(string.Format(Message, Args))
        {
        }

    }

    public sealed class HScriptCompileException : Exception
    {

        public HScriptCompileException(string Message, params object[] Args)
            : base(string.Format(Message, Args))
        {
        }

    }

    public sealed class HScriptParseException : Exception
    {

        public HScriptParseException(string Message, params object[] Args)
            : base(string.Format(Message, Args))
        {
        }

    }


}
