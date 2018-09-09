using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace Xrm.Oss.XTL.Interpreter
{
    public static class PropertyStringifier
    {
        public static string Stringify(Entity record, string field)
        {
            var value = record.GetAttributeValue<object>(field);

            if (value == null)
            {
                return null;
            }

            var entityReference = value as EntityReference;
            var optionSet = value as OptionSetValue;
            var money = value as Money;

            if (entityReference != null)
            {
                return record.FormattedValues.ContainsKey(field)
                        ? record.FormattedValues[field]
                        : entityReference.Name ?? entityReference.Id.ToString();
            }

            if (optionSet != null)
            {
                return record.FormattedValues.ContainsKey(field)
                        ? record.FormattedValues[field]
                        : optionSet.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (money != null)
            {
                return record.FormattedValues.ContainsKey(field)
                        ? record.FormattedValues[field]
                        : money.Value.ToString(CultureInfo.InvariantCulture);
            }

            return value.ToString();
        }
    }
}
