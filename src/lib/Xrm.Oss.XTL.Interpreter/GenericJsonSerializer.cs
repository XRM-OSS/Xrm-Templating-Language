using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Xrm.Oss.XTL.Interpreter;

namespace Xrm.Oss.XTL.Templating
{
    public class GenericJsonSerializer
    {
        public static string Serialize<T> (T input)
        {
            using (var memoryStream = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(T));

                var json = serializer.WriteObject(memoryStream, input);

                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }

        public static T Deserialize<T> (string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var serializer = new DataContractJsonSerializer(typeof(T));

                var output = serializer.ReadObject(memoryStream) as T;

                return output;
            }
        }
    }
}
