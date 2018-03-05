using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Xrm.Oss.XTL.Templating;

namespace Xrm.Oss.XTL.Interpreter
{
    public class XTLInterpreter
    {
        private StringReader _reader = null;
        private char _previous;
        private char _current;
        private bool _eof;

        private Entity _primary;
        private IOrganizationService _service;
        private ITracingService _tracing;
        private OrganizationConfig _organizationConfig;

        public delegate List<object> FunctionHandler(Entity primary, IOrganizationService service, ITracingService tracing, OrganizationConfig organizationConfig, List<object> parameters);

        private Dictionary<string, FunctionHandler> _handlers = new Dictionary<string, FunctionHandler>
        {
            { "If", FunctionHandlers.If },
            { "Or", FunctionHandlers.Or },
            { "And", FunctionHandlers.And },
            { "Not", FunctionHandlers.Not },
            { "IsNull", FunctionHandlers.IsNull },
            { "IsEqual", FunctionHandlers.IsEqual },
            { "Value", FunctionHandlers.GetValue },
            { "Text", FunctionHandlers.GetText },
            { "RecordUrl", FunctionHandlers.GetRecordUrl },
            { "Fetch", FunctionHandlers.Fetch },
            { "RecordTable", FunctionHandlers.RenderRecordTable },
            { "PrimaryRecord", FunctionHandlers.GetPrimaryRecord },
            { "First", FunctionHandlers.First },
            { "Last", FunctionHandlers.Last}
        };

        public XTLInterpreter(string input, Entity primary, OrganizationConfig organizationConfig, IOrganizationService service, ITracingService tracing)
        {
            _primary = primary;
            _service = service;
            _tracing = tracing;
            _organizationConfig = organizationConfig;

            _reader = new StringReader(input);
            GetChar();
            SkipWhiteSpace();
        }

        /// <summary>
        /// Reads the next character and sets it as current. Old current char becomes previous.
        /// </summary>
        /// <returns>True if read succeeded, false if end of input</returns>
        private void GetChar()
        {
            _previous = _current;
            var character = _reader.Read();
            _current = (char)character;
            
            if (character == -1)
            {
                _eof = true;
            }
        }

        private void Expected(string expected)
        {
            throw new InvalidPluginExecutionException($"{expected} Expected after {_previous}{_current}");
        }

        private void SkipWhiteSpace() 
        {
            while(char.IsWhiteSpace(_current) && !_eof) {
                GetChar();
            }
        }

        private void Match (char c) 
        {
            if (_current != c) 
            {
                Expected(c.ToString());
            }

            GetChar();
            SkipWhiteSpace();
        }

        private string GetName() 
        {
            if (!char.IsLetter(_current)) {
                Expected("Name");
            }

            var name = string.Empty;

            while (char.IsLetterOrDigit(_current) && !_eof) {
                name += _current;
                GetChar();
            }

            SkipWhiteSpace();
            return name;
        }

        private List<object> Expression()
        {
            var returnValue = new List<object>();

            do
            {
                if (_current == ',') {
                    GetChar();
                }
                else if (_current == '"')
                {
                    var stringConstant = string.Empty;
                    
                    // Skip opening quote
                    GetChar();

                    // Allow to escape quotes by backslashes
                    while ((_current != '"' || _previous == '\\') && !_eof)
                    {
                        stringConstant += _current;
                        GetChar();
                    }

                    // Skip closing quote
                    GetChar();
                    returnValue.Add(stringConstant);
                }
                else if (char.IsDigit(_current))
                {
                    var digit = 0;

                    do
                    {
                        digit = digit * 10 + int.Parse(_current.ToString());
                        GetChar();
                    } while (char.IsDigit(_current) && !_eof);

                    returnValue.Add(digit);
                }
                else if (_current == ')')
                {
                    // Parameterless function encountered
                }
                // The first char of a function must not be a digit
                else
                {
                    returnValue.AddRange(Formula());
                }

                SkipWhiteSpace();
            } while (_current != ')');

            return returnValue;
        }

        private List<object> ApplyExpression (string name, List<object> parameters) 
        {
            if (!_handlers.ContainsKey(name)) {
                throw new InvalidPluginExecutionException($"Function {name} is not known!");
            }

            _tracing.Trace($"Processing handler {name}");
            var result = _handlers[name](_primary, _service, _tracing, _organizationConfig, parameters);
            _tracing.Trace($"Successfully processed handler {name}");

            return result;
        }

        private List<object> Formula()
        {
            var name = GetName();

            switch(name)
            {
                case "true":
                    return new List<object>() { true };
                case "false":
                    return new List<object>() { false };
                case "null":
                    return new List<object>() { null };
                default:
                    Match('(');
                    var parameters = Expression();
                    Match(')');

                    return ApplyExpression(name, parameters);
            }

            
        }

        public string Produce() 
        {
            _tracing.Trace($"Initiating interpreter");
            var output = Formula();
            _tracing.Trace("All done");

            if (output.Any(item => !(item is string)))
            {
                return string.Empty;
            }

            if (output != null && output.Count > 0) {
                return string.Join(Environment.NewLine, output.Select(item => item as string));
            }

            return string.Empty;
        }
    }
}