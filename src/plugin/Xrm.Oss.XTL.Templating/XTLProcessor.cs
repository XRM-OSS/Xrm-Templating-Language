using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Xrm.Oss.XTL.Interpreter;

namespace Xrm.Oss.XTL.Templating
{
    public class XTLProcessor : IPlugin
    {
        private ProcessorConfig _config;
        private OrganizationConfig _organizationConfig;

        public XTLProcessor(): this("", "") { }

        public XTLProcessor (string unsecure, string secure)
        {
            _config = ProcessorConfig.Parse(unsecure);
            _organizationConfig = OrganizationConfig.Parse(secure);
        }

        public void Execute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetService(typeof(IPluginExecutionContext)) as IPluginExecutionContext;
            var crmTracing = serviceProvider.GetService(typeof(ITracingService)) as ITracingService;
            var tracing = new PersistentTracingService(crmTracing);
            var serviceFactory = serviceProvider.GetService(typeof(IOrganizationServiceFactory)) as IOrganizationServiceFactory;
            var service = serviceFactory.CreateOrganizationService(null);

            if (context.InputParameters.ContainsKey("jsonInput"))
            {
                HandleCustomAction(context, tracing, service);
            }
            else
            {
                HandleNonCustomAction(context, tracing, service);
            }
        }

        private void TriggerUpdateConditionally(string newValue, Entity target, ProcessorConfig config, IOrganizationService service)
        {
            if (!config.TriggerUpdate)
            {
                return;
            }

            if (string.IsNullOrEmpty(config.TargetField))
            {
                throw new InvalidPluginExecutionException("Target field is required when setting the 'triggerUpdate' flag");
            }

            if (config.ForceUpdate || !string.Equals(target.GetAttributeValue<string>(config.TargetField), newValue))
            {
                var updateObject = new Entity
                {
                    LogicalName = target.LogicalName,
                    Id = target.Id
                };

                updateObject[config.TargetField] = newValue;

                service.Update(updateObject);
            }
        }

        private void HandleCustomAction(IPluginExecutionContext context, PersistentTracingService tracing, IOrganizationService service)
        {
            var config = ProcessorConfig.Parse(context.InputParameters["jsonInput"] as string);
            
            if (config.Target == null && config.TargetEntity == null)
            {
                throw new InvalidPluginExecutionException("Target property inside JSON parameters is needed for custom actions");
            }

            ColumnSet columnSet;

            if (config.TargetColumns != null)
            {
                columnSet = new ColumnSet(config.TargetColumns);
            }
            else
            {
                columnSet = new ColumnSet(true);
            }

            try
            {
                var dataSource = config.TargetEntity != null ? config.TargetEntity : service.Retrieve(config.Target.LogicalName, config.Target.Id, columnSet);

                if (!CheckExecutionCriteria(config, dataSource, service, tracing))
                {
                    tracing.Trace("Execution criteria not met, aborting");

                    var abortResult = new ProcessingResult
                    {
                        Success = true,
                        Result = config.Template,
                        TraceLog = tracing.TraceLog
                    };
                    context.OutputParameters["jsonOutput"] = SerializeResult(abortResult);

                    return;
                }

                var templateText = RetrieveTemplate(config.Template, config.TemplateField, dataSource, service, tracing);

                if (string.IsNullOrEmpty(templateText))
                {
                    tracing.Trace("Template is empty, aborting");
                    return;
                }

                var output = TokenMatcher.ProcessTokens(templateText, dataSource, new OrganizationConfig { OrganizationUrl = config.OrganizationUrl }, service, tracing);

                var result = new ProcessingResult
                {
                    Success = true,
                    Result = output,
                    TraceLog = tracing.TraceLog
                };
                context.OutputParameters["jsonOutput"] = SerializeResult(result);

                TriggerUpdateConditionally(output, dataSource, config, service);
            }
            catch (Exception ex)
            {
                var result = new ProcessingResult
                {
                    Success = false,
                    Error = ex.Message,
                    TraceLog = tracing.TraceLog
                };
                context.OutputParameters["jsonOutput"] = SerializeResult(result);

                if (config.ThrowOnCustomActionError)
                {
                    throw;
                }
            }
        }

        private string SerializeResult(ProcessingResult result)
        {
            var serializer = new DataContractJsonSerializer(typeof(ProcessingResult));

            using (var memoryStream = new MemoryStream())
            {
                serializer.WriteObject(memoryStream, result);

                memoryStream.Position = 0;

                using (var streamReader = new StreamReader(memoryStream))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }

        private void HandleNonCustomAction(IPluginExecutionContext context, ITracingService tracing, IOrganizationService service)
        {
            var target = context.InputParameters.ContainsKey("Target") ? context.InputParameters["Target"] as Entity : null;

            if (target == null)
            {
                return;
            }

            var targetField = _config.TargetField;
            var template = _config.Template;
            var templateField = _config.TemplateField;

            var dataSource = GenerateDataSource(context, target);

            if (!CheckExecutionCriteria(_config, dataSource, service, tracing))
            {
                tracing.Trace("Execution criteria not met, aborting");
                return;
            }

            ValidateConfig(targetField, template, templateField);
            var templateText = RetrieveTemplate(template, templateField, dataSource, service, tracing);

            if (string.IsNullOrEmpty(templateText))
            {
                tracing.Trace("Template is empty, aborting");
                return;
            }

            var output = TokenMatcher.ProcessTokens(templateText, dataSource, _organizationConfig, service, tracing);

            target[targetField] = output;
            TriggerUpdateConditionally(output, dataSource, _config, service);
        }

        private static string RetrieveTemplate(string template, string templateField, Entity dataSource, IOrganizationService service, ITracingService tracing)
        {
            string templateText;

            if (!string.IsNullOrEmpty(template))
            {
                templateText = template;
            }
            else if (!string.IsNullOrEmpty(templateField))
            {
                if (new Regex("^[a-zA-Z_0-9]*$").IsMatch(templateField))
                {
                    templateText = dataSource.GetAttributeValue<string>(templateField);
                }
                else
                {
                    templateText = new XTLInterpreter(templateField, dataSource, null, service, tracing).Produce();
                }
            }
            else
            {
                throw new InvalidDataException("You must either pass a template text or define a template field");
            }


            // Templates inside e-mails will be HTML encoded
            templateText = WebUtility.HtmlDecode(templateText);
            return templateText;
        }

        private void ValidateConfig(string targetField, string template, string templateField)
        {
            if (string.IsNullOrEmpty(targetField))
            {
                throw new InvalidPluginExecutionException("Target field was null, please adapt the unsecure config!");
            }

            if (string.IsNullOrEmpty(template) && string.IsNullOrEmpty(templateField))
            {
                throw new InvalidPluginExecutionException("Both template and template field were null, please set one of them in the unsecure config!");
            }
        }

        private bool CheckExecutionCriteria(ProcessorConfig config, Entity dataSource, IOrganizationService service, ITracingService tracing)
        {
            if (!string.IsNullOrEmpty(config.ExecutionCriteria))
            {
                var criteriaInterpreter = new XTLInterpreter(config.ExecutionCriteria, dataSource, _organizationConfig, service, tracing);
                var result = criteriaInterpreter.Produce();

                var criteriaMatched = false;
                bool.TryParse(result, out criteriaMatched);

                if (!criteriaMatched)
                {
                    return false;
                }

                return true;
            }

            return true;
        }

        private Entity GenerateDataSource(IPluginExecutionContext context, Entity target)
        {
            // "Merge" pre entity images with targets for having all attribute values
            var dataSource = new Entity
            {
                LogicalName = target.LogicalName,
                Id = target.Id
            };

            foreach (var image in context.PreEntityImages.Values)
            {
                foreach (var property in image.Attributes)
                {
                    dataSource[property.Key] = property.Value;
                }
            }

            foreach (var property in target.Attributes)
            {
                dataSource[property.Key] = property.Value;
            }

            return dataSource;
        }
    }
}
