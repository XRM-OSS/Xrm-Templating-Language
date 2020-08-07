using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Xrm.Oss.XTL.Interpreter
{
    public static class TokenMatcher
    {
        private static Regex tokenRegex = new Regex(@"\${{([\s\S]*?(?=}}))}}", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);

        public static List<string> MatchTokens(string templateText)
        {
            var tokens = tokenRegex.Matches(templateText)
                .Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToList();

            return tokens;
        }

        public static string ProcessTokens(string templateText, Entity dataSource, OrganizationConfig config, IOrganizationService service, ITracingService tracing)
        {
            var tokens = MatchTokens(templateText);

            tokens.ForEach(token =>
            {
                tracing.Trace($"Processing token '{token}'");

                var processor = new XTLInterpreter(token, dataSource, config, service, tracing);
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
                tracing.Trace($"Replacing token with '{processed}'");
            });

            return templateText;
        }
    }
}
