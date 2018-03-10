using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
            var tracing = serviceProvider.GetService(typeof(ITracingService)) as ITracingService;
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

        private void HandleCustomAction(IPluginExecutionContext context, ITracingService tracing, IOrganizationService service)
        {
            var config = ProcessorConfig.Parse(context.InputParameters["jsonInput"] as string);
            
            if (config.Target == null)
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

            var dataSource = service.Retrieve(config.Target.LogicalName, config.Target.Id, columnSet);
            var output = ProcessTemplate(tracing, service, dataSource, config.Template);

            context.OutputParameters.Add("jsonOutput", output);
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

            if (!CheckExecutionCriteria(dataSource, service, tracing))
            {
                return;
            }

            ValidateConfig(targetField, template, templateField);
            var templateText = RetrieveTemplate(template, templateField, dataSource);

            templateText = ProcessTemplate(tracing, service, dataSource, templateText);

            target[targetField] = templateText;
        }

        private string ProcessTemplate(ITracingService tracing, IOrganizationService service, Entity dataSource, string templateText)
        {
            var tokenRegex = new Regex(@"\${{(.*?(?=}}))}}", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);

            var tokens = tokenRegex.Matches(templateText)
                .Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToList();

            tokens.ForEach(token =>
            {
                var processor = new XTLInterpreter(token, dataSource, _organizationConfig, service, tracing);
                var processed = string.Empty;

                try
                {
                    processed = processor.Produce();
                }
                catch (Exception ex)
                {
                    tracing.Trace($"Exception while processing token {token}, replacing by empty string. Message: {ex.Message}");
                }

                templateText = templateText.Replace($"${{{{{token}}}}}", processed);
            });
            return templateText;
        }

        private static string RetrieveTemplate(string template, string templateField, Entity dataSource)
        {
            string templateText;
            if (!string.IsNullOrEmpty(template))
            {
                templateText = template;
            }
            else
            {
                templateText = dataSource.GetAttributeValue<string>(templateField);
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

        private bool CheckExecutionCriteria(Entity dataSource, IOrganizationService service, ITracingService tracing)
        {
            if (!string.IsNullOrEmpty(_config.ExecutionCriteria))
            {
                var criteriaInterpreter = new XTLInterpreter(_config.ExecutionCriteria, dataSource, _organizationConfig, service, tracing);
                var result = criteriaInterpreter.Produce();

                var criteriaMatched = false;
                bool.TryParse(result, out criteriaMatched);

                if (!criteriaMatched)
                {
                    tracing.Trace($"Execution criteria {_config.ExecutionCriteria} did not match, aborting.");
                    return false;
                }

                return true;
            }

            return true;
        }

        private Entity GenerateDataSource(IPluginExecutionContext context, Entity target)
        {
            // "Merge" pre entity images with targets for having all attribute values
            var dataSource = new Entity();

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
