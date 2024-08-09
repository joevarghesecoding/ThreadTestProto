using System;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Threading;
using ThreadPrototype.Components;

namespace Program
{
    public class Program
    {
        public static void Main()
        {
            //-------------ESP32S3 part----------------
            int timeout = 5000;
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
            channel = ParseData(new Regex("Channel: (\\d+)"), channelInfo);
            panid = ParseData(new Regex("^PAN ID: (\\d\\w+)"), channelInfo);
            networkkey = ParseData(new Regex("^Network Key: (\\d\\w+)"), channelInfo);
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
            unit.SendRequest("command=get_mode");

            //Reset thread network

            //Verify thread network is reset

            //Set Thread network credentials - Check to see if return contains setting details

            //Enable Thread network

            //Give around 30-45 sec to complete thread network setup

            //Query Thread Network status- must be child







            //------------ESP32S3 ping part-------------
            //ping 


           
        }

        private static string ParseData(Regex regex, string data)
        {
            var match = regex.Match(data);
            return match.Groups[1].Value;
        }
    }
}
