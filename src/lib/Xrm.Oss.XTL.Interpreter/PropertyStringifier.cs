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
        public static string Stringify(string field, Entity record, IOrganizationService service, Dictionary<string, object> config = null)
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
                var textValue = optionSet.Value.ToString(CultureInfo.InvariantCulture);

                if (config == null)
                {
                    return textValue;
                }

                var configLanguage = config.ContainsKey("optionSetLcid") ? (int) config["optionSetLcid"] : 0;

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
                var metadata = (PicklistAttributeMetadata)response.AttributeMetadata;

                var fieldMetadata = metadata.OptionSet.Options.First(f => f.Value == optionSet.Value);

                var label = fieldMetadata.Label.LocalizedLabels.FirstOrDefault(l => l.LanguageCode == configLanguage)?.Label;

                if (label != null)
                {
                    return label;
                }

                return fieldMetadata.Label.UserLocalizedLabel.Label;
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
