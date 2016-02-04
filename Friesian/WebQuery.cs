using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;

namespace Equus.Friesian
{

    public static class WebQuery
    {

        public static void ProcessWebRequest(string URL, Stream Writer)
        {

            // Variables //
            WebRequest webreq;
            
            // Nokota request and response //
            webreq = HttpWebRequest.Create(URL);

            using (Stream stream = webreq.GetResponse().GetResponseStream())
            {
                stream.CopyTo(Writer);
            }

        }

        public static void ProcessWebRequest(string URL, string OutputFile)
        {

            // Open a stream //
            using (Stream writer = File.Create(OutputFile))
            {

                ProcessWebRequest(URL, writer);

            }

        }

        public static bool TryProcessWebRequest(string URL, string OutputFile)
        {

            try
            {
                ProcessWebRequest(URL, OutputFile);
            }
            catch (Exception e)
            {
                Horse.Comm.WriteLine(e.Message);
                return false;
            }
            return true;

        }

    }

    
}
