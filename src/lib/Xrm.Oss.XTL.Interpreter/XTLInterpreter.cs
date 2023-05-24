using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Xrm.Sdk;

namespace Xrm.Oss.XTL.Interpreter
{
    public class XTLInterpreter
    {
        private StringReader _reader = null;
        private int _position;
        private string _input;
        private char _previous;
        private char _current;

        private Entity _primary;
        private IOrganizationService _service;
        private ITracingService _tracing;
        private OrganizationConfig _organizationConfig;

        public delegate ValueExpression FunctionHandler(Entity primary, IOrganizationService service, ITracingService tracing, OrganizationConfig organizationConfig, List<ValueExpression> parameters);

        private Dictionary<string, FunctionHandler> _handlers = new Dictionary<string, FunctionHandler>
        {
            { "And", FunctionHandlers.And },
            { "Array", FunctionHandlers.Array },
            { "Case", FunctionHandlers.Case },
            { "Coalesce", FunctionHandlers.Coalesce },
            { "Concat", FunctionHandlers.Concat },
            { "ConvertDateTime", FunctionHandlers.ConvertDateTime },
            { "DateTimeNow", FunctionHandlers.DateTimeNow },
            { "DateTimeUtcNow", FunctionHandlers.DateTimeUtcNow },
            { "DateToString", FunctionHandlers.DateToString },
            { "Fetch", FunctionHandlers.Fetch },
            { "Filter", FunctionHandlers.Filter },
            { "First", FunctionHandlers.First },
            { "Format", FunctionHandlers.Format },
            { "GptPrompt", FunctionHandlers.GptPrompt },
            { "If", FunctionHandlers.If },
            { "IndexOf", FunctionHandlers.IndexOf },
            { "IsEqual", FunctionHandlers.IsEqual },
            { "IsGreater", FunctionHandlers.IsGreater },
            { "IsGreaterEqual", FunctionHandlers.IsGreaterEqual },
            { "IsLess", FunctionHandlers.IsLess },
            { "IsLessEqual", FunctionHandlers.IsLessEqual },
            { "IsNull", FunctionHandlers.IsNull },
            { "Join", FunctionHandlers.Join },
            { "Last", FunctionHandlers.Last},
            { "Length", FunctionHandlers.Length },
            { "Map", FunctionHandlers.Map },
            { "NewLine", FunctionHandlers.NewLine },
            { "Not", FunctionHandlers.Not },
            { "Or", FunctionHandlers.Or },
            { "OrganizationUrl", FunctionHandlers.GetOrganizationUrl },
            { "PrimaryRecord", FunctionHandlers.GetPrimaryRecord },
            { "RecordId", FunctionHandlers.GetRecordId },
            { "RecordLogicalName", FunctionHandlers.GetRecordLogicalName },
            { "RecordTable", FunctionHandlers.RenderRecordTable },
            { "RecordUrl", FunctionHandlers.GetRecordUrl },
            { "Replace", FunctionHandlers.Replace },
            { "RetrieveAudit", FunctionHandlers.RetrieveAudit },
            { "Snippet", FunctionHandlers.Snippet },
            { "Sort", FunctionHandlers.Sort },
            { "Static", FunctionHandlers.Static },
            { "Substring", FunctionHandlers.Substring },
            { "Union", FunctionHandlers.Union },
            { "Value", FunctionHandlers.GetValue }
        };

        public XTLInterpreter(string input, Entity primary, OrganizationConfig organizationConfig, IOrganizationService service, ITracingService tracing)
        {
            _primary = primary;
            _service = service;
            _tracing = tracing;
            _organizationConfig = organizationConfig;
            _input = input;
            _position = 0;

            _reader = new StringReader(input ?? string.Empty);
            GetChar();
            SkipWhiteSpace();
        }

        /// <summary>
        /// Reads the next character and sets it as current. Old current char becomes previous.
        /// </summary>
        /// <returns>True if read succeeded, false if end of input</returns>
        private void GetChar(int? index = null)
        {
            if (index != null)
            {
                // Initialize a new reader to move back to the beginning
                _reader = new StringReader(_input);
                _position = index.Value;

                // Skip to searched index
                for (var i = 0; i < index; i++)
                {
                    _reader.Read();
                }
            }

            _previous = _current;
            var character = _reader.Read();
            _current = (char)character;
            
            if (character != -1)
            {
                _position++;
            }
        }

        private void Expected(string expected)
        {
            throw new InvalidPluginExecutionException($"{expected} expected after '{_previous}' at position {_position}, but encountered '{_current}'");
        }

        private bool IsEof()
        {
            return _current == '\uffff';
        }

        private void SkipWhiteSpace() 
        {
            while(char.IsWhiteSpace(_current) && !IsEof()) {
                GetChar();
            }
        }

        private void Match (char c) 
        {
            if (_current != c) 
            {
                Expected(c.ToString(CultureInfo.InvariantCulture));
            }

            GetChar();
            SkipWhiteSpace();
        }

        private string GetName() 
        {
            SkipWhiteSpace();

            if (!char.IsLetter(_current)) {
                Expected($"Identifier");
            }

            var name = string.Empty;

            while (char.IsLetterOrDigit(_current) && !IsEof()) {
                name += _current;
                GetChar();
            }

            SkipWhiteSpace();
            return name;
        }

        private List<ValueExpression> Expression(char[] terminators, Dictionary<string, ValueExpression> formulaArgs)
        {
            var returnValue = new List<ValueExpression>();

            do
            {
                if (_current == ',') {
                    GetChar();
                }
                else if (_current == '"' || _current == '\'')
                {
                    var delimiter = _current;
                    var stringConstant = string.Empty;
                    
                    // Skip opening quote
                    GetChar();

                    // Allow to escape quotes by backslashes
                    while ((_current != delimiter || _previous == '\\') && !IsEof())
                    {
                        stringConstant += _current;
                        GetChar();
                    }

                    // Skip closing quote
                    GetChar();
                    returnValue.Add(new ValueExpression(stringConstant, stringConstant));
                }
                else if (char.IsDigit(_current) || _current == '-')
                {
                    var digit = 0;
                    var fractionalPart = 0;
                    var processingFractionalPart = false;

                    // Multiply by -1 for negative numbers
                    var multiplicator = 1;

                    do
                    {
                        if (_current == '-')
                        {
                            multiplicator = -1;
                        }
                        else if (_current != '.')
                        {
                            if (processingFractionalPart)
                            {
                                fractionalPart = fractionalPart * 10 + int.Parse(_current.ToString(CultureInfo.InvariantCulture));
                            }
                            else
                            {
                                digit = digit * 10 + int.Parse(_current.ToString(CultureInfo.InvariantCulture));
                            }
                        }
                        else
                        {
                            processingFractionalPart = true;
                        }

                        GetChar();
                    } while ((char.IsDigit(_current) || _current == '.') && !IsEof());

                    switch(_current)
                    {
                        case 'd':
                            double doubleValue = multiplicator * (digit + fractionalPart / Math.Pow(10, (fractionalPart.ToString(CultureInfo.InvariantCulture).Length)));
                            returnValue.Add(new ValueExpression(doubleValue.ToString(CultureInfo.InvariantCulture), doubleValue));
                            GetChar();
                            break;
                        case 'm':
                            decimal decimalValue = multiplicator * (digit + fractionalPart / (decimal) Math.Pow(10, (fractionalPart.ToString(CultureInfo.InvariantCulture).Length)));
                            returnValue.Add(new ValueExpression(decimalValue.ToString(CultureInfo.InvariantCulture), decimalValue));
                            GetChar();
                            break;
                        default:
                            if (processingFractionalPart)
                            {
                                throw new InvalidDataException("For defining numbers with fractional parts, please append d (for double) or m (for decimal) to your number");
                            }
                            var value = digit * multiplicator;

                            returnValue.Add(new ValueExpression(value.ToString(CultureInfo.InvariantCulture), value));
                            break;
                    }

                }
                else if (terminators.Contains(_current))
                {
                    // Parameterless function or empty array encountered
                }
                // The first char of a function must not be a digit
                else
                {
                    var value = Formula(formulaArgs);

                    if (value != null)
                    {
                        returnValue.Add(value);
                    }
                }

                SkipWhiteSpace();
            } while (!terminators.Contains(_current) && !IsEof());

            return returnValue;
        }

        private ValueExpression ApplyExpression (string name, List<ValueExpression> parameters, Dictionary<string, ValueExpression> formulaArgs = null) 
        {
            if (!_handlers.ContainsKey(name)) {
                throw new InvalidPluginExecutionException($"Function {name} is not known!");
            }

            // In this case we're only stepping through in the initial interpreting of the lambda
            if (formulaArgs != null && formulaArgs.Any(a => a.Value == null))
            {
                return new ValueExpression(null);
            }

            var lazyExecution = new Lazy<ValueExpression>(() =>
            {
                _tracing.Trace($"Processing handler {name}");
                var result = _handlers[name](_primary, _service, _tracing, _organizationConfig, parameters);
                _tracing.Trace($"Successfully processed handler {name}");

                return result;
            });

            return new ValueExpression(lazyExecution);
        }

        private ValueExpression Formula(Dictionary<string, ValueExpression> args)
        {
            SkipWhiteSpace();

            if (_current == '[') {
                Match('[');
                var arrayParameters = Expression(new[] { ']' }, args);
                Match(']');

                return ApplyExpression("Array", arrayParameters);
            }
            else if(_current == '(')
            {
                // Match arrow functions in style of (param) => Convert(param)
                Match('(');

                var variableNames = new List<string>();

                do
                {
                    SkipWhiteSpace();
                    variableNames.Add(GetName());
                    SkipWhiteSpace();

                    if (_current == ',')
                    {
                        GetChar();
                    }
                } while (_current != ')');

                // Initialize variables as null
                var formulaArgs = variableNames.ToDictionary(n => n, v => (ValueExpression) null);
                Match(')');

                var usedReservedWords = variableNames
                    .Where(n => new List<string> { "true", "false", "null" }.Concat(_handlers.Keys).Contains(n))
                    .ToList();

                if (usedReservedWords.Count > 0)
                {
                    throw new InvalidPluginExecutionException($"Your variable names {string.Join(", ", usedReservedWords.Select(w => $"'{w}'"))} is a reserved word, please choose a different name");
                }

                SkipWhiteSpace();
                Match('=');
                Match('>');
                SkipWhiteSpace();

                var lambdaPosition = this._position - 1;

                var lazyExecution = new Func<List<ValueExpression>, ValueExpression>((lambdaArgs) =>
                {
                    var currentIndex = this._position;
                    GetChar(lambdaPosition);

                    var arguments = formulaArgs.ToList();
                    for (var i = 0; i < lambdaArgs.Count; i++) {
                        if (i < formulaArgs.Count)
                        {
                            var parameterName = arguments[i].Key;
                            formulaArgs[parameterName] = lambdaArgs[i];
                        }
                    }

                    var result = Formula(formulaArgs);
                    GetChar(currentIndex - 1);

                    return result;
                });

                // Run only for skipping the formula part
                Formula(formulaArgs);

                return new ValueExpression(lazyExecution, formulaArgs);
            }
            else if (_current == '{')
            {
                Match('{');
                var dictionary = new Dictionary<string, object>();
                var firstRunPassed = false;

                do
                {
                    SkipWhiteSpace();

                    if (firstRunPassed)
                    {
                        Match(',');
                        SkipWhiteSpace();
                    }
                    else
                    {
                        firstRunPassed = true;
                    }

                    var name = GetName();
                    
                    SkipWhiteSpace();
                    Match(':');
                    SkipWhiteSpace();

                    dictionary[name] = Formula(args)?.Value;
                    
                    SkipWhiteSpace();
                } while (_current != '}');

                Match('}');

                return new ValueExpression(string.Join(", ", dictionary.Select(p => $"{p.Key}: {p.Value}")), dictionary);
            }
            else if (char.IsDigit(_current) || _current == '"' || _current == '\'' || _current == '-')
            {
                // This is only called in object initializers / dictionaries. Only one value should be entered here
                return Expression(new[] { '}', ',' }, args).First();
            }
            else {
                var name = GetName();

                if (args != null && args.ContainsKey(name))
                {
                    return args[name];
                }

                switch(name)
                {
                    case "true":
                        return new ValueExpression(bool.TrueString, true);
                    case "false":
                        return new ValueExpression(bool.FalseString, false);
                    case "null":
                        return new ValueExpression( null );
                    default:
                        Match('(');
                        var parameters = Expression(new[] { ')' }, args);
                        Match(')');

                        return ApplyExpression(name, parameters, args);
                }
            }
        }

        public string Produce() 
        {
            _tracing.Trace($"Initiating interpreter");

            if (string.IsNullOrWhiteSpace(_input)) {
                _tracing.Trace("No formula passed, exiting");
                return string.Empty;
            }

            var output = Formula(new Dictionary<string, ValueExpression> { });

            return output?.Text ?? string.Empty;
        }
    }
}
