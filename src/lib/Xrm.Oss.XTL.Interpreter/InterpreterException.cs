using System;
namespace Xrm.Oss.XTL.Interpreter
{
    [Serializable]
    public class InterpreterException : Exception
    {
        public InterpreterException(string message) : base(message) { }
    }
}
