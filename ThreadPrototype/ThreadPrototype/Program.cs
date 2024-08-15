using System;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Threading;
using ThreadPrototype.Components;
using System.Threading;
using System.Security.Cryptography;

namespace Program
{
    public class Program
    {
        public static void Main()
        {
            //-------------ESP32S3 part----------------
            int timeout = 10000;
            string channel = "", panid = "", networkkey = "";
            int retries = 10;
            ThreadPort threadPort = new ThreadPort("COM3");
            string output = string.Empty;
            //Check if dataset is already created
            try
            {
                threadPort.Write("dataset");
            }
            catch
            {
                Console.WriteLine("Test requires a powered ESP32-S3");
                Console.WriteLine("Check to make sure Serial Port COM is correct");
                return;
            }
            string channelInfo = threadPort.Read();
            //if not create a new dataset

            while (retries > 0)
            {
                //Get network details
                channel = ParseData(new Regex("Channel: (\\d+)"), channelInfo);
                panid = ParseData(new Regex(@"(?<!Ext\s)PAN ID:\s+(\S+)"), channelInfo);
                networkkey = ParseData(new Regex("Network Key: ([a-z0-9]+)"), channelInfo);
                if (String.IsNullOrEmpty(channel) || String.IsNullOrEmpty(panid) || String.IsNullOrEmpty(networkkey))
                {
                    threadPort.Write("dataset init new");
                    output = threadPort.Expect("Done", timeout).Result;
                    Thread.Sleep(2000);
                    threadPort.Write("dataset");
                    channelInfo = threadPort.Read();
                }
                else
                {

                    break;
                }

                retries--;
            }
            if (retries == 0)
            {
                Console.WriteLine($"{channelInfo} is in invalid state");
                return;
            }

            //Checks if already leader
            threadPort.Write("state");
            if (!threadPort.Read().Contains("leader"))
            {
                //dataset commit active
                threadPort.Write("dataset commit active");
                output = threadPort.Expect("Done", timeout).Result;
                //ifconfig up 
                threadPort.Write("ifconfig up");
                output = threadPort.Expect("Done", timeout).Result;
                //check if thread started
                threadPort.Write("thread start");
                //replaced done with port since it is one of the last words
                output = threadPort.Expect("Done", timeout).Result;
                //wait, then check if state is leader 
                threadPort.Write("state");
                retries = 10;
                while (!threadPort.Read().Contains("leader"))
                {
                    Thread.Sleep(10000);
                    threadPort.Write("state");
                    output = threadPort.Read();
                    retries--;
                }
            }
            //------------Thread device set up----------
            //Check if in warehouse mode
            int retryAttempts = 0;
            WarehousePort unit = new WarehousePort("https://192.168.1.1/cgi-bin/warehouse_api");
            output = unit.SendRequest("get_mode");
            while (!output.Contains("Warehouse"))
            {
                output = unit.SendRequest("get_mode");
                retryAttempts++;
                if (retryAttempts >= 3)
                {
                    Console.WriteLine($"Fail to get mode within {retryAttempts} attempts");
                    Console.WriteLine($"Incorrect output {output}, Network is not in connected to SCP network, please verify then rerun");
                    return;
                }
            }
            //Reset thread network
            output = unit.SendRequest("reset_thread");
            ////Verify thread network is reset
            output = unit.SendRequest("get_thread_status");
            //Set Thread network credentials - Check to see if return contains setting details
            output = unit.SendRequest($"set_thread_credential&channel={channel}&panid={panid}&networkkey={networkkey}");
            output = unit.SendRequest("get_thread_credential");
            //Enable Thread network
            output = unit.SendRequest("enable_thread_network");
            //Query Thread Network status- must be child 
            output = unit.SendRequest("get_thread_status");
            retryAttempts = 0;
            while (!output.Contains("child") && !output.Contains("router"))
            {
                retryAttempts++;
                Thread.Sleep(10000);
                output = unit.SendRequest("get_thread_status");
                if (retryAttempts >= 5)
                {
                    Console.WriteLine($"Fail to get thread status within {retryAttempts} attempts");
                    Console.WriteLine($"Incorrect output: {output}, please verify and rerun");
                    return;
                }
            }
            //Get thread IPv6 address
            string ipv6 = unit.SendRequest("get_thread_address").Split('\n')[1].ToString();

            //------------ESP32S3 ping part-------------
            //ping 10 times

            int successfulPings = RunPing(threadPort, ipv6, 10);
            Console.WriteLine($"Num of successful pings: {successfulPings}");

        }

        private static string ParseData(Regex regex, string data)
        {
            var match = regex.Match(data);
            return match.Groups[1].Value;
        }

        private static int RunPing(ThreadPort tp, string ipv6, int numPings)
        {
            int successfulPings = 0;

            Action<int> ping = (i) =>
            {
                string output = GetPing(tp, ipv6);

                if (!output.Contains("Failed"))
                {
                    Interlocked.Increment(ref successfulPings);
                }
                else
                {
                    Console.WriteLine($"Ping {i} Failed");
                }
            };

            Parallel.For(0, numPings, ping);

            return successfulPings;
        }

        private static string GetPing(ThreadPort tp, string ipv6)
        {
            tp.Write($"ping {ipv6}");
            return tp.Read();
        }
    }
}
