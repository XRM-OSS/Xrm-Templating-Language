using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xrm.Sdk;

namespace Xrm.Oss.RecursiveDescentParser
{
    public class XTLInterpreter
    {
        private StringReader _reader = null;
        private char _previous;
        private char _current;

        private Entity _primary;
        private IOrganizationService _service;

        private Dictionary<string, Func<Entity, IOrganizationService, List<object>, List<object>>> _handlers = new Dictionary<string, Func<Entity, IOrganizationService, List<object>, List<object>>>
        {
            { "GetValue", GetValue }
        };

        private static Func<Entity, IOrganizationService, List<object>, List<object>> GetValue = (primary, service, parameters) =>
        {
            if (primary == null)
            {
                return null;
            }

            var field = parameters.FirstOrDefault() as string;

            if (field == null)
            {
                throw new InterpreterException("GetValue requires a field target string as input");
            }

            return new List<object> { primary.GetAttributeValue<string>(field) };
        };
                

        public XTLInterpreter(string input, Entity primary, IOrganizationService service) 
        {
            _primary = primary;
            _service = service;

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
            throw new InterpreterException($"{expected} Expected after {_previous}{_current}");
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
                    GetChar();

                    // Allow to escape quotes by backslashes
                    while (_current != '"' || _previous == '\\')
                    {
                        stringConstant += _current;
                        GetChar();
                    }

                    GetChar();
                    returnValue.Add(stringConstant);
                }
                else
                {
                    returnValue.Add(Formula());
                }

                SkipWhiteSpace();
            } while (_current != ')');

            return returnValue;
        }

        private List<object> ApplyExpression (string name, List<object> parameters) 
        {
            if (!_handlers.ContainsKey(name)) {
                throw new InterpreterException($"Function {name} is not known!");
            }

            return _handlers[name](_primary, _service, parameters);
        }

        private object Formula()
        {
            var name = GetName();

            Match('(');
            var parameters = Expression();
            Match(')');

            return ApplyExpression(name, parameters);
        }

        public string Produce() 
        {
            var output = Formula();
            var outputList = output as List<object>;

            if (outputList != null) {
                return string.Join(Environment.NewLine, outputList);
            }

            return output as string ?? string.Empty;
        }
    }
}