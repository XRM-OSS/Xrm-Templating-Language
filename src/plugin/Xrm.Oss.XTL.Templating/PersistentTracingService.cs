using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xrm.Oss.XTL.Templating
{
    public class PersistentTracingService : ITracingService
    {
        private StringBuilder _traceBuilder;
        private ITracingService _innerTracing;
        
        public string TraceLog
        {
            get
            {
                return _traceBuilder.ToString();
            }
        }

        public PersistentTracingService(ITracingService innerTracing)
        {
            _traceBuilder = new StringBuilder();
            _innerTracing = innerTracing;
        }

        public void Trace(string message, params object[] args)
        {
            _traceBuilder.AppendLine(message);
            _innerTracing.Trace("{0}", message);
        }
    }
}
