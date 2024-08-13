using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace ThreadPrototype.Components
{
    /// <summary>
    /// Includes all the functions and parameters relating to the serial port
    /// </summary>
    public class ThreadPort
    {
        private SerialPort _serialPort;
        Int32 _baudrate = 9600;

        public ThreadPort(string com)
        {
            _serialPort = new SerialPort(com);
            _serialPort.BaudRate = _baudrate;
            _serialPort.ReadTimeout = 1000000;
            _serialPort.WriteTimeout = 5000;
        }
        /// <summary>
        /// Writes to the serial port if it is open
        /// </summary>
        /// <param name="message"> The command written to serial</param>
        /// <returns></returns>
        public void WriteLine(string message)
        {
            if(!_serialPort.IsOpen)
                _serialPort.Open();
            _serialPort.WriteLine(message);

        }
        /// <summary>
        /// Gets the feedback from serial port from the write
        /// </summary>
        /// <returns> return response </returns>
        public string Read()
        {
            if(_serialPort.IsOpen)
            {
                byte[] buffer = new byte[4092];

                _serialPort.Read(buffer, 0, buffer.Length);
                string response = Encoding.UTF8.GetString(buffer);

                return response;
            }

            return string.Empty;
        }
        /// <summary>
        /// Reads the output until a string is found, or enough time has elapsed 
        /// </summary>
        /// <param name="message"> The string being searched</param>
        /// /// <param name="timeout"> The enough time in milliseconds</param>
        /// <returns> res and result </returns>
        public async Task<string> Expect(string message, int timeout)
        {
            string result = string.Empty;
            Task<string> readTask = Task.Run(() =>
            {
                string res = "";
                while (!String.IsNullOrEmpty(res = Read()))
                {
                    //string res = Read();
                    if (res.Contains(message))
                    {
                        return res;
                    }
                }
                return res;
            });

            Task complete = await Task.WhenAny(readTask, Task.Delay(timeout));
            if (complete == readTask)
            {
                result = await readTask;
            }
            else
            {
                Console.WriteLine($"{message} not detected within {timeout} ms");

            }
            Console.WriteLine(result);
            return result;

        }
          
    }
}
