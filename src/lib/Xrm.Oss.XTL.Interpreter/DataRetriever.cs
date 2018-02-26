using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xrm.Sdk;

namespace Xrm.Oss.XTL.Interpreter
{
    public static class DataRetriever
    {
        public static string ResolveTokenText(string token, Entity primary, IOrganizationService service)
        {
            return ResolveToken(token, primary, service);
        }

        public static string ResolveTokenValue(string token, Entity primary, IOrganizationService service)
        {
            return ResolveToken(token, primary, service);
        }

        private static string ResolveToken(string token, Entity primary, IOrganizationService service)
        {
            return primary.GetAttributeValue<string>(token);
        }
    }
}
