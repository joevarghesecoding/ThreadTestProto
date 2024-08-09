using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace ThreadPrototype.Components
{
    public class ThreadPort
    {
        private SerialPort _serialPort;
        Int32 _baudrate = 9600;

        public ThreadPort(string com)
        {
            _serialPort = new SerialPort(com);
            _serialPort.BaudRate = _baudrate;
            _serialPort.ReadTimeout = 5000;
            _serialPort.WriteTimeout = 5000;
        }

        public void WriteLine(string message)
        {
            if(!_serialPort.IsOpen)
                _serialPort.Open();
            _serialPort.Write(message + '\n');

        }

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

        public async Task<string>? Expect(string message, int timeout)
        {
            string result = string.Empty;
            Task<string> readTask = Task.Run(() =>
            {
                while (true)
                {
                    string res = Read();
                    if (res.Contains(message))
                    {
                        return res;
                    }
                }
            });

            Task complete = await Task.WhenAny(readTask, Task.Delay(timeout));
            if(complete == readTask)
            {
               result = await readTask;
            }
            else
            {
                Console.WriteLine($"{message} not detected within {timeout} ms");
            }

            return result;
        }

    }
}
