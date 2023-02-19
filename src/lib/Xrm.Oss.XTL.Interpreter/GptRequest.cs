using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace Xrm.Oss.XTL.Interpreter
{
    [DataContract]
    public class GptRequest
    {
        [DataMember(Name = "model")]
        public string Model { get; set; }

        [DataMember(Name = "prompt")]
        public string Prompt { get; set; }

        [DataMember(Name = "temperature")]
        public int Temperature { get; set; }

        [DataMember(Name = "max_tokens")]
        public int MaxTokens { get; set; }

        [DataMember(Name = "stop")]
        public List<string> Stop { get; set; }
    }
}