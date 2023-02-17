namespace Xrm.Oss.XTL.Interpreter
{
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
    }
}