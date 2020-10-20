using System;
using System.Collections.Generic;
using System.Text;

namespace Xrm.Oss.XTL.Interpreter
{
    public class ValueExpression
    {
        private Lazy<string> _text;
        private Lazy<object> _value;

        public string Text
        {
            get
            {
                return _text.Value;
            }
        }

        public object Value
        {
            get
            {
                return _value.Value;
            }
        }

        public ValueExpression(string text, object value)
        {
            _text = new Lazy<string>(() => text);
            _value = new Lazy<object>(() => value);
        }

        public ValueExpression(Lazy<ValueExpression> expression)
        {
            _text = new Lazy<string>(() => expression?.Value?.Text);
            _value = new Lazy<object>(() => expression?.Value?.Value);
        }

        public ValueExpression(Func<List<ValueExpression>, ValueExpression> expression, Dictionary<string, ValueExpression> args)
        {
            _text = new Lazy<string>(() => string.Empty);
            _value = new Lazy<object>(() => expression);
        }
    }
}
