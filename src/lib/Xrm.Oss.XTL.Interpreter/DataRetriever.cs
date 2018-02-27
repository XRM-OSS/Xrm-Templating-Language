using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Xrm.Oss.XTL.Interpreter
{
    public static class DataRetriever
    {
        public static object ResolveTokenValue(string token, Entity primary, IOrganizationService service)
        {
            var path = new Queue<string>(token.Split('.'));
            var currentEntity = primary;
            object value = null;

            do
            {
                var currentField = path.Dequeue();
                var currentObject = currentEntity.GetAttributeValue<object>(currentField);

                if (currentObject == null)
                {
                    return null;
                }

                var entityReference = currentObject as EntityReference;
                var nextField = path.Count > 0 ? path.Peek() : null;

                if (entityReference == null || (entityReference != null && nextField == null))
                {
                    value = currentObject;
                }
                else
                {
                    currentEntity = service.Retrieve(entityReference.LogicalName, entityReference.Id, new ColumnSet(nextField));
                }
            } while (path.Count > 0);

            return value;
        }

        public static string ResolveTokenText(string token, Entity primary, IOrganizationService service)
        {
            var path = new Queue<string>(token.Split('.'));
            var currentEntity = primary;
            string value = null;

            do
            {
                var currentField = path.Dequeue();
                var currentObject = currentEntity.GetAttributeValue<object>(currentField);

                if (currentObject == null)
                {
                    return null;
                }

                var entityReference = currentObject as EntityReference;
                var optionSet = currentObject as OptionSetValue;
                var money = currentObject as Money;

                if (entityReference != null)
                {
                    var nextField = path.Peek();
                    if (nextField != null)
                    {
                        currentEntity = service.Retrieve(entityReference.LogicalName, entityReference.Id, new ColumnSet(nextField));
                    }
                    else
                    {
                        value = currentEntity.FormattedValues.ContainsKey(currentField)
                        ? currentEntity.FormattedValues[currentField]
                        : entityReference.Name ?? entityReference.Id.ToString();
                    }
                }
                else if (optionSet != null)
                {
                    value = currentEntity.FormattedValues.ContainsKey(currentField)
                        ? currentEntity.FormattedValues[currentField]
                        : optionSet.Value.ToString();
                }
                else if (money != null)
                {
                    value = currentEntity.FormattedValues.ContainsKey(currentField)
                        ? currentEntity.FormattedValues[currentField]
                        : money.Value.ToString();
                }
                else
                {
                    value = currentObject.ToString();
                }
            } while (path.Count > 0);

            return value;
        }
    }
}
