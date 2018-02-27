using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Xrm.Oss.XTL.Interpreter;

namespace Xrm.Oss.XTL.Templating
{
    public class XTLProcessor : IPlugin
    {
        private ProcessorConfig _config;

        public XTLProcessor (string unsecure, string secure)
        {
            _config = ProcessorConfig.Parse(unsecure);
        }

        public void Execute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetService(typeof(IPluginExecutionContext)) as IPluginExecutionContext;
            var serviceFactory = serviceProvider.GetService(typeof(IOrganizationServiceFactory)) as IOrganizationServiceFactory;
            var service = serviceFactory.CreateOrganizationService(null);

            var targetField = _config.TargetField;
            var template = _config.Template;
            var templateField = _config.TemplateField;

            var target = context.InputParameters["Target"] as Entity;

            string templateText;

            if (target == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(targetField))
            {
                throw new InvalidPluginExecutionException("Target field was null, please adapt the unsecure config!");
            }

            if (string.IsNullOrEmpty(template) && string.IsNullOrEmpty(templateField))
            {
                throw new InvalidPluginExecutionException("Both template and template field were null, please set one of them in the unsecure config!");
            }

            if (!string.IsNullOrEmpty(template))
            {
                templateText = template;
            }
            else
            {
                templateText = target.GetAttributeValue<string>(templateField);
            }

            var tokenRegex = new Regex(@"\$\{(.*)?(?=\})\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

            var tokens = tokenRegex.Matches(templateText)
                .Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToList();

            tokens.ForEach(token =>
            {
                var processor = new XTLInterpreter(token, target, service);

                var processed = processor.Produce();
                templateText = templateText.Replace($"${{{token}}}", processed);
            });

            target[targetField] = templateText;
        }
    }
}
