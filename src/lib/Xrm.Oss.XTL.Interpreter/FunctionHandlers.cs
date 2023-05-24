using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Xrm.Oss.FluentQuery;
using static Xrm.Oss.XTL.Interpreter.XTLInterpreter;

namespace Xrm.Oss.XTL.Interpreter
{
    #pragma warning disable S1104 // Fields should not have public accessibility
    public static class FunctionHandlers
    {
        private static Lazy<HttpClient> _httpClient = new Lazy<HttpClient>(() => 
        {
            var client = new HttpClient();
            
            client.DefaultRequestHeaders
                .Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        });
        
        private static ConfigHandler GetConfig(List<ValueExpression> parameters)
        {
            return new ConfigHandler((Dictionary<string, object>) parameters.LastOrDefault(p => p?.Value is Dictionary<string, object>)?.Value ?? new Dictionary<string, object>());
        }

        public static FunctionHandler Not = (primary, service, tracing, organizationConfig, parameters) =>
        {
            var target = parameters.FirstOrDefault();
            var result = !CheckedCast<bool>(target?.Value, "Not expects a boolean input, consider using one of the Is methods");

            return new ValueExpression(result.ToString(CultureInfo.InvariantCulture), result);
        };

        public static FunctionHandler First = (primary, service, tracing, organizationConfig, parameters) =>
        {
            if (parameters.Count != 1)
            {
                throw new InvalidPluginExecutionException("First expects a list as only parameter!");
            }

            var firstParam = CheckedCast<List<ValueExpression>>(parameters.FirstOrDefault().Value, string.Empty, false)?.ToList();
            var entityCollection = CheckedCast<EntityCollection>(parameters.FirstOrDefault().Value, string.Empty, false)?.Entities.ToList();

            if (firstParam == null && entityCollection == null)
            {
                throw new InvalidPluginExecutionException("First expects a list or EntityCollection as input");
            }

            var outputValue = firstParam?.FirstOrDefault()?.Value ?? entityCollection?.FirstOrDefault();
            var outputText = firstParam?.FirstOrDefault()?.Text ?? entityCollection?.FirstOrDefault()?.LogicalName;
            
            return new ValueExpression(outputText, outputValue);
        };

        public static FunctionHandler Last = (primary, service, tracing, organizationConfig, parameters) =>
        {
            if (parameters.Count != 1)
            {
                throw new InvalidPluginExecutionException("Last expects a list as only parameter!");
            }

            var firstParam = CheckedCast<List<ValueExpression>>(parameters.FirstOrDefault().Value, string.Empty, false)?.ToList();
            var entityCollection = CheckedCast<EntityCollection>(parameters.FirstOrDefault().Value, string.Empty, false)?.Entities.ToList();

            if (firstParam == null && entityCollection == null)
            {
                throw new InvalidPluginExecutionException("Last expects a list or EntityCollection as input");
            }

            var outputValue = firstParam?.LastOrDefault()?.Value ?? entityCollection?.LastOrDefault();
            var outputText = firstParam?.LastOrDefault()?.Text ?? entityCollection?.LastOrDefault()?.LogicalName;
            
            return new ValueExpression(outputText, outputValue);
        };

        public static FunctionHandler IsLess = (primary, service, tracing, organizationConfig, parameters) =>
        {
            bool result = Compare(parameters) < 0;

            return new ValueExpression(result.ToString(CultureInfo.InvariantCulture), result);
        };

        public static FunctionHandler IsLessEqual = (primary, service, tracing, organizationConfig, parameters) =>
        {
            bool result = Compare(parameters) <= 0;

            return new ValueExpression(result.ToString(CultureInfo.InvariantCulture), result);
        };

        public static FunctionHandler IsGreater = (primary, service, tracing, organizationConfig, parameters) =>
        {
            bool result = Compare(parameters) > 0;

            return new ValueExpression(result.ToString(CultureInfo.InvariantCulture), result);
        };

        public static FunctionHandler IsGreaterEqual = (primary, service, tracing, organizationConfig, parameters) =>
        {
            bool result = Compare(parameters) >= 0;

            return new ValueExpression(result.ToString(CultureInfo.InvariantCulture), result);
        };

        private static int Compare(List<ValueExpression> parameters)
        {
            if (parameters.Count != 2)
            {
                throw new InvalidPluginExecutionException("IsLess expects exactly 2 parameters!");
            }

            var actual = CheckedCast<IComparable>(parameters[0].Value, "Actual value is not comparable");
            var expected = CheckedCast<IComparable>(parameters[1].Value, "Expected value is not comparable");

            // Negative: actual is less than expected, 0: equal, 1: actual is greater than expected
            return actual.CompareTo(expected);
        }

        public static FunctionHandler IsEqual = (primary, service, tracing, organizationConfig, parameters) =>
        {
            if (parameters.Count != 2)
            {
                throw new InvalidPluginExecutionException("IsEqual expects exactly 2 parameters!");
            }

            var expected = parameters[0];
            var actual = parameters[1];
            var tempGuid = Guid.Empty;

            var falseReturn = new ValueExpression(bool.FalseString, false);
            var trueReturn = new ValueExpression(bool.TrueString, true);

            if (expected.Value == null && actual.Value == null)
            {
                return trueReturn;
            }

            if (expected.Value == null && actual.Value != null)
            {
                return falseReturn;
            }

            if (expected.Value != null && actual.Value == null)
            {
                return falseReturn;
            }

            if (new[] { expected.Value, actual.Value }.All(v => v is int || v is OptionSetValue))
            {
                var values = new[] { expected.Value, actual.Value }
                    .Select(v => v is OptionSetValue ? ((OptionSetValue)v).Value : (int)v)
                    .ToList();

                var optionSetResult = values[0].Equals(values[1]);
                return new ValueExpression(optionSetResult.ToString(CultureInfo.InvariantCulture), optionSetResult);
            }
            else if (new[] { expected.Value, actual.Value }.All(v => v is Guid || (v is string && Guid.TryParse((string) v, out tempGuid)) || v is EntityReference || v is Entity))
            {
                var values = new[] { expected.Value, actual.Value }
                    .Select(v =>
                    {
                        if (v is Guid)
                        {
                            return (Guid) v;
                        }

                        if (v is string)
                        {
                            return tempGuid;
                        }

                        if (v is EntityReference)
                        {
                            return ((EntityReference) v).Id;
                        }

                        if (v is Entity)
                        {
                            return ((Entity) v).Id;
                        }

                        return Guid.Empty;
                    })
                    .ToList();

                var guidResult = values[0].Equals(values[1]);
                return new ValueExpression(guidResult.ToString(CultureInfo.InvariantCulture), guidResult);
            }
            else
            {
                var result = expected.Value.Equals(actual.Value);

                return new ValueExpression(result.ToString(CultureInfo.InvariantCulture), result);
            }
        };

        public static FunctionHandler And = (primary, service, tracing, organizationConfig, parameters) =>
        {
            if (parameters.Count < 2)
            {
                throw new InvalidPluginExecutionException("And expects at least 2 conditions!");
            }

            if (parameters.Any(p => !(p.Value is bool)))
            {
                throw new InvalidPluginExecutionException("And: All conditions must be booleans!");
            }

            var conditions = parameters.All(p =>
            {
                if (p.Value is bool)
                {
                    return (bool)p.Value;   
                }
               
                throw new InvalidPluginExecutionException("And: All conditions must be booleans!");
            });
            
            if (conditions)
            {
                return new ValueExpression(bool.TrueString, true);
            }

            return new ValueExpression(bool.FalseString, false);
        };

        public static FunctionHandler Or = (primary, service, tracing, organizationConfig, parameters) =>
        {
            if (parameters.Count < 2)
            {
                throw new InvalidPluginExecutionException("Or expects at least 2 conditions!");
            }

            var conditions = parameters.Any(p =>
            {
                if (p.Value is bool)
                {
                    return (bool)p.Value;   
                }
               
                throw new InvalidPluginExecutionException("Or: All conditions must be booleans!");
            });
            
            if (conditions)
            {
                return new ValueExpression(bool.TrueString, true);
            }

            return new ValueExpression(bool.FalseString, false);
        };

        public static FunctionHandler IsNull = (primary, service, tracing, organizationConfig, parameters) =>
        {
            var target = parameters.FirstOrDefault();

            if (target.Value == null)
            {
                return new ValueExpression(bool.TrueString, true);
            }

            return new ValueExpression(bool.FalseString, false);
        };

        public static FunctionHandler If = (primary, service, tracing, organizationConfig, parameters) =>
        {
            if (parameters.Count != 3)
            {
                throw new InvalidPluginExecutionException("If-Then-Else expects exactly three parameters: Condition, True-Action, False-Action");
            }

            var condition = CheckedCast<bool>(parameters[0]?.Value, "If condition must be a boolean!");
            var trueAction = parameters[1];
            var falseAction = parameters[2];

            if (condition)
            {
                tracing.Trace("Executing true condition");
                return new ValueExpression(new Lazy<ValueExpression>(() => trueAction));
            }

            tracing.Trace("Executing false condition");
            return new ValueExpression(new Lazy<ValueExpression>(() => falseAction));
        };

        public static FunctionHandler GetPrimaryRecord = (primary, service, tracing, organizationConfig, parameters) =>
        {
            if (primary == null)
            {
                return new ValueExpression(null);
            }

            return new ValueExpression(string.Empty, primary);
        };

        public static FunctionHandler GetRecordUrl = (primary, service, tracing, organizationConfig, parameters) =>
        {
            if (organizationConfig == null || string.IsNullOrEmpty(organizationConfig.OrganizationUrl))
            {
                throw new InvalidPluginExecutionException("GetRecordUrl can't find the Organization Url inside the plugin step secure configuration. Please add it.");
            }

            if (!parameters.All(p => p.Value is EntityReference || p.Value is Entity || p.Value is Dictionary<string, object> || p.Value == null))
            {
                throw new InvalidPluginExecutionException("Only Entity Reference and Entity ValueExpressions are supported in GetRecordUrl");
            }

            var refs = parameters.Where(p => p != null && !(p?.Value is Dictionary<string, object>)).Select(e =>
            {
                var entityReference = e.Value as EntityReference;

                if (entityReference != null) {
                    return new
                    {
                        Id = entityReference.Id,
                        LogicalName = entityReference.LogicalName
                    };
                }

                var entity = e.Value as Entity;

                return new
                {
                    Id = entity.Id,
                    LogicalName = entity.LogicalName
                };
            });
            var organizationUrl = organizationConfig.OrganizationUrl.EndsWith("/") ? organizationConfig.OrganizationUrl : organizationConfig.OrganizationUrl + "/";

            var config = GetConfig(parameters);
            var linkText = config.GetValue<string>("linkText", "linkText must be a string");
            var appId = config.GetValue<string>("appId", "appId must be a string");

            var urls = string.Join(Environment.NewLine, refs.Select(e =>
            {
                var url = $"{organizationUrl}main.aspx?etn={e.LogicalName}&id={e.Id}&newWindow=true&pagetype=entityrecord{(string.IsNullOrEmpty(appId) ? string.Empty : $"&appid={appId}")}";
                return $"<a href=\"{url}\">{(string.IsNullOrEmpty(linkText) ? url : linkText)}</a>";
            }));

            return new ValueExpression(urls, urls);
        };

        public static FunctionHandler GetOrganizationUrl = (primary, service, tracing, organizationConfig, parameters) =>
        {
            if (organizationConfig == null || string.IsNullOrEmpty(organizationConfig.OrganizationUrl))
            {
                throw new InvalidPluginExecutionException("GetOrganizationUrl can't find the Organization Url inside the plugin step secure configuration. Please add it.");
            }

            var config = GetConfig(parameters);
            var linkText = config.GetValue<string>("linkText", "linkText must be a string", string.Empty);
            var urlSuffix = config.GetValue<string>("urlSuffix", "urlSuffix must be a string", string.Empty);
            var asHtml = config.GetValue<bool>("asHtml", "asHtml must be a boolean");

            if (asHtml)
            {
                var url = $"{organizationConfig.OrganizationUrl}{urlSuffix}";
                var href = $"<a href=\"{url}\">{(string.IsNullOrEmpty(linkText) ? url : linkText)}</a>";
                return new ValueExpression(href, href);
            }
            else
            {
                return new ValueExpression(organizationConfig.OrganizationUrl, organizationConfig.OrganizationUrl);
            }
        };

        public static FunctionHandler Union = (primary, service, tracing, organizationConfig, parameters) =>
        {
            if (parameters.Count < 2)
            {
                throw new InvalidPluginExecutionException("Union function needs at least two parameters: Arrays to union");
            }

            var union = parameters.Select(p =>
            {
                if (p == null)
                {
                    return null;
                }

                return p.Value as List<ValueExpression>;
            })
            .Where(p => p != null)
            .SelectMany(p => p)
            .ToList();

            return new ValueExpression(null, union);
        };

        public static FunctionHandler Map = (primary, service, tracing, organizationConfig, parameters) =>
        {
            if (parameters.Count < 2)
            {
                throw new InvalidPluginExecutionException("Map function needs at least an array with data and a function for mutating the data");
            }

            var config = GetConfig(parameters);

            var values = parameters[0].Value as List<ValueExpression>;

            if (!(values is IEnumerable))
            {
                throw new InvalidPluginExecutionException("Map needs an array as first parameter.");
            }

            var lambda = parameters[1].Value as Func<List<ValueExpression>, ValueExpression>;

            if (lambda == null)
            {
                throw new InvalidPluginExecutionException("Lambda function must be a proper arrow function");
            }

            var mappedValues = values.Select(v => lambda(new List<ValueExpression> { v })).ToList();
            
            return new ValueExpression(string.Join(", ", mappedValues.Select(p => p.Text)), mappedValues);
        };

        public static FunctionHandler Length = (primary, service, tracing, organizationConfig, parameters) =>
        {
            if (parameters.Count < 1)
            {
                throw new InvalidPluginExecutionException("Length function needs at least an array or a string as input");
            }

            var config = GetConfig(parameters);

            var values = parameters[0]?.Value as List<ValueExpression>;

            if (values is IEnumerable)
            {
                return new ValueExpression(values.Count.ToString(), values.Count);
            }

            var stringValue = CheckedCast<string>(parameters[0]?.Value, "Parameter of length function must be either an array or a string");

            return new ValueExpression(stringValue.Length.ToString(), stringValue.Length.ToString());
        };

        public static FunctionHandler Filter = (primary, service, tracing, organizationConfig, parameters) =>
        {
            if (parameters.Count < 2)
            {
                throw new InvalidPluginExecutionException("Filter function needs at least an array with data and a function for filtering the data");
            }

            var config = GetConfig(parameters);

            var values = parameters[0].Value as List<ValueExpression>;

            if (!(values is IEnumerable))
            {
                throw new InvalidPluginExecutionException("Filter needs an array as first parameter.");
            }

            var lambda = parameters[1].Value as Func<List<ValueExpression>, ValueExpression>;

            if (lambda == null)
            {
                throw new InvalidPluginExecutionException("Lambda function must be a proper arrow function");
            }

            var filteredValues = values.Where(v => lambda(new List<ValueExpression> { v }).Value as bool? ?? false).ToList();

            return new ValueExpression(string.Join(", ", filteredValues.Select(p => p.Text)), filteredValues);
        };

        public static FunctionHandler Coalesce = (primary, service, tracing, organizationConfig, parameters) =>
        {
            var firstNonNullValue = parameters.FirstOrDefault(p => p?.Value != null);

            return new ValueExpression(firstNonNullValue?.Text, firstNonNullValue?.Value);
        };

        public static FunctionHandler Case = (primary, service, tracing, organizationConfig, parameters) =>
        {
            if (parameters.Count % 2 == 0)
            {
                throw new InvalidPluginExecutionException("Case function expects an odd number of parameters, as it consists of if-then tuples followed by a final 'else'");
            }

            var match = parameters
                .Select((parameter, index) => new { parameter, index })
                .SkipWhile(tuple => !(tuple.index % 2 == 0 && (tuple.parameter?.Value as bool? ?? false)))
                // If match was found, use next value as match is the condition and next value is the corresponding result to use
                .Skip(1)
                .FirstOrDefault();

            var result = match?.parameter ?? parameters.Last(); 

            return new ValueExpression(result?.Text, result?.Value);
        };

        public static FunctionHandler Sort = (primary, service, tracing, organizationConfig, parameters) =>
        {
            if (parameters.Count < 1)
            {
                throw new InvalidPluginExecutionException("Sort function needs at least an array to sort and optionally a property for sorting");
            }

            var config = GetConfig(parameters);

            var values = parameters[0].Value as List<ValueExpression>;

            if (!(values is IEnumerable))
            {
                throw new InvalidPluginExecutionException("Sort needs an array as first parameter.");
            }

            var descending = config.GetValue<bool>("descending", "descending must be a bool");
            var property = config.GetValue<string>("property", "property must be a string");

            if (string.IsNullOrEmpty(property))
            {
                if (descending)
                {
                    return new ValueExpression(null, values.OrderByDescending(v => v.Value).ToList());
                }
                else
                {
                    return new ValueExpression(null, values.OrderBy(v => v.Value).ToList());
                }
            }
            else
            {
                if (descending)
                {
                    return new ValueExpression(null, values.OrderByDescending(v => (v.Value as Entity)?.GetAttributeValue<object>(property)).ToList());
                }
                else
                {
                    return new ValueExpression(null, values.OrderBy(v => (v.Value as Entity)?.GetAttributeValue<object>(property)).ToList());
                }
            }
        };

        private static Func<string, IOrganizationService, Dictionary<string, string>> RetrieveColumnNames = (entityName, service) =>
        {
            return ((RetrieveEntityResponse)service.Execute(new RetrieveEntityRequest
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

            var records = CheckedCast<List<ValueExpression>>(parameters[0].Value, "RecordTable requires the first parameter to be a list of entities")
                .Select(p => (p as ValueExpression)?.Value)
                .Cast<Entity>()
                .ToList();

            tracing.Trace($"Records: {records.Count}");

            // We need the entity name although it should be set in the record. If no records are passed, we would fail to display the grid with proper columns otherwise
            var entityName = CheckedCast<string>(parameters[1]?.Value, "Second parameter of the RecordTable function needs to be the entity name as string");

            if (string.IsNullOrEmpty(entityName))
            {
                throw new InvalidPluginExecutionException("Second parameter of the RecordTable function needs to be the entity name as string");
            }

            // We need the column names explicitly, since CRM does not return new ValueExpression(null)-valued columns, so that we can't rely on the column union of all records. In addition to that, the order can be set this way
            var displayColumns = CheckedCast<List<ValueExpression>>(parameters[2]?.Value, "List of column names for record table must be an array expression")
                .Select(p => p.Value)
                .Select(p => p is Dictionary<string, object> ? (Dictionary<string, object>) p : new Dictionary<string, object> { { "name", p } })
                .ToList();

            tracing.Trace("Retrieving column names");
            var columnNames = RetrieveColumnNames(entityName, service);
            tracing.Trace($"Column names done");

            var config = GetConfig(parameters);
            var addRecordUrl = config.GetValue<bool>("addRecordUrl", "When setting addRecordUrl, value must be a boolean");

            var tableStyle = config.GetValue("tableStyle", "tableStyle must be a string!", string.Empty);

            if (!string.IsNullOrEmpty(tableStyle))
            {
                tableStyle = $" style=\"{tableStyle}\"";
            }

            var tableHeadStyle = config.GetValue("headerStyle", "headerStyle must be a string!", @"border:1px solid black;text-align:left;padding:1px 15px 1px 5px;");
            var tableDataStyle = config.GetValue("dataStyle", "dataStyle must be a string!", @"border:1px solid black;padding:1px 15px 1px 5px;");

            var evenDataStyle = config.GetValue<string>("evenDataStyle", "evenDataStyle must be a string!");
            var unevenDataStyle = config.GetValue<string>("unevenDataStyle", "unevenDataStyle must be a string!");

            tracing.Trace("Parsed parameters");

            // Create table header
            var stringBuilder = new StringBuilder($"<table{tableStyle}>\n<tr>");
            foreach (var column in displayColumns)
            {
                var name = string.Empty;
                var columnName = column.ContainsKey("name") ? column["name"] as string : string.Empty;

                if (columnName.Contains(":"))
                {
                    name = columnName.Substring(columnName.IndexOf(':') + 1);
                }
                else
                {
                    name = columnNames.ContainsKey(columnName) ? columnNames[columnName] : columnName;
                }

                if (column.ContainsKey("label"))
                {
                    name = column["label"] as string;
                }

                if (column.ContainsKey("style"))
                {
                    if (!column.ContainsKey("mergeStyle") || (bool) column["mergeStyle"])
                    {
                        stringBuilder.AppendLine($"<th style=\"{tableHeadStyle}{column["style"]}\">{name}</th>");
                    }
                    else
                    {
                        stringBuilder.AppendLine($"<th style=\"{column["style"]}\">{name}</th>");
                    }
                }
                else
                {
                    stringBuilder.AppendLine($"<th style=\"{tableHeadStyle}\">{name}</th>");
                }
            }

            // Add column for url if wanted
            if (addRecordUrl)
            {
                stringBuilder.AppendLine($"<th style=\"{tableHeadStyle}\">URL</th>");
            }
            stringBuilder.AppendLine("</tr>");

            if (records != null)
            {
                for (var i = 0; i < records.Count; i++)
                {
                    var record = records[i];
                    var isEven = i % 2 == 0;
                    var lineStyle = (isEven ? evenDataStyle : unevenDataStyle) ?? tableDataStyle;

                    stringBuilder.AppendLine("<tr>");

                    foreach (var column in displayColumns)
                    {
                        var columnName = column.ContainsKey("name") ? column["name"] as string : string.Empty;
                        columnName = columnName.Contains(":") ? columnName.Substring(0, columnName.IndexOf(':')) : columnName;
                        
                        var renderFunction = column.ContainsKey("renderFunction") ? column["renderFunction"] as Func<List<ValueExpression>, ValueExpression> : null;
                        var entityConfig = column.ContainsKey("nameByEntity") ? column["nameByEntity"] as Dictionary<string, object> : null;

                        if (entityConfig != null && entityConfig.ContainsKey(record.LogicalName))
                        {
                            columnName = entityConfig[record.LogicalName] as string;
                        }

                        var staticValues = column.ContainsKey("staticValueByEntity") ? column["staticValueByEntity"] as Dictionary<string, object> : null;

                        string value;

                        if (staticValues != null && staticValues.ContainsKey(record.LogicalName))
                        {
                            value = staticValues[record.LogicalName] as string;
                        }
                        else if (renderFunction != null)
                        {
                            var rowRecord = new ValueExpression(null, record);
                            var rowColumnName = new ValueExpression(columnName, columnName);

                            value = renderFunction(new List<ValueExpression> { rowRecord, rowColumnName })?.Text;
                        }
                        else
                        {
                            value = PropertyStringifier.Stringify(columnName, record, service, config);
                        }

                        if (column.ContainsKey("style"))
                        {
                            if (!column.ContainsKey("mergeStyle") || (bool)column["mergeStyle"])
                            {
                                stringBuilder.AppendLine($"<td style=\"{lineStyle}{column["style"]}\">{value}</td>");
                            }
                            else
                            {
                                stringBuilder.AppendLine($"<td style=\"{column["style"]}\">{value}</td>");
                            }
                        }
                        else
                        {
                            stringBuilder.AppendLine($"<td style=\"{lineStyle}\">{value}</td>");
                        }
                    }

                    if (addRecordUrl)
                    {
                        stringBuilder.AppendLine($"<td style=\"{lineStyle}\">{GetRecordUrl(primary, service, tracing, organizationConfig, new List<ValueExpression> { new ValueExpression(string.Empty, record), new ValueExpression(string.Empty, config.Dictionary) }).Value}</td>");
                    }

                    stringBuilder.AppendLine("</tr>");
                }
            }

            stringBuilder.AppendLine("</table>");
            var table = stringBuilder.ToString();

            return new ValueExpression(table, table);
        };

        public static FunctionHandler Fetch = (primary, service, tracing, organizationConfig, parameters) =>
        {
            if (parameters.Count < 1)
            {
                throw new InvalidPluginExecutionException("Fetch needs at least one parameter: Fetch XML.");
            }

            var fetch = parameters[0].Value as string;

            if (string.IsNullOrEmpty(fetch))
            {
                throw new InvalidPluginExecutionException("First parameter of Fetch function needs to be a fetchXml string");
            }

            var references = new List<object> { primary?.Id };

            if (parameters.Count > 1)
            {
                if (!(parameters[1].Value is List<ValueExpression>))
                {
                    throw new InvalidPluginExecutionException("Fetch parameters must be an array expression");
                }

                var @params = parameters[1].Value as List<ValueExpression>;
                
                if (@params is IEnumerable)
                {
                    foreach (var item in @params)
                    {
                        var reference = item.Value as EntityReference;
                        if (reference != null)
                        {
                            references.Add(reference.Id);
                            continue;
                        }

                        var entity = item.Value as Entity;
                        if (entity != null)
                        {
                            references.Add(entity.Id);
                            continue;
                        }

                        var optionSet = item.Value as OptionSetValue;
                        if (optionSet != null)
                        {
                            references.Add(optionSet.Value);
                            continue;
                        }

                        references.Add(item.Value);
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
                    throw new InvalidPluginExecutionException($"You tried using reference {referenceNumber} in fetch, but there are less reference inputs than that. Please check your reference number or your reference input array.");
                }

                return references[referenceNumber]?.ToString();
            });

            tracing.Trace("References replaced");
            tracing.Trace($"Executing fetch: {query}");
            records.AddRange(service.RetrieveMultiple(new FetchExpression(query)).Entities);

            return new ValueExpression(string.Empty, records.Select(r => new ValueExpression(string.Empty, r)).ToList());
        };

        public static FunctionHandler GetValue = (primary, service, tracing, organizationConfig, parameters) =>
        {
            if (primary == null)
            {
                return new ValueExpression(null);
            }

            var field = parameters.FirstOrDefault()?.Text;

            if (string.IsNullOrEmpty(field))
            {
                throw new InvalidPluginExecutionException("First parameter of Value function needs to be the field name as string");
            }

            var target = primary;
            var config = GetConfig(parameters);

            if (config.Contains("explicitTarget"))
            {
                if (config.IsSet("explicitTarget"))
                {
                    var explicitTarget = config.GetValue<Entity>("explicitTarget", "explicitTarget must be an entity!");

                    target = explicitTarget;
                }
                else
                {
                    return new ValueExpression(string.Empty, string.Empty);
                }
            }

            if (field == null)
            {
                throw new InvalidPluginExecutionException("Value requires a field target string as input");
            }

            return DataRetriever.ResolveTokenValue(field, target, service, config);
        };

        public static FunctionHandler Join = (primary, service, tracing, organizationConfig, parameters) =>
        {
            if (parameters.Count < 2)
            {
                throw new InvalidPluginExecutionException("Join function needs at lease two parameters: Separator and an array of values to concatenate");
            }

            var separator = parameters.FirstOrDefault()?.Text;

            if (string.IsNullOrEmpty(separator))
            {
                throw new InvalidPluginExecutionException("First parameter of Join function needs to be the separator string");
            }

            var values = parameters[1].Value as List<ValueExpression>;

            if (!(values is IEnumerable))
            {
                throw new InvalidPluginExecutionException("The values parameter needs to be an enumerable, please wrap them using an Array expression.");
            }

            var config = GetConfig(parameters);
            var removeEmptyEntries = false;

            if (parameters.Count > 2 && parameters[2].Value is bool)
            {
                removeEmptyEntries = (bool) parameters[2].Value;
            }

            var valuesToConcatenate = values
                .Where(v => !removeEmptyEntries || !string.IsNullOrEmpty(v.Text))
                .Select(v => v.Text);
                
            var joined = string.Join(separator, valuesToConcatenate);

            return new ValueExpression(joined, joined);
        };

        public static FunctionHandler NewLine = (primary, service, tracing, organizationConfig, parameters) =>
        {
            return new ValueExpression(Environment.NewLine, Environment.NewLine);
        };

        public static FunctionHandler Concat = (primary, service, tracing, organizationConfig, parameters) =>
        {
            var text = "";

            foreach (var parameter in parameters)
            {
                text += parameter.Text;
            }

            return new ValueExpression(text, text);
        };

        private static T CheckedCast<T>(object input, string errorMessage, bool failOnError = true)
        {
            var value = input;

            if (input is Money)
            {
                value = (input as Money).Value;
            }

            if (input is OptionSetValue)
            {
                value = (input as OptionSetValue).Value;
            }

            if (!(value is T))
            {
                if (failOnError)
                {
                    throw new InvalidPluginExecutionException(errorMessage);
                }

                return default(T);
            }

            return (T)value;
        }

        public static FunctionHandler Substring = (primary, service, tracing, organizationConfig, parameters) =>
        {
            if (parameters.Count < 2)
            {
                throw new InvalidPluginExecutionException("Substring expects at least two parameters: text, start index and optionally a length");
            }

            var text = parameters[0].Text;
            var startIndex = CheckedCast<int>(parameters[1].Value, "Start index parameter must be an int!");
            var length = -1;

            if (parameters.Count > 2)
            {
                length = CheckedCast<int>(parameters[2].Value, "Length parameter must be an int!");
            }

            var subString = length > -1 ? text.Substring(startIndex, length) : text.Substring(startIndex);
            return new ValueExpression(subString, subString);
        };

        public static FunctionHandler IndexOf = (primary, service, tracing, organizationConfig, parameters) =>
        {
            if (parameters.Count < 2)
            {
                throw new InvalidPluginExecutionException("IndexOf needs a source string and a string to search for");
            }

            var value = CheckedCast<string>(parameters[0].Value, "Source must be a string");
            var searchText = CheckedCast<string>(parameters[1].Value, "Search text must be a string");

            var config = GetConfig(parameters);
            var ignoreCase = config.GetValue<bool>("ignoreCase", "ignoreCase must be a boolean!");

            var index = value.IndexOf(searchText, ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture);

            return new ValueExpression(index.ToString(), index);
        };

        public static FunctionHandler Replace = (primary, service, tracing, organizationConfig, parameters) =>
        {
            if (parameters.Count < 3)
            {
                throw new InvalidPluginExecutionException("Replace expects three parameters: text input, regex pattern, regex replacement");
            }

            var input = parameters[0].Text;
            var pattern = parameters[1].Text;
            var replacement = parameters[2].Text;

            var replaced = Regex.Replace(input, pattern, replacement);

            return new ValueExpression(replaced, replaced);
        };

        public static FunctionHandler Array = (primary, service, tracing, organizationConfig, parameters) =>
        {
            return new ValueExpression(string.Join(", ", parameters.Select(p => p.Text)), parameters);
        };

        public static FunctionHandler DateTimeNow = (primary, service, tracing, organizationConfig, parameters) =>
        {
            var date = DateTime.Now;
            return new ValueExpression(date.ToString("o", CultureInfo.InvariantCulture), date);
        };

        public static FunctionHandler DateTimeUtcNow = (primary, service, tracing, organizationConfig, parameters) =>
        {
            var date = DateTime.UtcNow;
            return new ValueExpression(date.ToString("o", CultureInfo.InvariantCulture), date);
        };

        public static FunctionHandler Static = (primary, service, tracing, organizationConfig, parameters) =>
        {
            if (parameters.Count < 1)
            {
                throw new InvalidOperationException("You have to pass a static value");
            }

            var parameter = parameters[0];
            return new ValueExpression(parameter.Text, parameter.Value);
        };

        public static FunctionHandler DateToString = (primary, service, tracing, organizationConfig, parameters) =>
        {
            if (parameters.Count < 1)
            {
                throw new Exception("No date to stringify");
            }

            var date = CheckedCast<DateTime>(parameters[0].Value, "You need to pass a date");
            var config = GetConfig(parameters);
            var format = config.GetValue<string>("format", "format must be a string!");

            if (!string.IsNullOrEmpty(format))
            {
                return new ValueExpression(date.ToString(format), date.ToString(format));
            }

            return new ValueExpression(date.ToString(CultureInfo.InvariantCulture), date.ToString(CultureInfo.InvariantCulture));
        };

        public static FunctionHandler Format = (primary, service, tracing, organizationConfig, parameters) =>
        {
            if (parameters.Count < 2)
            {
                throw new InvalidPluginExecutionException("Format needs a value to format and a config for defining further options");
            }

            var value = parameters[0].Value;
            var config = GetConfig(parameters);
            var format = config.GetValue<string>("format", "format must be a string!");

            var knownTypes = new Dictionary<Type, Func<object, ValueExpression>>
            {
                { typeof(Money), (obj) => { var val = obj as Money; var formatted = string.Format(CultureInfo.InvariantCulture, format, val.Value); return new ValueExpression( formatted, formatted ); } }
            };

            if(knownTypes.ContainsKey(value.GetType()))
            {
                return knownTypes[value.GetType()](value);
            }
            else
            {
                var formatted = string.Format(CultureInfo.InvariantCulture, format, value);
                return new ValueExpression(formatted, formatted);
            }
        };

        private static Entity FetchSnippetByUniqueName(string uniqueName, IOrganizationService service)
        {
            var fetch = $@"<fetch no-lock=""true"">
                <entity name=""oss_xtlsnippet"">
                    <attribute name=""oss_xtlexpression"" />
                    <attribute name=""oss_containsplaintext"" />
                    <filter operator=""and"">
                        <condition attribute=""oss_uniquename"" operator=""eq"" value=""{uniqueName}"" />
                    </filter>
                </entity>
            </fetch>";

            var snippet = service.RetrieveMultiple(new FetchExpression(fetch))
                .Entities
                .FirstOrDefault();

            return snippet;
        }

        private static Entity FetchSnippet(string name, string filter, Entity primary, OrganizationConfig organizationConfig, IOrganizationService service, ITracingService tracing)
        {
            var uniqueNameSnippet = FetchSnippetByUniqueName(name, service);

            if (uniqueNameSnippet != null)
            {
                tracing.Trace("Found snippet by unique name");
                return uniqueNameSnippet;
            }

            if (!string.IsNullOrEmpty(filter))
            {
                tracing.Trace("Processing tokens in custom snippet filter");
            }

            var fetch = $@"<fetch no-lock=""true"">
                <entity name=""oss_xtlsnippet"">
                    <attribute name=""oss_xtlexpression"" />
                    <attribute name=""oss_containsplaintext"" />
                    <filter operator=""and"">
                        <condition attribute=""oss_name"" operator=""eq"" value=""{name}"" />
                        { (!string.IsNullOrEmpty(filter) ? TokenMatcher.ProcessTokens(filter, primary, organizationConfig, service, tracing) : string.Empty) }
                    </filter>
                </entity>
            </fetch>";
            
            if (!string.IsNullOrEmpty(filter))
            {
                tracing.Trace("Done processing tokens in custom snippet filter");
            }

            var snippet = service.RetrieveMultiple(new FetchExpression(fetch))
                .Entities
                .FirstOrDefault();

            return snippet;
        }

        public static FunctionHandler Snippet = (primary, service, tracing, organizationConfig, parameters) =>
        {
            if (parameters.Count < 1)
            {
                throw new InvalidPluginExecutionException("Snippet needs at least a name as first parameter and optionally a config for defining further options");
            }

            var name = CheckedCast<string>(parameters[0].Value, "Name must be a string!");
            var config = GetConfig(parameters);

            var filter = config?.GetValue<string>("filter", "filter must be a string containing your fetchXml filter, which may contain XTL expressions on its own");

            var snippet = FetchSnippet(name, filter, primary, organizationConfig, service, tracing);

            if (snippet == null)
            {
                tracing.Trace("Failed to find a snippet matching the input");
                return new ValueExpression(string.Empty, null);
            }

            var containsPlainText = snippet.GetAttributeValue<bool>("oss_containsplaintext");
            var value = snippet.GetAttributeValue<string>("oss_xtlexpression");

            // Wrap it in ${{ ... }} block
            var processedValue = containsPlainText ? value : $"${{{{ {value} }}}}";

            tracing.Trace("Processing snippet tokens");

            var result = TokenMatcher.ProcessTokens(processedValue, primary, organizationConfig, service, tracing);

            tracing.Trace("Done processing snippet tokens");

            return new ValueExpression(result, result);
        };

        public static FunctionHandler ConvertDateTime = (primary, service, tracing, organizationConfig, parameters) =>
        {
            if (parameters.Count < 2)
            {
                throw new InvalidPluginExecutionException("Convert DateTime needs a DateTime and a config for defining further options");
            }

            var date = CheckedCast<DateTime>(parameters[0].Value, "You need to pass a date");
            var config = GetConfig(parameters);

            var timeZoneId = config.GetValue<string>("timeZoneId", "timeZoneId must be a string");
            var userId = config.GetValue<EntityReference>("userId", "userId must be an EntityReference");

            if (userId == null && string.IsNullOrEmpty(timeZoneId))
            {
                throw new InvalidPluginExecutionException("You need to either set a userId for converting to a user's configured timezone, or pass a timeZoneId");
            }

            if (userId != null)
            {
                var userSettings = service.Retrieve("usersettings", userId.Id, new ColumnSet("timezonecode"));
                var timeZoneCode = userSettings.GetAttributeValue<int>("timezonecode");

                timeZoneId = service.Query("timezonedefinition")
                    .IncludeColumns("standardname")
                    .Where(e => e
                        .Attribute(a => a
                            .Named("timezonecode")
                            .Is(ConditionOperator.Equal)
                            .To(timeZoneCode)
                        )
                    )
                    .Retrieve()
                    .FirstOrDefault()
                    ?.GetAttributeValue<string>("standardname");
            }

            if (string.IsNullOrEmpty(timeZoneId))
            {
                throw new InvalidPluginExecutionException("Failed to retrieve timeZoneId, can't convert datetime");
            }

            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            var localTime = TimeZoneInfo.ConvertTime(date, timeZone);
            var text = localTime.ToString(config.GetValue<string>("format", "format must be a string", "g"), CultureInfo.InvariantCulture);

            return new ValueExpression(text, localTime);
        };

        public static FunctionHandler RetrieveAudit = (primary, service, tracing, organizationConfig, parameters) =>
        {
            var firstParam = parameters.FirstOrDefault()?.Value;
            var reference = (firstParam as Entity)?.ToEntityReference() ?? firstParam as EntityReference;
            var config = GetConfig(parameters);

            if (firstParam != null && reference == null)
            {
                throw new InvalidPluginExecutionException("RetrieveAudit: First Parameter must be an Entity or EntityReference");
            }

            if (reference == null)
            {
                return new ValueExpression(string.Empty, null);
            }

            var field = CheckedCast<string>(parameters[1]?.Value, "RetrieveAudit: fieldName must be a string");

            var request = new RetrieveRecordChangeHistoryRequest
            {
                Target = reference
            };
            var audit = service.Execute(request) as RetrieveRecordChangeHistoryResponse;

            var auditValue = audit.AuditDetailCollection.AuditDetails.Select(d =>
            {
                var detail = d as AttributeAuditDetail;

                if (detail == null)
                {
                    return null;
                }

                var oldValue = detail.OldValue.GetAttributeValue<object>(field);
                
                return Tuple.Create(PropertyStringifier.Stringify(field, detail.OldValue, service, config), oldValue);
            })
            .FirstOrDefault(t => t != null);
            
            return new ValueExpression(auditValue?.Item1 ?? string.Empty, auditValue?.Item2);
        };

        public static FunctionHandler GetRecordId = (primary, service, tracing, organizationConfig, parameters) =>
        {
            var firstParam = parameters.FirstOrDefault()?.Value;
            var reference = (firstParam as Entity)?.ToEntityReference() ?? firstParam as EntityReference;
            var config = GetConfig(parameters);

            if (firstParam != null && reference == null)
            {
                throw new InvalidPluginExecutionException("RecordId: First Parameter must be an Entity or EntityReference");
            }

            if (reference == null)
            {
                return new ValueExpression(string.Empty, null);
            }

            var textValue = reference.Id.ToString(config.GetValue<string>("format", "format must be a string", "D"));

            return new ValueExpression(textValue, reference.Id);
        };

        public static FunctionHandler GetRecordLogicalName = (primary, service, tracing, organizationConfig, parameters) =>
        {
            var firstParam = parameters.FirstOrDefault()?.Value;
            var reference = (firstParam as Entity)?.ToEntityReference() ?? firstParam as EntityReference;

            if (firstParam != null && reference == null)
            {
                throw new InvalidPluginExecutionException("RecordLogicalName: First Parameter must be an Entity or EntityReference");
            }

            if (reference == null)
            {
                return new ValueExpression(string.Empty, null);
            }

            return new ValueExpression(reference.LogicalName, reference.LogicalName);
        };

        public static FunctionHandler GptPrompt = (primary, service, tracing, organizationConfig, parameters) =>
        {
            if (organizationConfig == null || string.IsNullOrEmpty(organizationConfig.OpenAIAccessToken))
            {
                throw new InvalidPluginExecutionException("GptPrompt can't find the OpenAI access token inside the plugin step secure configuration. Please add it.");
            }

            if (parameters.Count < 1)
            {
                throw new InvalidPluginExecutionException("GptPrompt needs an input string and optionally a config for defining further options");
            }

            var prompt = CheckedCast<string>(parameters.FirstOrDefault()?.Value, "You need to pass a prompt text (string) for GPT!");

            var config = GetConfig(parameters);
            var model = config.GetValue<string>("model", "model must be a string!");
            var temperature = config.GetValue<int>("temperature", "temperature must be an int!");
            var maxTokens = config.GetValue<int>("maxTokens", "maxTokens must be an int!");
            var stop = config.GetValue<List<ValueExpression>>("stop", "stop must be an array!");

            var stopSequences = stop?.Select(i => i.Text)?.ToList();

            var gptRequest = new GptRequest
            {
                Model = model ?? "text-davinci-003",
                Temperature = temperature,
                MaxTokens = maxTokens,
                Prompt = prompt,
                Stop = stopSequences
            };

            var httpClient = _httpClient.Value;

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", organizationConfig.OpenAIAccessToken);

            var jsonRequest = GenericJsonSerializer.Serialize(gptRequest);

            tracing.Trace("Sending request to GPT: " + jsonRequest);

            request.Content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = httpClient.SendAsync(request).Result;

            if (response.IsSuccessStatusCode)
            {
                var content = response.Content.ReadAsStringAsync().Result;

                tracing.Trace("Response from GPT: " + content);

                var gptResponse = GenericJsonSerializer.Deserialize<GptResponse>(content);

                var choice = gptResponse.Choices?.FirstOrDefault();

                return new ValueExpression(choice?.Text, choice?.Text);
            }
            else
            {
                tracing.Trace($"Request not successful: {response.StatusCode}. Message: {response.Content.ReadAsStringAsync().Result}");
                return new ValueExpression(string.Empty, string.Empty);
            }
        };
    }
}
#pragma warning restore S1104 // Fields should not have public accessibility
