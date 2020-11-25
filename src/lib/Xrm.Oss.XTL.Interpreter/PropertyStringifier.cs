using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using Microsoft.Xrm.Sdk.Messages;
using System.Linq;
using Microsoft.Xrm.Sdk.Metadata;

namespace Xrm.Oss.XTL.Interpreter
{
    public static class PropertyStringifier
    {
        private static string StringifyProperty(string field, object value, Entity record, IOrganizationService service, ConfigHandler config = null)
        {
            var entityReference = value as EntityReference;
            var optionSet = value as OptionSetValue;
            var money = value as Money;
            var aliasedValue = value as AliasedValue;

            if (optionSet != null)
            {
                var textValue = optionSet.Value.ToString(CultureInfo.InvariantCulture);

                if (config == null)
                {
                    return textValue;
                }

                var configLanguage = config.GetValue<int>("optionSetLcid", "optionSetLcid must be an int!");

                if (configLanguage == 0)
                {
                    return textValue;
                }

                var request = new RetrieveAttributeRequest
                {
                    EntityLogicalName = record.LogicalName,
                    RetrieveAsIfPublished = true,
                    LogicalName = field
                };

                var response = service.Execute(request) as RetrieveAttributeResponse;
                var metadata = (EnumAttributeMetadata)response.AttributeMetadata;

                var fieldMetadata = metadata.OptionSet.Options.First(f => f.Value == optionSet.Value);

                var label = fieldMetadata.Label.LocalizedLabels.FirstOrDefault(l => l.LanguageCode == configLanguage)?.Label;

                if (label != null)
                {
                    return label;
                }

                return fieldMetadata.Label.UserLocalizedLabel.Label;
            }

            if (record.FormattedValues.ContainsKey(field))
            {
                return record.FormattedValues[field];
            }

            if (entityReference != null)
            {
                return entityReference.Name ?? entityReference.Id.ToString();
            }

            if (money != null)
            {
                return money.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (aliasedValue != null)
            {
                return StringifyProperty(field, aliasedValue.Value, record, service, config);
            }

            return value.ToString();
        }

        public static string Stringify(string field, Entity record, IOrganizationService service, ConfigHandler config = null)
        {
            var value = record.GetAttributeValue<object>(field);

            if (value == null)
            {
                return null;
            }

            return StringifyProperty(field, value, record, service, config);
        }
    }
}
