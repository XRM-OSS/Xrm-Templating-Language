using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Xrm.Oss.XTL.Templating
{
    [DataContract]
    public class ProcessingResult
    {
        [DataMember(Name = "success")]
        public bool Success { get; set; }

        [DataMember(Name = "traceLog")]
        public string TraceLog { get; set; }

        [DataMember(Name = "result")]
        public string Result { get; set; }

        [DataMember(Name = "error")]
        public string Error { get; set; }
    }
}
