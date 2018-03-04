using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using static Xrm.Oss.XTL.Interpreter.XTLInterpreter;

namespace Xrm.Oss.XTL.Interpreter
{
    public static class FunctionHandlers
    {
        public static FunctionHandler Not = (primary, service, tracing, organizationConfig, parameters) =>
        {
            var target = parameters.FirstOrDefault();

            if (!(target is bool))
            {
                throw new InvalidPluginExecutionException("Not expects a boolean input, consider using one of the Is methods");
            }

            return new List<object> { !((bool)target) };
        };

        public static FunctionHandler First = (primary, service, tracing, organizationConfig, parameters) =>
        {
            if (parameters.Count != 1)
            {
                throw new InvalidPluginExecutionException("First expects a list as only parameter!");
            }

            return new List<object> { ((List<object>) parameters.FirstOrDefault()).FirstOrDefault() };
        };

        public static FunctionHandler Last = (primary, service, tracing, organizationConfig, parameters) =>
        {
            if (parameters.Count != 1)
            {
                throw new InvalidPluginExecutionException("Last expects a list as only parameter!");
            }

            return new List<object> { ((List<object>)parameters.FirstOrDefault()).LastOrDefault() };
        };

        public static FunctionHandler IsEqual = (primary, service, tracing, organizationConfig, parameters) =>
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

        public static FunctionHandler And = (primary, service, tracing, organizationConfig, parameters) =>
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

        public static FunctionHandler Or = (primary, service, tracing, organizationConfig, parameters) =>
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

        public static FunctionHandler IsNull = (primary, service, tracing, organizationConfig, parameters) =>
        {
            var target = parameters.FirstOrDefault();

            if (target == null)
            {
                return new List<object> { true };
            }

            return new List<object> { false };
        };

        public static FunctionHandler If = (primary, service, tracing, organizationConfig, parameters) =>
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

        public static FunctionHandler GetPrimaryRecord = (primary, service, tracing, organizationConfig, parameters) =>
        {
            if (primary == null)
            {
                return null;
            }

            return new List<object> { primary };
        };

        public static FunctionHandler GetRecordUrl = (primary, service, tracing, organizationConfig, parameters) =>
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
            return ((RetrieveEntityResponse) service.Execute(new RetrieveEntityRequest
            {
                EntityFilters = EntityFilters.Attributes,
                LogicalName = entityName,
                RetrieveAsIfPublished = false
            }))
            .EntityMetadata
            .Attributes
            .ToDictionary(a => a.LogicalName, a => a?.DisplayName?.UserLocalizedLabel?.Label ?? a.LogicalName);
        };

        public static FunctionHandler RenderRecordTable = (primary, service, tracing, organizationConfig, parameters) =>
        {
            tracing.Trace("Parsing parameters");

            if (parameters.Count < 3)
            {
                throw new InvalidPluginExecutionException("RecordTable needs at least 3 parameters: Entities, entity name, add url boolean, display columns as separate string constants");
            }

            var records = ((List<object>)parameters[0]).Cast<Entity>().ToList();
            tracing.Trace($"Records: {records.Count}");

            // We need the entity name although it should be set in the record. If no records are passed, we would fail to display the grid with proper columns otherwise
            var entityName = parameters[1] as string;
            var addRecordUrl = parameters[2] as bool?;

            // We need the column names explicitly, since CRM does not return null-valued columns, so that we can't rely on the column union of all records. In addition to that, the order can be set this way
            var displayColumns = parameters.Skip(3)?.Cast<string>() ?? new List<string>();

            tracing.Trace("Retrieving column names");
            var columnNames = RetrieveColumnNames(entityName, service);
            tracing.Trace($"Column names done");

            var tableHeadStyle = @"style=""border:1px solid black;text-align:left;padding:1px 15px 1px 5px""";
            var tableDataStyle = @"style=""border:1px solid black;padding:1px 15px 1px 5px""";

            tracing.Trace("Parsed parameters");

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

            if (records != null)
            {
                foreach (var record in records)
                {
                    stringBuilder.AppendLine("<tr>");

                    foreach (var column in displayColumns)
                    {
                        stringBuilder.AppendLine($"<td {tableDataStyle}>{PropertyStringifier.Stringify(record, column)}</td>");
                    }

                    if (addRecordUrl.HasValue && addRecordUrl.Value)
                    {
                        stringBuilder.AppendLine($"<td {tableDataStyle}>{GetRecordUrl(primary, service, tracing, organizationConfig, new List<object> { record }).FirstOrDefault()}</td>");
                    }

                    stringBuilder.AppendLine("<tr />");
                }
            }

            stringBuilder.AppendLine("</table>");
            
            return new List<object> { stringBuilder.ToString() };
        };

        public static FunctionHandler Fetch = (primary, service, tracing, organizationConfig, parameters) =>
        {
            if (parameters.Count < 1)
            {
                throw new InvalidPluginExecutionException("Fetch needs at least one parameter: Fetch XML.");
            }

            var fetch = parameters[0] as string;
            var @params = parameters.Skip(1).ToList();
            
            List<object> references = new List<object> { primary.Id };

            if (@params is IEnumerable)
            {
                foreach (var item in @params)
                {
                    var reference = item as EntityReference;
                    if (reference != null)
                    {
                        references.Add(reference.Id);
                    }

                    var entity = item as Entity;
                    if (entity != null)
                    {
                        references.Add(entity.Id);
                    }

                    var optionSet = item as OptionSetValue;
                    if (optionSet != null)
                    {
                        references.Add(optionSet.Value);
                    }
                }
            }

            var records = new List<object>();

            var query = fetch;

            if (primary != null)
            {
                query = query.Replace("{0}", references[0].ToString());
            }

            tracing.Trace("Replacing references");

            var referenceRegex = new Regex("{([0-9]+)}", RegexOptions.Compiled | RegexOptions.CultureInvariant);
            query = referenceRegex.Replace(query, match =>
            {
                var capture = match.Groups[1].Value;
                var referenceNumber = int.Parse(capture);

                if (referenceNumber >= references.Count)
                {
                    throw new InvalidPluginExecutionException($"You tried using reference {referenceNumber} in fetch, but there are less reference inputs than that.");
                }

                return references[referenceNumber].ToString();
            });

            tracing.Trace("References replaced");
            tracing.Trace($"Executing fetch: {query}");
            records.AddRange(service.RetrieveMultiple(new FetchExpression(query)).Entities);

            return new List<object> { records };
        };

        public static FunctionHandler GetText = (primary, service, tracing, organizationConfig, parameters) =>
        {
            if (primary == null)
            {
                return null;
            }

            var field = parameters.FirstOrDefault() as string;
            var target = primary;

            if (parameters.Count > 1)
            {
                target = parameters[1] as Entity;
            }

            if (field == null)
            {
                throw new InvalidPluginExecutionException("Text requires a field target string as input");
            }

            return new List<object> { DataRetriever.ResolveTokenText(field, target, service) };
        };

        public static FunctionHandler GetValue = (primary, service, tracing, organizationConfig, parameters) =>
        {
            if (primary == null)
            {
                return null;
            }

            var field = parameters.FirstOrDefault() as string;
            var target = primary;

            if (parameters.Count > 1)
            {
                target = parameters[1] as Entity;
            }

            if (field == null)
            {
                throw new InvalidPluginExecutionException("Value requires a field target string as input");
            }

            return new List<object> { DataRetriever.ResolveTokenValue(field, target, service) };
        };
    }
}
