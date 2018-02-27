using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xrm.Sdk;

namespace Xrm.Oss.XTL.Interpreter
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
            { "If", If },
            { "Or", Or },
            { "And", And },
            { "Not", Not },
            { "IsNull", IsNull },
            { "IsEqual", IsEqual },
            { "Value", GetValue },
            { "Text", GetText },
            { "RecordUrl", GetRecordUrl },
            { "PrimaryRecord", GetPrimaryRecord }
        };

        private static Func<Entity, IOrganizationService, List<object>, List<object>> Not = (primary, service, parameters) =>
        {
            var target = parameters.FirstOrDefault();

            if (!(target is bool))
            {
                throw new InterpreterException("Not expects a boolean input, consider using one of the Is methods");
            }

            return new List<object> { !((bool) target) };
        };

        private static Func<Entity, IOrganizationService, List<object>, List<object>> IsEqual = (primary, service, parameters) =>
        {
            if (parameters.Count != 2)
            {
                throw new InterpreterException("IsEqual expects exactly 2 parameters!");
            }

            var expected = parameters[0];
            var actual = parameters[1];

            var falseReturn = new List<object> { false };
            var trueReturn = new List<object> { true };

            if (expected == null && actual == null)
            {
                return trueReturn;
            }

            if (expected == null && actual != null)
            {
                return falseReturn;
            }

            if (expected != null && actual == null)
            {
                return falseReturn;
            }

            // TODO: Compare Logic per CRM Type
            return trueReturn;
        };

        private static Func<Entity, IOrganizationService, List<object>, List<object>> And = (primary, service, parameters) =>
        {
            if (parameters.Count != 2)
            {
                throw new InterpreterException("And expects at least 2 conditions!");
            }

            if (parameters.Any(p => !(p is bool)))
            {
                throw new InterpreterException("And: All conditions must be booleans!");
            }

            if (parameters.All(p => (bool) p))
            {
                return new List<object> { true };
            }

            return new List<object> { false };
        };

        private static Func<Entity, IOrganizationService, List<object>, List<object>> Or = (primary, service, parameters) =>
        {
            if (parameters.Count != 2)
            {
                throw new InterpreterException("Or expects at least 2 conditions!");
            }

            if (parameters.Any(p => !(p is bool)))
            {
                throw new InterpreterException("Or: All conditions must be booleans!");
            }

            if (parameters.Any(p => (bool)p))
            {
                return new List<object> { true };
            }

            return new List<object> { false };
        };

        private static Func<Entity, IOrganizationService, List<object>, List<object>> IsNull = (primary, service, parameters) =>
        {
            var target = parameters.FirstOrDefault();

            if (target == null)
            {
                return new List<object> { true };
            }

            return new List<object> { false };
        };

        private static Func<Entity, IOrganizationService, List<object>, List<object>> If = (primary, service, parameters) =>
        {
            if (parameters.Count != 3)
            {
                throw new InterpreterException("If-Then-Else expects exactly three parameters: Condition, True-Action, False-Action");
            }

            var condition = parameters[0];
            var trueAction = parameters[1];
            var falseAction = parameters[2];

            if (!(condition is bool))
            {
                throw new InterpreterException("If condition must be a boolean!");
            }

            if ((bool) condition)
            {
                return new List<object> { trueAction };
            }
            
            return new List<object> { falseAction };
        };

        private static Func<Entity, IOrganizationService, List<object>, List<object>> GetPrimaryRecord = (primary, service, parameters) =>
        {
            if (primary == null)
            {
                return null;
            }

            return new List<object> { primary.ToEntityReference() };
        };

        private static Func<Entity, IOrganizationService, List<object>, List<object>> GetRecordUrl = (primary, service, parameters) =>
        {
            if (primary == null)
            {
                return null;
            }

            throw new NotImplementedException();
        };

        private static Func<Entity, IOrganizationService, List<object>, List<object>> GetText = (primary, service, parameters) =>
        {
            if (primary == null)
            {
                return null;
            }

            var field = parameters.FirstOrDefault() as string;

            if (field == null)
            {
                throw new InterpreterException("Text requires a field target string as input");
            }

            return new List<object> { DataRetriever.ResolveTokenText(field, primary, service) };
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
                throw new InterpreterException("Value requires a field target string as input");
            }

            return new List<object> { DataRetriever.ResolveTokenValue(field, primary, service) };
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
                throw new InterpreterException($"Function {name} is not known!");
            }

            return _handlers[name](_primary, _service, parameters);
        }

        private List<object> Formula()
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