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
        
        [DataMember(Name = "executionCriteria")]
        public string ExecutionCriteria { get; set; }

        [DataMember(Name = "target")]
        public EntityReference Target { get; set; }

        [DataMember(Name = "targetEntity")]
        public Entity TargetEntity { get; set; }

        [DataMember(Name = "targetColumns")]
        public string[] TargetColumns { get; set; }

        [DataMember(Name = "triggerUpdate")]
        public bool TriggerUpdate { get; set; }

        [DataMember(Name = "forceUpdate")]
        public bool ForceUpdate { get; set; }

        [DataMember(Name = "throwOnCustomActionError")]
        public bool ThrowOnCustomActionError { get; set; }

        [DataMember(Name = "organizationUrl")]
        public string OrganizationUrl { get; set; }

        public static ProcessorConfig Parse (string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

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
