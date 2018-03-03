using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using static Xrm.Oss.XTL.Interpreter.XTLInterpreter;

namespace Xrm.Oss.XTL.Interpreter
{
    public static class FunctionHandlers
    {
        public static FunctionHandler Not = (primary, service, organizationConfig, parameters) =>
        {
            var target = parameters.FirstOrDefault();

            if (!(target is bool))
            {
                throw new InvalidPluginExecutionException("Not expects a boolean input, consider using one of the Is methods");
            }

            return new List<object> { !((bool)target) };
        };

        public static FunctionHandler IsEqual = (primary, service, organizationConfig, parameters) =>
        {
            if (parameters.Count != 2)
            {
                throw new InvalidPluginExecutionException("IsEqual expects exactly 2 parameters!");
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

            if (expected is string && actual is string)
            {
                return new List<object> { expected.Equals(actual) };
            }

            if (expected is bool && actual is bool)
            {
                return new List<object> { expected.Equals(actual) };
            }

            if (expected is int && actual is int)
            {
                return new List<object> { expected.Equals(actual) };
            }

            if (expected is EntityReference && actual is EntityReference)
            {
                return new List<object> { expected.Equals(actual) };
            }

            if (new[] { expected, actual }.All(v => v is int || v is OptionSetValue))
            {
                var values = new[] { expected, actual }
                    .Select(v => v is OptionSetValue ? ((OptionSetValue)v).Value : (int)v)
                    .ToList();

                return new List<object> { values[0].Equals(values[1]) };
            }

            throw new InvalidPluginExecutionException($"Incompatible comparison types: {expected.GetType().Name} and {actual.GetType().Name}");
        };

        public static FunctionHandler And = (primary, service, organizationConfig, parameters) =>
        {
            if (parameters.Count != 2)
            {
                throw new InvalidPluginExecutionException("And expects at least 2 conditions!");
            }

            if (parameters.Any(p => !(p is bool)))
            {
                throw new InvalidPluginExecutionException("And: All conditions must be booleans!");
            }

            if (parameters.All(p => (bool)p))
            {
                return new List<object> { true };
            }

            return new List<object> { false };
        };

        public static FunctionHandler Or = (primary, service, organizationConfig, parameters) =>
        {
            if (parameters.Count != 2)
            {
                throw new InvalidPluginExecutionException("Or expects at least 2 conditions!");
            }

            if (parameters.Any(p => !(p is bool)))
            {
                throw new InvalidPluginExecutionException("Or: All conditions must be booleans!");
            }

            if (parameters.Any(p => (bool)p))
            {
                return new List<object> { true };
            }

            return new List<object> { false };
        };

        public static FunctionHandler IsNull = (primary, service, organizationConfig, parameters) =>
        {
            var target = parameters.FirstOrDefault();

            if (target == null)
            {
                return new List<object> { true };
            }

            return new List<object> { false };
        };

        public static FunctionHandler If = (primary, service, organizationConfig, parameters) =>
        {
            if (parameters.Count != 3)
            {
                throw new InvalidPluginExecutionException("If-Then-Else expects exactly three parameters: Condition, True-Action, False-Action");
            }

            var condition = parameters[0];
            var trueAction = parameters[1];
            var falseAction = parameters[2];

            if (!(condition is bool))
            {
                throw new InvalidPluginExecutionException("If condition must be a boolean!");
            }

            if ((bool)condition)
            {
                return new List<object> { trueAction };
            }

            return new List<object> { falseAction };
        };

        public static FunctionHandler GetPrimaryRecord = (primary, service, organizationConfig, parameters) =>
        {
            if (primary == null)
            {
                return null;
            }

            return new List<object> { primary.ToEntityReference() };
        };

        public static FunctionHandler GetRecordUrl = (primary, service, organizationConfig, parameters) =>
        {
            if (organizationConfig == null || string.IsNullOrEmpty(organizationConfig.OrganizationUrl))
            {
                throw new InvalidPluginExecutionException("GetRecordUrl can't find the Organization Url inside the plugin step secure configuration. Please add it.");
            }

            if (!parameters.All(p => p is EntityReference || p is Entity || p == null))
            {
                throw new InvalidPluginExecutionException("Only Entity Reference Objects are supported in GetRecordUrl");
            }

            var refs = parameters.Where(p => p != null).Select(e =>
            {
                var entityReference = e as EntityReference;

                if (entityReference != null) {
                    return new
                    {
                        Id = entityReference.Id,
                        LogicalName = entityReference.LogicalName
                    };
                }

                var entity = e as Entity;
                
                return new
                {
                    Id = entity.Id,
                    LogicalName = entity.LogicalName
                };
            });
            var organizationUrl = organizationConfig.OrganizationUrl.EndsWith("/") ? organizationConfig.OrganizationUrl : organizationConfig.OrganizationUrl + "/";

            return new List<object>{
                string.Join(Environment.NewLine, refs.Select(e => 
                {
                    var url = $"{organizationUrl}main.aspx?etn={e.LogicalName}&id={e.Id}&newWindow=true&pagetype=entityrecord";
                    return $"<a href=\"{url}\">{url}</a>";
                }))
            };
        };

        private static Func<string, IOrganizationService, Dictionary<string, string>> RetrieveColumnNames = (entityName, service) =>
        {
            return (service.Execute(new RetrieveEntityRequest
            {
                EntityFilters = EntityFilters.Attributes,
                LogicalName = entityName
            }) as RetrieveEntityResponse)
            .EntityMetadata
            .Attributes
            .ToDictionary(a => a.LogicalName, a => a.DisplayName.UserLocalizedLabel.Label);
        };

        public static FunctionHandler GetSubRecordTable = (primary, service, organizationConfig, parameters) =>
        {
            if (parameters.Count < 4)
            {
                throw new InvalidPluginExecutionException("GetSubRecords needs at least 4 parameters: Parent Entity / Entities, sub entity name, sub entity lookup, add url boolean, display columns as separate string constants");
            }

            var subEntityName = parameters[1] as string;
            var addRecordUrl = parameters[3] as bool?;
            var displayColumns = parameters.Skip(4).Cast<string>();
            var records = GetSubRecords(primary, service, organizationConfig, parameters).Cast<Entity>();
            var columnNames = RetrieveColumnNames(subEntityName, service);

            var tableHeadStyle = @"style=""border:1px solid black;text-align:left;padding:1px 15px 1px 5px""";
            var tableDataStyle = @"style=""border:1px solid black;padding:1px 15px 1px 5px""";

            // Create table header
            var stringBuilder = new StringBuilder("<table>\n<tr>");
            foreach (var column in displayColumns)
            {
                var name = columnNames.ContainsKey(column) ? columnNames[column] : column;
                stringBuilder.AppendLine($"<th {tableHeadStyle}>{name}</th>");
            }

            // Add column for url if wanted
            if (addRecordUrl.HasValue && addRecordUrl.Value)
            {
                stringBuilder.AppendLine($"<th {tableHeadStyle}>URL</th>");
            }
            stringBuilder.AppendLine("<tr />");

            foreach (var record in records)
            {
                stringBuilder.AppendLine("<tr>");

                foreach ( var column in displayColumns )
                {
                    stringBuilder.AppendLine($"<td {tableDataStyle}>{PropertyStringifier.Stringify(record, column)}</td>");
                }

                if (addRecordUrl.HasValue && addRecordUrl.Value)
                {
                    stringBuilder.AppendLine($"<td {tableDataStyle}>{GetRecordUrl(primary, service, organizationConfig, new List<object> { record }).FirstOrDefault()}</td>");
                }

                stringBuilder.AppendLine("<tr />");
            }

            stringBuilder.AppendLine("</table>");
            
            return new List<object> { stringBuilder.ToString() };
        };

        public static FunctionHandler GetSubRecords = (primary, service, organizationConfig, parameters) =>
        {
            if (parameters.Count < 4)
            {
                throw new InvalidPluginExecutionException("GetSubRecords needs 4 parameters: Parent Entity / Entities, sub entity name, sub entity lookup, display field");
            }

            var parentEntities = parameters[0];
            var subEntityName = parameters[1] as string;
            var subEntityLookup = parameters[2] as string;
            var addRecordUrl = parameters[3] as bool?;
            var displayColumns = parameters.Skip(4).Cast<string>();

            List<EntityReference> parents = new List<EntityReference>();

            if (parentEntities is IEnumerable)
            {
                foreach (var item in (IEnumerable)parentEntities)
                {
                    if (item is EntityReference)
                    {
                        parents.Add(item as EntityReference);
                    }

                    if (item is Entity)
                    {
                        parents.Add((item as Entity).ToEntityReference());
                    }
                }
            }
            else if (parentEntities is EntityReference)
            {
                parents.Add(parentEntities as EntityReference);
            }
            else if (parentEntities is Entity)
            {
                parents.Add((parentEntities as Entity).ToEntityReference());
            }
            else if (parentEntities == null)
            {
                return null;
            }
            else
            {
                throw new InvalidPluginExecutionException($"Parent type {parentEntities.GetType()} is invalid, please pass an entity or entityreference or collections of these");
            }

            var records = new List<object>();

            foreach (var parent in parents)
            {
                var query = new QueryExpression
                {
                    EntityName = subEntityName,
                    NoLock = true,
                    ColumnSet = new ColumnSet(new string[] { subEntityLookup }.Concat(displayColumns).ToArray()),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression(subEntityLookup, ConditionOperator.Equal, parent.Id)
                        }
                    }
                };

                records.AddRange(service.RetrieveMultiple(query).Entities);
            }

            return records;
        };

        public static FunctionHandler GetText = (primary, service, organizationConfig, parameters) =>
        {
            if (primary == null)
            {
                return null;
            }

            var field = parameters.FirstOrDefault() as string;

            if (field == null)
            {
                throw new InvalidPluginExecutionException("Text requires a field target string as input");
            }

            return new List<object> { DataRetriever.ResolveTokenText(field, primary, service) };
        };

        public static FunctionHandler GetValue = (primary, service, organizationConfig, parameters) =>
        {
            if (primary == null)
            {
                return null;
            }

            var field = parameters.FirstOrDefault() as string;

            if (field == null)
            {
                throw new InvalidPluginExecutionException("Value requires a field target string as input");
            }

            return new List<object> { DataRetriever.ResolveTokenValue(field, primary, service) };
        };
    }
}
