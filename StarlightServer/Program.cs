using System;
using System.Collections.Generic;
using System.IO;
using StarlightNetwork;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace StarlightServer
{
    class Program
    {
        static Dictionary<int, Server> m_games = new Dictionary<int, Server>();

        static void Main(string[] args)
        {
            Console.WriteLine("Starting server...");

            // Start a server on port 5001
            RESTServer server = new RESTServer(new[] { "http://localhost:5001/" });

            // Add a POST handler for /Servers endpoint (create new server)
            server.AddRequestHandler("POST:/Servers", (HttpListenerRequest request, HttpListenerResponse response) =>
            {
                Console.WriteLine("New game request");
                JsonSerializer serializer = new JsonSerializer();

                // Get state from request
                StreamReader streamReader = new StreamReader(request.InputStream);
                GameState state = (GameState)serializer.Deserialize(streamReader, typeof(GameState));

                // Get new id for server
                int id = 0;
                lock (m_games)
                {
                    while (true)
                    {
                        if (m_games.ContainsKey(id))
                        {
                            id++;
                        }
                        else
                        {
                            Console.WriteLine("Using ID {0}", id);
                            m_games.Add(id, new Server(server, state, id));
                            break;
                        }
                    }
                }

                // send ID as response
                Console.WriteLine("Sending response to client - 200 OK");
                StreamWriter streamWriter = new StreamWriter(response.OutputStream);
                serializer.Serialize(streamWriter, id);
                streamWriter.Flush();
                response.StatusCode = 200;
            });
        }
    }
}
