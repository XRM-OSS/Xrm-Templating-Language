using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Xrm.Oss.XTL.Templating
{
    [DataContract]
    public class ProcessorConfig : IConfig
    {
        public string Raw { get; set; }

        [DataMember(Name = "targetField")]
        public string TargetField { get; set; }

        [DataMember(Name = "templateField")]
        public string TemplateField { get; set; }

        [DataMember(Name = "template")] 
        public string Template { get; set; }
        

        public static ProcessorConfig Parse (string json)
        {
            using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var serializer = new DataContractJsonSerializer(typeof(ProcessorConfig));

                var config = serializer.ReadObject(memoryStream) as ProcessorConfig;
                config.Raw = json;

                return config;
            }
        }
    }
}
