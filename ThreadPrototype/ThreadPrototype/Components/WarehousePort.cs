using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ThreadPrototype.Components
{
    /// <summary>
    /// Contains the testing parameters and function used to performed the tests done when unit is in warehouse mode
    /// </summary>
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
        /// <returns>returns output</returns>
        public string SendRequest(string requestMessage)
        {
            string output = string.Empty;

            using (Process process = new Process())
            {
                process.StartInfo = processStartInfo;
                process.StartInfo.Arguments = $"/C curl -k --data \"command={requestMessage}\" \"{_url}\"\r\n";
                process.Start();

                output = process.StandardOutput.ReadToEnd();
                Console.WriteLine(output);
                process.WaitForExit();
            }

            return output;

        }
    }
}
