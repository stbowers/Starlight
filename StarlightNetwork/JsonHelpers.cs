﻿using System;
using System.IO;
using Newtonsoft.Json;

namespace StarlightNetwork
{
    public static class JsonHelpers
    {
        public static T CreateFromJsonStream<T>(this Stream stream)

        {

            JsonSerializer serializer = new JsonSerializer();

            T data;

            using (StreamReader streamReader = new StreamReader(stream))

            {

                data = (T)serializer.Deserialize(streamReader, typeof(T));

            }

            return data;

        }

        public static T CreateFromJsonString<T>(this String json)

        {

            T data;

            using (MemoryStream stream = new MemoryStream(System.Text.Encoding.Default.GetBytes(json)))

            {

                data = CreateFromJsonStream<T>(stream);

            }

            return data;

        }

        public static T CreateFromJsonFile<T>(this String fileName)

        {

            T data;

            using (FileStream fileStream = new FileStream(fileName, FileMode.Open))

            {

                data = CreateFromJsonStream<T>(fileStream);

            }

            return data;

        }

        public static void Serialize(object value, Stream s)

        {

            StreamWriter writer = new StreamWriter(s);

            JsonTextWriter jsonWriter = new JsonTextWriter(writer);

            JsonSerializer ser = new JsonSerializer();

            ser.Serialize(jsonWriter, value);

            jsonWriter.Flush();

        }

        public static string SerializeToString(object value)
        {
            StringWriter stringWriter = new StringWriter();
            JsonSerializer serializer = new JsonSerializer();
            serializer.Serialize(stringWriter, value);
            stringWriter.Flush();
            return stringWriter.ToString();
        }
    }
}
