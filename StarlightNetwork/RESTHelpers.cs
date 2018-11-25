using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace StarlightNetwork
{
    public class RESTResponse
    {
        string status;
        object data;
    }

    public static class RESTHelpers
    {
        static HttpClient client = new HttpClient();

        // /servers
        // GET: list of servers
        // POST: create new server
        // /servers/id
        // GET: server details
        // DELETE: remove server

        public static async RESTResponse GetAsync(string uri)
        {
            HttpRequestMessage request = new HttpRequestMessage();
            request.Method = HttpMethod.Get;
            request.RequestUri = new Uri(uri);

            HttpResponseMessage response = await client.SendAsync(request);
            Console.WriteLine("Response: {0} - {1}", response.StatusCode.GetHashCode(), response.StatusCode.ToString());

            Stream responseStream = await response.Content.ReadAsStreamAsync();

            return JsonHelpers.CreateFromJsonStream<RESTResponse>(responseStream);
        }

        public static void RegisterGetCallback(string uri, GetCallback getCallback)
        {
        }

        public delegate void GetCallback();
    }
}