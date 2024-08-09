using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreadPrototype.Components
{
    public class WarehousePort
    {
        private HttpClient client;
        private string _url;
        public WarehousePort(string url) 
        {
            _url = url;
            client = new HttpClient();          
        }

        /// <summary>
        /// Sends cUrl request and returns the response
        /// </summary>
        /// <param name="requestMessage"></param>
        /// <returns>returns response</returns>
        public async void SendRequest(string requestMessage)
        {
            if(client == null)
            {
                client = new HttpClient();
            }

            var content = new StringContent(requestMessage, Encoding.UTF8, "application/x-www-form-urlencoded");

            HttpResponseMessage response = await client.PostAsync(_url, content);

            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseBody);
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}");
            }

        }
    }
}
