using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ThreadPrototype.Components
{
    public class WarehousePort
    {
        private string _url;
        private ProcessStartInfo processStartInfo;
        public WarehousePort(string url) 
        {
            _url = url;

            processStartInfo = new ProcessStartInfo();
            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;
            processStartInfo.RedirectStandardInput = true;
            processStartInfo.FileName = "cmd";
            processStartInfo.CreateNoWindow = true;
        }

        /// <summary>
        /// Sends cUrl request and returns the response
        /// </summary>
        /// <param name="requestMessage"></param>
        /// <returns>returns response</returns>
        public string SendRequest(string requestMessage)
        {
            string output = string.Empty;
            using(Process process = new Process())
            {
                process.StartInfo = processStartInfo;
                process.StartInfo.Arguments = $"/C curl -k --data \"command={requestMessage}\" \"https://192.168.1.1/cgi-bin/warehouse_api\"";
                process.Start();

                output = process.StandardOutput.ReadToEnd();
                Console.WriteLine(output);
                process.WaitForExit();
            }

            return output;

        }
    }
}
