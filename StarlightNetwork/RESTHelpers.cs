using System;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace StarlightNetwork
{
    public static class RESTHelpers
    {
        static HttpClient client = new HttpClient();

        public static async Task<HttpResponseMessage> GetAsync(string uri)
        {
            HttpRequestMessage request = new HttpRequestMessage();
            request.Method = HttpMethod.Get;
            request.RequestUri = new Uri(uri);

            HttpResponseMessage response = await client.SendAsync(request);
            return response;
        }

        public static async Task<HttpResponseMessage> PostAsync(string uri, HttpContent content)
        {
            HttpRequestMessage request = new HttpRequestMessage();
            request.Method = HttpMethod.Post;
            request.RequestUri = new Uri(uri);
            request.Content = content;

            HttpResponseMessage response = await client.SendAsync(request);
            return response;
        }

        public static async Task<string> GetStringAsync(string uri)
        {
            HttpResponseMessage message = await GetAsync(uri);

            if ((int)message.StatusCode != 200)
            {
                throw new HttpRequestException();
            }
            else
            {
                return await message.Content.ReadAsStringAsync();
            }
        }

        public static async Task<HttpResponseMessage> PostStringAsync(string uri, string content, Encoding encoding, string mediaType)
        {
            StringContent httpContent = new StringContent(content, encoding, mediaType);
            HttpResponseMessage response = await PostAsync(uri, httpContent);
            return response;
        }

        public static async Task<T> GetJSONObjectAsync<T>(string uri)
        {
            string json = await GetStringAsync(uri);
            return JsonHelpers.CreateFromJsonString<T>(json);
        }

        public static async Task<HttpResponseMessage> PostJSONObjectAsync<T>(string uri, T data)
        {
            return await PostStringAsync(uri, JsonHelpers.SerializeToString(data), Encoding.UTF8, "application/json");
        }
    }

    public class RESTServer
    {
        HttpListener m_listener;
        Thread m_dispatchThread;
        Dictionary<string, RequestHandler> m_requestHandlers = new Dictionary<string, RequestHandler>();

        public RESTServer(string[] prefixes)
        {
            m_listener = new HttpListener();
            foreach (string prefix in prefixes)
            {
                m_listener.Prefixes.Add(prefix);
            }

            m_listener.Start();

            m_dispatchThread = new Thread(() =>
            {
                while (true)
                {
                    // Wait for request
                    HttpListenerContext context = m_listener.GetContext();

                    // Spawn new thread to handle request
                    ThreadPool.QueueUserWorkItem((object o) =>
                    {
                        // Get request and response objects from context
                        HttpListenerRequest request = context.Request;
                        HttpListenerResponse response = context.Response;

                        string requestHandle = string.Format("{0}:{1}", request.HttpMethod, request.Url.LocalPath);

                        if (m_requestHandlers.ContainsKey(requestHandle))
                        {
                            m_requestHandlers[requestHandle].Invoke(request, response);
                        }
                        else
                        {
                            response.StatusCode = 404;
                        }

                        // Close response stream
                        response.OutputStream.Close();
                    });
                }
            });
            m_dispatchThread.Start();
        }

        public void AddRequestHandler(string requestHandle, RequestHandler handler)
        {
            m_requestHandlers.Add(requestHandle, handler);
        }

        // Delegates
        public delegate void RequestHandler(HttpListenerRequest request, HttpListenerResponse response);
    }
}