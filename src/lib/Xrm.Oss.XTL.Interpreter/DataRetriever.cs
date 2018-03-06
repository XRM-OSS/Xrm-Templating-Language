using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Xrm.Oss.XTL.Interpreter
{
    public static class DataRetriever
    {
        public static ValueExpression ResolveTokenValue(string token, Entity primary, IOrganizationService service)
        {
            var path = new Queue<string>(token.Split('.'));
            var currentEntity = primary;
            ValueExpression value = null;

            do
            {
                var currentField = path.Dequeue();
                var currentObject = currentEntity.GetAttributeValue<object>(currentField);

                if (currentObject == null)
                {
                    return new ValueExpression(null);
                }

                var entityReference = currentObject as EntityReference;
                var nextField = path.Count > 0 ? path.Peek() : null;

                if (entityReference == null || (entityReference != null && nextField == null))
                {
                    value = new ValueExpression(PropertyStringifier.Stringify(currentEntity, currentField), currentObject);
                }
                else
                {
                    currentEntity = service.Retrieve(entityReference.LogicalName, entityReference.Id, new ColumnSet(nextField));
                }
            } while (path.Count > 0);

            return value;
        }
    }
}
