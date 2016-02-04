using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Equus.Horse
{

    public static class Comm
    {

        internal const string HEADER = "<<<<<::::::::::::::::::::{0}::::::::::::::::::::>>>>>";
        
        public static void WriteLine(string Message, params object[] Values)
        {
            Console.WriteLine(Message, Values);
        }

        public static void Write(string Message, params object[] Values)
        {
            Console.Write(Message, Values);
        }

        public static void WriteHeader(string Header)
        {
            Comm.WriteLine(HEADER, Header);
        }

        public static void WriteFooter(string Header)
        {
            string t = new string(':', Header.Length);
            Comm.WriteLine(HEADER, t);
        }

    }

}
