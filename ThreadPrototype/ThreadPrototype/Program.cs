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
            int timeout = 15000;
            string channelInfo = string.Empty;
            string channel, panid, networkkey;
            int retries = 10;
            ThreadPort threadPort = new ThreadPort("COM3");
            string output = string.Empty;
            //Check if dataset is already created
            threadPort.WriteLine("dataset");
            output = threadPort.Read();
            //if not create a new dataset
            while(retries > 0)
            {
                if (!output.Contains("Active"))
                {
                    threadPort.WriteLine("dataset init new");
                    output = threadPort.Expect("Done", timeout).Result;
                    retries--;
                    Thread.Sleep(2000);
                    threadPort.WriteLine("dataset");
                    output = threadPort.Read();
                }
                else
                {
                    break;
                }
            }
            channelInfo = output;
            //Get network details
            channel = ParseData(new Regex("Channel: (\\d+)"), channelInfo);
            panid = ParseData(new Regex(@"PAN ID: (\d\w+)"), channelInfo);
            networkkey = ParseData(new Regex("Network Key: (\\d\\w+)"), channelInfo);
            //dataset commit active
            threadPort.WriteLine("dataset commit active");
            output = threadPort.Expect("Done", timeout).Result;

            //ifconfig up 
            threadPort.WriteLine("ifconfig up");
            output = threadPort.Expect("Done", timeout).Result;
            //check if thread started
            threadPort.WriteLine("thread start");
            output =threadPort.Read();
            //wait until state is leader
            Thread.Sleep(30000);
            threadPort.WriteLine("state");
            output = threadPort.Read();
            retries = 10;
            while (!output.Contains("leader"))
            {
                Thread.Sleep(10000);
                threadPort.WriteLine("state");
                output = threadPort.Read();
                retries--;
            }

            //------------Thread device set up----------
            //Check if in warehouse mode
            WarehousePort unit = new WarehousePort("https://192.168.1.1/cgi-bin/warehouse_api");
            output = unit.SendRequest("get_mode");
            string ipv6 = "";
            if (output.Contains("Warehouse"))
            {
                //Reset thread network
                output = unit.SendRequest("reset_thread");
                //Verify thread network is reset
                output = unit.SendRequest("get_thread_status");
                //Set Thread network credentials - Check to see if return contains setting details
                output = unit.SendRequest($"set_thread_credential&channel={channel}&panid={panid}&networkkey={networkkey}");
                output = unit.SendRequest("get_thread_credential");
                //Enable Thread network
                output = unit.SendRequest("enable_thread_network");
                //Give around 30-45 sec to complete thread network setup
                Thread.Sleep(45000);
                //Query Thread Network status- must be child
                output = unit.SendRequest("get_thread_status");
                //Get thread IPv6 address
                output = unit.SendRequest("get_thread_address");
                ipv6 = output.Split('\n')[1].ToString();
            }
            else
            {
                Console.WriteLine("Network is not in connected to SCP network, please verify");
            }

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
                    Console.WriteLine("Ping Failed");
                }
            };

            Parallel.For(0, numPings, ping);

            return successfulPings;
        }

        private static string GetPing(ThreadPort tp, string ipv6)
        {
            tp.WriteLine($"ping {ipv6}");
            return tp.Read();
        }
    }
}
