using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Xrm.Oss.XTL.Interpreter
{
    public class ConfigHandler
    {
        public Dictionary<string, object> Dictionary { get; internal set; }

        public ConfigHandler(Dictionary<string, object> config)
        {
            Dictionary = config;
        }

        public bool Contains(string name)
        {
            return Dictionary.ContainsKey(name);
        }

        public bool IsSet(string name)
        {
            return Dictionary.ContainsKey(name) && Dictionary[name] != null;
        }

        public T GetValue<T>(string name, string typeErrorMessage, T defaultValue = default(T))
        {
            if (!Dictionary.ContainsKey(name))
            {
                return defaultValue;
            }

            var value = Dictionary[name];

            if (!(value is T))
            {
                throw new InvalidDataException(typeErrorMessage);
            }

            return (T)value;
        }
    }
}
