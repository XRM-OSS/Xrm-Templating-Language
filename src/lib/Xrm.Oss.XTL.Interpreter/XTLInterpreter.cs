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

        private Entity _primary;
        private IOrganizationService _service;
        private OrganizationConfig _organizationConfig;

        public delegate List<object> FunctionHandler(Entity primary, IOrganizationService service, OrganizationConfig organizationConfig, List<object> parameters);

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
            { "SubRecords", FunctionHandlers.GetSubRecords },
            { "PrimaryRecord", FunctionHandlers.GetPrimaryRecord }
        };

        public XTLInterpreter(string input, Entity primary, OrganizationConfig organizationConfig, IOrganizationService service)
        {
            _primary = primary;
            _service = service;
            _organizationConfig = organizationConfig;

            _reader = new StringReader(input);
            GetChar();
            SkipWhiteSpace();
        }

        private void GetChar()
        {
            _previous = _current;
            _current = (char)_reader.Read();
        }

        private void Expected(string expected)
        {
            throw new InvalidPluginExecutionException($"{expected} Expected after {_previous}{_current}");
        }

        private void SkipWhiteSpace() 
        {
            while(char.IsWhiteSpace(_current)) {
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

            while (char.IsLetterOrDigit(_current)) {
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
                    while (_current != '"' || _previous == '\\')
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
                    } while (char.IsDigit(_current));

                    returnValue.Add(digit);
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

            return _handlers[name](_primary, _service, _organizationConfig, parameters);
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
            var output = Formula();

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