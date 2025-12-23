using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using static Xrm.Oss.XTL.Interpreter.XTLInterpreter;

namespace Xrm.Oss.XTL.Interpreter
{
    [DataContract]
    public class SnippetConfig
    {
        [DataMember(Name = "tableLogicalName")]
        public string TableLogicalName { get; set; }

        [DataMember(Name = "searchColumnLogicalName")]
        public string SearchColumnLogicalName { get; set; }

        [DataMember(Name = "nameColumnLogicalName")]
        public string NameColumnLogicalName { get; set; }

        [DataMember(Name = "valueColumnLogicalName")]
        public string ValueColumnLogicalName { get; set; }
    }

    [DataContract]
    public class InterpreterConfig : IConfig
    {
        public string Raw { get; set; }

        [DataMember(Name = "organizationUrl")]
        public string OrganizationUrl { get; set; }

        [DataMember(Name = "openAIAccessToken")]
        public string OpenAIAccessToken { get; set; }

        [DataMember(Name = "snippetConfig")]
        public SnippetConfig SnippetConfig { get; set; }

        [DataMember(Name = "inputParameters")]
        public Dictionary<string, object> InputParameters { get; set; }

        public Dictionary<string, FunctionHandler> CustomHandlers { get; set; }

        public static InterpreterConfig Parse(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var serializer = new DataContractJsonSerializer(typeof(InterpreterConfig));

                var config = serializer.ReadObject(memoryStream) as InterpreterConfig;
                config.Raw = json;

                return config;
            }
        }
    }

    // Backward compatibility alias
    [Obsolete("Use InterpreterConfig instead")]
    public class OrganizationConfig : InterpreterConfig { }
}
