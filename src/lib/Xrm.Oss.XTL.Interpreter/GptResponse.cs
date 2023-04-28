using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace Xrm.Oss.XTL.Interpreter
{
    [DataContract]
    public class Choice
    {
        [DataMember(Name = "text")]
        public string Text { get; set; }

        [DataMember(Name = "index")]
        public int Index { get; set; }

        [DataMember(Name = "logprobs")]
        public object LogProbs { get; set; }

        [DataMember(Name = "finish_reason")]
        public string FinishReason { get; set; }
    }

    [DataContract]
    public class GptResponse
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "object")]
        public string @Object { get; set; }

        [DataMember(Name = "created")]
        public int Created { get; set; }

        [DataMember(Name = "model")]
        public string Model { get; set; }

        [DataMember(Name = "choices")]
        public List<Choice> Choices { get; set; }

        [DataMember(Name = "usage")]
        public Usage Usage { get; set; }
    }

    [DataContract]
    public class Usage
    {
        [DataMember(Name = "prompt_tokens")]
        public int PromptTokens { get; set; }

        [DataMember(Name = "completion_tokens")]
        public int CompletionTokens { get; set; }

        [DataMember(Name = "total_tokens")]
        public int TotalTokens { get; set; }
    }
}