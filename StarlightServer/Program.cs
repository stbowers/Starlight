using System;
using System.IO;
using StarlightNetwork;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace StarlightServer
{
    class Program
    {
        class Book
        {
            public int ID;
            public string Title;
            public string Description;
            public int PageCount;
            public string Excerpt;
            public string PublishDate;
        }

        [Serializable]
        class TestJSONObject : ISerializable
        {
            public string Name;
            private int m_num;

            public TestJSONObject(string name, int num)
            {
                Name = name;
                m_num = num;
            }

            public TestJSONObject(SerializationInfo serializationInfo, StreamingContext streamingContext)
            {
                Name = serializationInfo.GetString("NameValue");
                m_num = (int)serializationInfo.GetValue("Number", typeof(int));
            }

            public void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
            {
                serializationInfo.AddValue("NameValue", Name);
                serializationInfo.AddValue("Number", m_num);
            }
        }
        static void Main(string[] args)
        {
            TestJSONObject testobj = new TestJSONObject("Test", 5);
            string test = JsonHelpers.SerializeToString(testobj);
            Console.WriteLine(test);

            RESTServer server = new RESTServer(new[] { "http://localhost:25565/" });
            server.AddRequestHandler("GET:/Servers", (HttpListenerRequest request, HttpListenerResponse response) =>
            {
                StreamWriter streamWriter = new StreamWriter(response.OutputStream);
                streamWriter.Write(JsonHelpers.SerializeToString(testobj));
                streamWriter.Flush();
            });

            Task<TestJSONObject> testobjdl = RESTHelpers.GetJSONObjectAsync<TestJSONObject>("http://localhost:25565/Servers");
            testobjdl.Wait();
            Console.WriteLine(testobjdl.Result.Name);
        }
    }
}
