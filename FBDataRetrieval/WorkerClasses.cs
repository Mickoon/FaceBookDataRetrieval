using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;

namespace FBDataRetrieval
{
    class WorkerClasses
    {
        public static string getSourceCode(string url)
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8);
                string sourceCode = sr.ReadToEnd();
                sr.Close();
                resp.Close();
                return sourceCode;
            }
            catch
            {
                return "invalid";
            }
        }
    }
}
