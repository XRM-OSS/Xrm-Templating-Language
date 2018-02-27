using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FakeXrmEasy;
using Microsoft.Xrm.Sdk;
using NUnit.Framework;

namespace Xrm.Oss.XTL.Templating.Tests
{
    [TestFixture]
    public class XTLProcessorTests
    {
        [Test]
        public void It_Should_Replace_Simple_Formula()
        {
            var context = new XrmFakedContext();

            var email = new Entity
            {
                Id = Guid.NewGuid(),
                LogicalName = "email",
                Attributes =
                {
                    { "subject", "Demo" },
                    { "description", "Hello ${Text(\"subject\")}" }
                }
            };

            var pluginContext = context.GetDefaultPluginContext();
            pluginContext.InputParameters = new ParameterCollection
            {
                { "Target", email }
            };

            var config = @"{ ""targetField"": ""description"",  ""templateField"": ""description"" }";
            context.ExecutePluginWithConfigurations<XTLProcessor>(pluginContext, config, string.Empty);

            Assert.That(email.GetAttributeValue<string>("description"), Is.EqualTo("Hello Demo"));
        }
    }
}
