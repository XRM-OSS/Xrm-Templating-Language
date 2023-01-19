using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FakeItEasy;
using FakeXrmEasy;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
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
        public void It_Should_Retrieve_Values_From_Preimages()
        {
            var context = new XrmFakedContext();

            var email = new Entity
            {
                Id = Guid.NewGuid(),
                LogicalName = "email",
                Attributes =
                {
                    { "description", "Hello ${{Value(\"subject\")}}" }
                }
            };

            var pluginContext = context.GetDefaultPluginContext();
            pluginContext.InputParameters = new ParameterCollection
            {
                { "Target", email }
            };
            pluginContext.PreEntityImages = new EntityImageCollection
            {
                { "preimg", new Entity
                    {
                        Attributes =
                        {
                            { "subject", "Demo Pre" }
                        }
                    }
                }
            };

            var config = @"{ ""targetField"": ""description"",  ""templateField"": ""description"" }";
            context.ExecutePluginWithConfigurations<XTLProcessor>(pluginContext, config, string.Empty);

            Assert.That(email.GetAttributeValue<string>("description"), Is.EqualTo("Hello Demo Pre"));
        }

        [Test]
        public void It_Should_Allow_Expressions_Inside_Template_Fields()
        {
            var context = new XrmFakedContext();

            var account = new Entity
            {
                Id = Guid.NewGuid(),
                LogicalName = "account",
                Attributes =
                {
                    { "oss_template", "AccountTemplate - ${{Value(\"firstname\")}}" }
                }
            };

            var contact = new Entity
            {
                Id = Guid.NewGuid(),
                LogicalName = "contact",
                Attributes =
                {
                    { "firstname", "Frodo" },
                    { "parentcustomerid", account.ToEntityReference() }
                }
            };

            context.Initialize(new Entity[] { account, contact });
            var pluginContext = context.GetDefaultPluginContext();
            pluginContext.InputParameters = new ParameterCollection
            {
                { "Target", contact }
            };

            var config = @"{ ""targetField"": ""description"",  ""templateField"": ""Value(\""parentcustomerid.oss_template\"")"" }";
            context.ExecutePluginWithConfigurations<XTLProcessor>(pluginContext, config, string.Empty);

            Assert.That(contact.GetAttributeValue<string>("description"), Is.EqualTo("AccountTemplate - Frodo"));
        }

        [Test]
        public void It_Should_Not_Fail_On_Empty_Valued_Template_Field()
        {
            var context = new XrmFakedContext();

            var email= new Entity
            {
                Id = Guid.NewGuid(),
                LogicalName = "email"
            };

            context.Initialize(new Entity[] { email });
            var pluginContext = context.GetDefaultPluginContext();
            pluginContext.InputParameters = new ParameterCollection
            {
                { "Target", email }
            };

            var config = @"{ ""targetField"": ""description"",  ""templateField"": ""description"" }";
            Assert.That(() => context.ExecutePluginWithConfigurations<XTLProcessor>(pluginContext, config, string.Empty), Throws.Nothing);
        }

        [Test]
        public void It_Should_Fail_If_No_Template_Passed()
        {
            var context = new XrmFakedContext();

            var contact = new Entity
            {
                Id = Guid.NewGuid(),
                LogicalName = "contact",
                Attributes =
                {
                    { "firstname", "Frodo" }
                }
            };

            context.Initialize(new Entity[] { contact });
            var pluginContext = context.GetDefaultPluginContext();
            pluginContext.InputParameters = new ParameterCollection
            {
                { "Target", contact }
            };

            var config = @"{ ""targetField"": ""description"" }";
            Assert.That(() => context.ExecutePluginWithConfigurations<XTLProcessor>(pluginContext, config, string.Empty), Throws.Exception.With.Message.EqualTo("Both template and template field were null, please set one of them in the unsecure config!"));
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
                    { "description", "Hello ${{Value(\"subject\")}} ${{Value(\"subject\")}}" }
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
        public void It_Should_Preserve_Whitespace()
        {
            var context = new XrmFakedContext();

            var email = new Entity
            {
                Id = Guid.NewGuid(),
                LogicalName = "email",
                Attributes =
                {
                    { "name", "Demo" },
                    { "description", "Hi Tester,\n\n${{\nValue(\"name\")}}" }
                }
            };

            var pluginContext = context.GetDefaultPluginContext();
            pluginContext.InputParameters = new ParameterCollection
            {
                { "Target", email }
            };

            var config = @"{ ""targetField"": ""description"",  ""templateField"": ""description"" }";
            context.ExecutePluginWithConfigurations<XTLProcessor>(pluginContext, config, string.Empty);

            Assert.That(email.GetAttributeValue<string>("description"), Is.EqualTo("Hi Tester,\n\nDemo"));
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

        [Test]
        public void It_Should_Execute_On_Custom_Action()
        {
            var context = new XrmFakedContext();
            var template = "Hello ${{Value(\\\"subject\\\")}}";

            var email = new Entity
            {
                Id = Guid.NewGuid(),
                LogicalName = "email",
                Attributes =
                {
                    { "subject", "Demo" }
                }
            };

            context.Initialize(email);

            var inputParameters = new ParameterCollection
            {
                { "jsonInput", "{" +
                $"\"template\": \"{template}\"," +
                $"\"target\": {{\"Id\": \"{email.Id}\", \"LogicalName\": \"{email.LogicalName}\"}}" +
                "}" }
            };

            var pluginContext = context.GetDefaultPluginContext();
            pluginContext.InputParameters = inputParameters;
            pluginContext.OutputParameters = new ParameterCollection();

            context.ExecutePluginWith<XTLProcessor>(pluginContext);

            var expected = @"{""error"":null,""result"":""Hello Demo"",""success"":true,""traceLog"":""Processing token 'Value(\""subject\"")'\u000d\u000aInitiating interpreter\u000d\u000aProcessing handler Value\u000d\u000aSuccessfully processed handler Value\u000d\u000aReplacing token with 'Demo'\u000d\u000a""}";
            Assert.That(pluginContext.OutputParameters["jsonOutput"], Is.EqualTo(expected));
        }

        [Test]
        public void It_Should_Use_Organization_Url_On_Custom_Action()
        {
            var context = new XrmFakedContext();
            var template = "Hello ${{RecordUrl(PrimaryRecord())}}";

            var email = new Entity
            {
                Id = new Guid("e8ac0401-1078-471d-a59e-c879a8d8ef23"),
                LogicalName = "email",
                Attributes =
                {
                    { "subject", "Demo" }
                }
            };

            context.Initialize(email);

            var inputParameters = new ParameterCollection
            {
                { "jsonInput", "{" +
                $"\"template\": \"{template}\"," +
                "\"organizationUrl\": \"https://crmOrg/\"," +
                "\"throwOnCustomActionError\": true," +
                $"\"target\": {{\"Id\": \"{email.Id}\", \"LogicalName\": \"{email.LogicalName}\"}}" +
                "}" }
            };

            var pluginContext = context.GetDefaultPluginContext();
            pluginContext.InputParameters = inputParameters;
            pluginContext.OutputParameters = new ParameterCollection();

            context.ExecutePluginWith<XTLProcessor>(pluginContext);

            var expected = @"{""error"":null,""result"":""Hello <a href=\""https:\/\/crmOrg\/main.aspx?etn=email&id=e8ac0401-1078-471d-a59e-c879a8d8ef23&newWindow=true&pagetype=entityrecord\"">https:\/\/crmOrg\/main.aspx?etn=email&id=e8ac0401-1078-471d-a59e-c879a8d8ef23&newWindow=true&pagetype=entityrecord<\/a>"",""success"":true,""traceLog"":""Processing token 'RecordUrl(PrimaryRecord())'\u000d\u000aInitiating interpreter\u000d\u000aProcessing handler RecordUrl\u000d\u000aProcessing handler PrimaryRecord\u000d\u000aSuccessfully processed handler PrimaryRecord\u000d\u000aSuccessfully processed handler RecordUrl\u000d\u000aReplacing token with '<a href=\""https:\/\/crmOrg\/main.aspx?etn=email&id=e8ac0401-1078-471d-a59e-c879a8d8ef23&newWindow=true&pagetype=entityrecord\"">https:\/\/crmOrg\/main.aspx?etn=email&id=e8ac0401-1078-471d-a59e-c879a8d8ef23&newWindow=true&pagetype=entityrecord<\/a>'\u000d\u000a""}";
            Assert.That(pluginContext.OutputParameters["jsonOutput"], Is.EqualTo(expected));
        }

        [Test]
        public void It_Should_Send_Organization_Service_Update_With_Trigger_Update_Flag()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();

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

            var config = @"{ ""targetField"": ""description"",  ""templateField"": ""description"", ""triggerUpdate"": true }";
            context.Initialize(email);
            context.ExecutePluginWithConfigurations<XTLProcessor>(pluginContext, config, string.Empty);

            email = service.Retrieve(email.LogicalName, email.Id, new ColumnSet(true));

            Assert.That(email.GetAttributeValue<string>("description"), Is.EqualTo("Hello Demo"));
        }

        [Test]
        public void It_Should_Not_Send_Organization_Service_Update_Without_Trigger_Update_Flag()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();

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
            context.Initialize(email);
            context.ExecutePluginWithConfigurations<XTLProcessor>(pluginContext, config, string.Empty);

            email = service.Retrieve(email.LogicalName, email.Id, new ColumnSet(true));

            Assert.That(email.GetAttributeValue<string>("description"), Is.EqualTo("Hello ${{Value(\"subject\")}}"));
            A.CallTo(() => service.Update(A<Entity>._)).MustHaveHappened(Repeated.Never);
        }

        [Test]
        public void It_Should_Not_Send_Organization_Service_Update_If_Value_Identical_And_No_Force_Flag_Set()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();

            var email = new Entity
            {
                Id = Guid.NewGuid(),
                LogicalName = "email",
                Attributes =
                {
                    { "subject", "Demo" },
                    { "description", "Hello Demo" }
                }
            };

            var pluginContext = context.GetDefaultPluginContext();
            pluginContext.InputParameters = new ParameterCollection
            {
                { "Target", email }
            };

            var config = @"{ ""targetField"": ""description"",  ""templateField"": ""description"", ""triggerUpdate"": true }";
            context.Initialize(email);
            context.ExecutePluginWithConfigurations<XTLProcessor>(pluginContext, config, string.Empty);

            email = service.Retrieve(email.LogicalName, email.Id, new ColumnSet(true));

            Assert.That(email.GetAttributeValue<string>("description"), Is.EqualTo("Hello Demo"));
            A.CallTo(() => service.Update(A<Entity>._)).MustHaveHappened(Repeated.Never);
        }

        [Test]
        public void It_Should_Send_Organization_Service_Update_If_Value_Identical_And_Force_Flag_Set()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();

            var email = new Entity
            {
                Id = Guid.NewGuid(),
                LogicalName = "email",
                Attributes =
                {
                    { "subject", "Demo" },
                    { "description", "Hello Demo" }
                }
            };

            var pluginContext = context.GetDefaultPluginContext();
            pluginContext.InputParameters = new ParameterCollection
            {
                { "Target", email }
            };

            var config = @"{ ""targetField"": ""description"",  ""templateField"": ""description"", ""triggerUpdate"": true, ""forceUpdate"": true }";
            context.Initialize(email);
            context.ExecutePluginWithConfigurations<XTLProcessor>(pluginContext, config, string.Empty);

            email = service.Retrieve(email.LogicalName, email.Id, new ColumnSet(true));

            Assert.That(email.GetAttributeValue<string>("description"), Is.EqualTo("Hello Demo"));
            A.CallTo(() => service.Update(A<Entity>._)).MustHaveHappened(Repeated.Exactly.Once);
        }

        [Test]
        public void It_Should_Trigger_Update_On_Custom_Action_If_Flag_Set()
        {
            var context = new XrmFakedContext();
            var template = "Hello ${{Value(\\\"subject\\\")}}";

            var email = new Entity
            {
                Id = Guid.NewGuid(),
                LogicalName = "email",
                Attributes =
                {
                    { "subject", "Demo" }
                }
            };

            context.Initialize(email);

            var inputParameters = new ParameterCollection
            {
                { "jsonInput", "{" +
                "\"triggerUpdate\": true," +
                "\"targetField\": \"description\"," +
                $"\"template\": \"{template}\"," +
                $"\"target\": {{\"Id\": \"{email.Id}\", \"LogicalName\": \"{email.LogicalName}\"}}" +
                "}" }
            };

            var pluginContext = context.GetDefaultPluginContext();
            pluginContext.InputParameters = inputParameters;
            pluginContext.OutputParameters = new ParameterCollection();

            context.ExecutePluginWith<XTLProcessor>(pluginContext);

            email = context.GetFakedOrganizationService().Retrieve(email.LogicalName, email.Id, new ColumnSet(true));

            Assert.That(email["description"], Is.EqualTo("Hello Demo"));
        }

        [Test]
        public void It_Should_Use_ColumnSet_If_Set_On_Custom_Action()
        {
            var context = new XrmFakedContext();
            var template = "Hello ${{Value(\\\"subject\\\")}}";

            var email = new Entity
            {
                Id = Guid.NewGuid(),
                LogicalName = "email",
                Attributes =
                {
                    { "subject", "Demo" }
                }
            };

            context.Initialize(email);

            var inputParameters = new ParameterCollection
            {
                { "jsonInput", "{" +
                "\"triggerUpdate\": true," +
                "\"targetField\": \"description\"," +
                "\"targetColumns\": [\"description\"]," +
                $"\"template\": \"{template}\"," +
                $"\"target\": {{\"Id\": \"{email.Id}\", \"LogicalName\": \"{email.LogicalName}\"}}" +
                "}" }
            };

            var pluginContext = context.GetDefaultPluginContext();
            pluginContext.InputParameters = inputParameters;
            pluginContext.OutputParameters = new ParameterCollection();

            context.ExecutePluginWith<XTLProcessor>(pluginContext);

            email = context.GetFakedOrganizationService().Retrieve(email.LogicalName, email.Id, new ColumnSet(true));

            Assert.That(email["description"], Is.EqualTo("Hello "));
        }

        [Test]
        public void It_Should_Not_Send_Update_On_Custom_Action_If_Value_Identical_And_No_Flag_Set()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();

            var template = "Hello ${{Value(\\\"subject\\\")}}";

            var email = new Entity
            {
                Id = Guid.NewGuid(),
                LogicalName = "email",
                Attributes =
                {
                    { "subject", "Demo" },
                    { "description", "Hello Demo" }
                }
            };

            context.Initialize(email);

            var inputParameters = new ParameterCollection
            {
                { "jsonInput", "{" +
                "\"triggerUpdate\": true," +
                "\"targetField\": \"description\"," +
                $"\"template\": \"{template}\"," +
                $"\"target\": {{\"Id\": \"{email.Id}\", \"LogicalName\": \"{email.LogicalName}\"}}" +
                "}" }
            };

            var pluginContext = context.GetDefaultPluginContext();
            pluginContext.InputParameters = inputParameters;
            pluginContext.OutputParameters = new ParameterCollection();

            context.ExecutePluginWith<XTLProcessor>(pluginContext);

            email = service.Retrieve(email.LogicalName, email.Id, new ColumnSet(true));

            Assert.That(email["description"], Is.EqualTo("Hello Demo"));
            A.CallTo(() => service.Update(A<Entity>._)).MustHaveHappened(Repeated.Never);
        }

        [Test]
        public void It_Should_Send_Update_On_Custom_Action_If_Value_Identical_And_Flag_Set()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();

            var template = "Hello ${{Value(\\\"subject\\\")}}";

            var email = new Entity
            {
                Id = Guid.NewGuid(),
                LogicalName = "email",
                Attributes =
                {
                    { "subject", "Demo" },
                    { "description", "Hello Demo" }
                }
            };

            context.Initialize(email);

            var inputParameters = new ParameterCollection
            {
                { "jsonInput", "{" +
                "\"triggerUpdate\": true," +
                "\"forceUpdate\": true," +
                "\"targetField\": \"description\"," +
                $"\"template\": \"{template}\"," +
                $"\"target\": {{\"Id\": \"{email.Id}\", \"LogicalName\": \"{email.LogicalName}\"}}" +
                "}" }
            };

            var pluginContext = context.GetDefaultPluginContext();
            pluginContext.InputParameters = inputParameters;
            pluginContext.OutputParameters = new ParameterCollection();

            context.ExecutePluginWith<XTLProcessor>(pluginContext);

            email = service.Retrieve(email.LogicalName, email.Id, new ColumnSet(true));

            Assert.That(email["description"], Is.EqualTo("Hello Demo"));
            A.CallTo(() => service.Update(A<Entity>._)).MustHaveHappened(Repeated.Exactly.Once);
        }

        [Test]
        public void It_Should_Check_Execution_Criteria_On_Custom_Action()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();

            var template = "Hello ${{Value(\\\"subject\\\")}}";

            var email = new Entity
            {
                Id = Guid.NewGuid(),
                LogicalName = "email",
                Attributes =
                {
                    { "subject", "Demo" }
                }
            };

            context.Initialize(email);

            var inputParameters = new ParameterCollection
            {
                { "jsonInput", "{" +
                "\"triggerUpdate\": true," +
                "\"executionCriteria\": \"IsNull(Value(\\\"subject\\\"))\"," +
                "\"targetField\": \"description\"," +
                $"\"template\": \"{template}\"," +
                $"\"target\": {{\"Id\": \"{email.Id}\", \"LogicalName\": \"{email.LogicalName}\"}}" +
                "}" }
            };

            var pluginContext = context.GetDefaultPluginContext();
            pluginContext.InputParameters = inputParameters;
            pluginContext.OutputParameters = new ParameterCollection();

            context.ExecutePluginWith<XTLProcessor>(pluginContext);

            email = service.Retrieve(email.LogicalName, email.Id, new ColumnSet(true));
            A.CallTo(() => service.Update(A<Entity>._)).MustHaveHappened(Repeated.Never);
        }

        [Test]
        public void It_Should_Throw_If_Trigger_Update_Flag_Set_But_No_Target_Field()
        {
            var context = new XrmFakedContext();
            var template = "Hello ${{Value(\\\"subject\\\")}}";

            var email = new Entity
            {
                Id = Guid.NewGuid(),
                LogicalName = "email",
                Attributes =
                {
                    { "subject", "Demo" }
                }
            };

            context.Initialize(email);

            var inputParameters = new ParameterCollection
            {
                { "jsonInput", "{" +
                "\"triggerUpdate\": true," +
                "\"throwOnCustomActionError\": true," +
                $"\"template\": \"{template}\"," +
                $"\"target\": {{\"Id\": \"{email.Id}\", \"LogicalName\": \"{email.LogicalName}\"}}" +
                "}" }
            };

            var pluginContext = context.GetDefaultPluginContext();
            pluginContext.InputParameters = inputParameters;
            pluginContext.OutputParameters = new ParameterCollection();

            Assert.That(() => context.ExecutePluginWith<XTLProcessor>(pluginContext), Throws.TypeOf<InvalidPluginExecutionException>().With.Message.EqualTo("Target field is required when setting the 'triggerUpdate' flag"));
        }

        [Test]
        public void It_Should_Throw_On_Custom_Action_If_No_Target_Passed()
        {
            var context = new XrmFakedContext();
            var template = "Hello ${{Value(\\\"subject\\\")}}";

            var email = new Entity
            {
                Id = Guid.NewGuid(),
                LogicalName = "email",
                Attributes =
                {
                    { "subject", "Demo" }
                }
            };

            context.Initialize(email);

            var inputParameters = new ParameterCollection
            {
                { "jsonInput", "{" +
                "\"triggerUpdate\": true," +
                "\"throwOnCustomActionError\": true," +
                $"\"template\": \"{template}\"" +
                "}" }
            };

            var pluginContext = context.GetDefaultPluginContext();
            pluginContext.InputParameters = inputParameters;
            pluginContext.OutputParameters = new ParameterCollection();

            Assert.That(() => context.ExecutePluginWith<XTLProcessor>(pluginContext), Throws.TypeOf<InvalidPluginExecutionException>().With.Message.EqualTo("Target property inside JSON parameters is needed for custom actions"));
        }

        [Test]
        public void It_Should_Not_Throw_On_Custom_Action_If_Throw_Flag_Not_Set()
        {
            var context = new XrmFakedContext();
            var template = "Hello ${{Value(\\\"subject\\\")}}";

            var email = new Entity
            {
                Id = Guid.NewGuid(),
                LogicalName = "email",
                Attributes =
                {
                    { "subject", "Demo" }
                }
            };

            context.Initialize(email);

            var inputParameters = new ParameterCollection
            {
                { "jsonInput", "{" +
                "\"triggerUpdate\": true," +
                $"\"template\": \"{template}\"," +
                $"\"target\": {{\"Id\": \"{email.Id}\", \"LogicalName\": \"{email.LogicalName}\"}}" +
                "}" }
            };

            var pluginContext = context.GetDefaultPluginContext();
            pluginContext.InputParameters = inputParameters;
            pluginContext.OutputParameters = new ParameterCollection();

            Assert.That(() => context.ExecutePluginWith<XTLProcessor>(pluginContext), Throws.Nothing);
        }

        [Test]
        public void It_Should_Not_Break_HTML_Encoded_Stuff()
        {
            var context = new XrmFakedContext();
            var dynamicsFromText = "<b>From:</b> Microsoft Power Platform &lt;powerplat-noreply@microsoft.com&gt;; <br><b>";

            var email = new Entity
            {
                Id = Guid.NewGuid(),
                LogicalName = "email",
                Attributes =
                {
                    { "subject", "Demo" },
                    { "description", dynamicsFromText }
                }
            };

            var pluginContext = context.GetDefaultPluginContext();
            pluginContext.InputParameters = new ParameterCollection
            {
                { "Target", email }
            };

            var config = @"{ ""targetField"": ""description"",  ""templateField"": ""description"" }";
            context.ExecutePluginWithConfigurations<XTLProcessor>(pluginContext, config, string.Empty);

            Assert.That(email.GetAttributeValue<string>("description"), Is.EqualTo(dynamicsFromText));
        }
    }
}
