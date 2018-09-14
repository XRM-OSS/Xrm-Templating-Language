using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Xrm.Oss.XTL.Interpreter
{
    public class ConfigHandler
    {
        private Dictionary<string, object> _config;

        public ConfigHandler(Dictionary<string, object> config)
        {
            _config = config;
        }

        public bool Contains(string name)
        {
            return _config.ContainsKey(name);
        }

        public bool IsSet(string name)
        {
            return _config.ContainsKey(name) && _config[name] != null;
        }

        public T GetValue<T>(string name, string typeErrorMessage, T defaultValue = default(T))
        {
            if (!_config.ContainsKey(name))
            {
                return defaultValue;
            }

            var value = _config[name];

            if (!(value is T))
            {
                throw new InvalidDataException(typeErrorMessage);
            }

            return (T)value;
        }
    }
}
