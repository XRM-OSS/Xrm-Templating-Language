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
                    { "description", "Hello ${{Value(\"subject\")}}" }
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

        [Test]
        public void It_Should_Preserve_Text()
        {
            var context = new XrmFakedContext();

            var email = new Entity
            {
                Id = Guid.NewGuid(),
                LogicalName = "email",
                Attributes =
                {
                    { "subject", "Demo" },
                    { "description", "Hello ${{If( IsNull ( \"{0}\" ), \"\", \"\")}}${{Value(\"subject\")}} ${{Value(\"subject\")}}" }
                }
            };

            var pluginContext = context.GetDefaultPluginContext();
            pluginContext.InputParameters = new ParameterCollection
            {
                { "Target", email }
            };

            var config = @"{ ""targetField"": ""description"",  ""templateField"": ""description"" }";
            context.ExecutePluginWithConfigurations<XTLProcessor>(pluginContext, config, string.Empty);

            Assert.That(email.GetAttributeValue<string>("description"), Is.EqualTo("Hello Demo Demo"));
        }

        [Test]
        public void It_Should_Replace_Invalid_Placeholder_By_Empty_String()
        {
            var context = new XrmFakedContext();

            var email = new Entity
            {
                Id = Guid.NewGuid(),
                LogicalName = "email",
                Attributes =
                {
                    { "subject", "Demo" },
                    { "description", "Hello ${{Value(\"subject)}}" }
                }
            };

            var pluginContext = context.GetDefaultPluginContext();
            pluginContext.InputParameters = new ParameterCollection
            {
                { "Target", email }
            };

            var config = @"{ ""targetField"": ""description"",  ""templateField"": ""description"" }";
            Assert.That(() => context.ExecutePluginWithConfigurations<XTLProcessor>(pluginContext, config, string.Empty), Throws.Nothing);

            Assert.That(email.GetAttributeValue<string>("description"), Is.EqualTo("Hello "));
        }

        [Test]
        public void It_Should_Not_Throw_But_Replace_By_Empty_String_On_Error()
        {
            var context = new XrmFakedContext();

            var email = new Entity
            {
                Id = Guid.NewGuid(),
                LogicalName = "email",
                Attributes =
                {
                    { "subject", "Demo" },
                    { "description", "Hello ${{Text \"subject\"}} ${{Value(\"subject\")}}" }
                }
            };

            var pluginContext = context.GetDefaultPluginContext();
            pluginContext.InputParameters = new ParameterCollection
            {
                { "Target", email }
            };

            var config = @"{ ""targetField"": ""description"",  ""templateField"": ""description"" }";
            Assert.That(() => context.ExecutePluginWithConfigurations<XTLProcessor>(pluginContext, config, string.Empty), Throws.Nothing);
            Assert.That(email.GetAttributeValue<string>("description"), Is.EqualTo("Hello  Demo"));
        }

        [Test]
        public void It_Should_Abort_If_Execution_Criteria_Set_And_No_Match()
        {
            var context = new XrmFakedContext();

            var email = new Entity
            {
                Id = Guid.NewGuid(),
                LogicalName = "email",
                Attributes =
                {
                    { "subject", "Demo" },
                    { "description", "Hello ${{Value(\"subject\")}}" }
                }
            };

            var pluginContext = context.GetDefaultPluginContext();
            pluginContext.InputParameters = new ParameterCollection
            {
                { "Target", email }
            };
            
            var config = @"{ ""targetField"": ""description"",  ""templateField"": ""description"", ""executionCriteria"": ""IsNull(Value(\""subject\""))"" }";
            context.ExecutePluginWithConfigurations<XTLProcessor>(pluginContext, config, string.Empty);

            Assert.That(email.GetAttributeValue<string>("description"), Is.EqualTo("Hello ${{Value(\"subject\")}}"));
        }

        [Test]
        public void It_Should_Proceed_If_Execution_Criteria_Set_And_Matches()
        {
            var context = new XrmFakedContext();

            var email = new Entity
            {
                Id = Guid.NewGuid(),
                LogicalName = "email",
                Attributes =
                {
                    { "subject", "Demo" },
                    { "description", "Hello ${{Value(\"subject\")}}" }
                }
            };

            var pluginContext = context.GetDefaultPluginContext();
            pluginContext.InputParameters = new ParameterCollection
            {
                { "Target", email }
            };

            var config = @"{ ""targetField"": ""description"",  ""templateField"": ""description"", ""executionCriteria"": ""Not(IsNull(Value(\""subject\"")))"" }";
            context.ExecutePluginWithConfigurations<XTLProcessor>(pluginContext, config, string.Empty);

            Assert.That(email.GetAttributeValue<string>("description"), Is.EqualTo("Hello Demo"));
        }

        [Test]
        public void It_Should_Parse_Organization_Config()
        {
            var context = new XrmFakedContext();

            var contact = new Entity
            {
                LogicalName = "contact",
                Id = Guid.NewGuid()
            };

            var email = new Entity
            {
                Id = Guid.NewGuid(),
                LogicalName = "email",
                Attributes =
                {
                    { "subject", "Demo" },
                    { "description", "Hello ${{RecordUrl(Value(\"regardingobjectid\"))}}" },
                    { "regardingobjectid", contact.ToEntityReference() }
                }
            };

            context.Initialize(new[] { contact, email });

            var pluginContext = context.GetDefaultPluginContext();
            pluginContext.InputParameters = new ParameterCollection
            {
                { "Target", email }
            };

            var config = @"{ ""targetField"": ""description"",  ""templateField"": ""description"" }";
            var orgConfig = @"{ ""organizationUrl"": ""https://someUrl/"" }";
            context.ExecutePluginWithConfigurations<XTLProcessor>(pluginContext, config, orgConfig);

            Assert.That(email.GetAttributeValue<string>("description"), Is.EqualTo($"Hello <a href=\"https://someUrl/main.aspx?etn={contact.LogicalName}&id={contact.Id}&newWindow=true&pagetype=entityrecord\">https://someUrl/main.aspx?etn={contact.LogicalName}&id={contact.Id}&newWindow=true&pagetype=entityrecord</a>"));
        }
    }
}
