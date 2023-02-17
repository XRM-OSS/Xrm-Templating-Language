using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Xrm.Oss.XTL.Interpreter
{
    [DataContract]
    public class OrganizationConfig : IConfig
    {
        public string Raw { get; set; }

        [DataMember(Name = "organizationUrl")]
        public string OrganizationUrl { get; set; }

        [DataMember(Name = "openAIAccessToken")]
        public string OpenAIAccessToken { get; set; }

        public static OrganizationConfig Parse(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var serializer = new DataContractJsonSerializer(typeof(OrganizationConfig));

                var config = serializer.ReadObject(memoryStream) as OrganizationConfig;
                config.Raw = json;

                return config;
            }
        }
    }
}
