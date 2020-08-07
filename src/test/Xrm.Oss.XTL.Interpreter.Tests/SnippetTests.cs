using FakeXrmEasy;
using Microsoft.Xrm.Sdk;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xrm.Oss.XTL.Interpreter.Tests
{
    [TestFixture]
    public class SnippetTests
    {
        [Test]
        public void It_Should_Throw_If_Not_Enough_Params()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var contact = new Entity
            {
                LogicalName = "contact",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "firstname", "Frodo" }
                }
            };

            var formula = "Snippet ( )";
            Assert.That(() => new XTLInterpreter(formula, contact, null, service, tracing).Produce(), Throws.TypeOf<InvalidPluginExecutionException>());
        }

        [Test]
        public void It_Should_Fetch_Simple_Snippet_By_Name()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var contact = new Entity
            {
                LogicalName = "contact",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "firstname", "Frodo" }
                }
            };

            var snippetEs = new Entity
            {
                LogicalName = "oss_xtlsnippet",
                Id = Guid.NewGuid(),
                ["oss_name"] = "salutation es"
            };

            var snippetDe = new Entity
            {
                LogicalName = "oss_xtlsnippet",
                Id = Guid.NewGuid(),
                ["oss_name"] = "salutation de",
                ["oss_xtlexpression"] = "Value('firstname')"
            };

            context.Initialize(new Entity[] { snippetEs, snippetDe });

            var formula = "Snippet ('salutation de')";
            var result = new XTLInterpreter(formula, contact, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("Frodo"));
        }

        [Test]
        public void It_Should_Fetch_Simple_Snippet_By_Unique_Name()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var contact = new Entity
            {
                LogicalName = "contact",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "firstname", "Frodo" }
                }
            };

            var snippetEs = new Entity
            {
                LogicalName = "oss_xtlsnippet",
                Id = Guid.NewGuid(),
                ["oss_name"] = "salutation es"
            };

            var snippetDe = new Entity
            {
                LogicalName = "oss_xtlsnippet",
                Id = Guid.NewGuid(),
                ["oss_uniquename"] = "salutation de",
                ["oss_xtlexpression"] = "Value('firstname')"
            };

            context.Initialize(new Entity[] { snippetEs, snippetDe });

            var formula = "Snippet ('salutation de')";
            var result = new XTLInterpreter(formula, contact, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("Frodo"));
        }

        [Test]
        public void It_Should_Not_Wrap_Automatically_If_Contains_Plain_Text()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var contact = new Entity
            {
                LogicalName = "contact",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "firstname", "Frodo" }
                }
            };

            var snippetEs = new Entity
            {
                LogicalName = "oss_xtlsnippet",
                Id = Guid.NewGuid(),
                ["oss_name"] = "salutation es"
            };

            var snippetDe = new Entity
            {
                LogicalName = "oss_xtlsnippet",
                Id = Guid.NewGuid(),
                ["oss_name"] = "salutation de",
                ["oss_xtlexpression"] = "This contains text: ${{Value('firstname')}}",
                ["oss_containsplaintext"] = true
            };

            context.Initialize(new Entity[] { snippetEs, snippetDe });

            var formula = "Snippet ('salutation de')";
            var result = new XTLInterpreter(formula, contact, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("This contains text: Frodo"));
        }

        [Test]
        public void It_Should_Honour_Filter_Config()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var contact = new Entity
            {
                LogicalName = "contact",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "firstname", "Frodo" }
                }
            };

            var snippetDe = new Entity
            {
                LogicalName = "oss_xtlsnippet",
                Id = Guid.NewGuid(),
                ["oss_name"] = "salutation",
                ["new_customlanguage"] = "de",
                ["oss_xtlexpression"] = "Hier steht text: ${{Value('firstname')}}",
                ["oss_containsplaintext"] = true
            };

            var snippetEn = new Entity
            {
                LogicalName = "oss_xtlsnippet",
                Id = Guid.NewGuid(),
                ["oss_name"] = "salutation",
                ["new_customlanguage"] = "en",
                ["oss_xtlexpression"] = "This contains text: ${{Value('firstname')}}",
                ["oss_containsplaintext"] = true
            };

            context.Initialize(new Entity[] { snippetDe, snippetEn });

            var formulaEn = "Snippet ('salutation', { filter: '<filter><condition attribute=\"new_customlanguage\" operator=\"eq\" value=\"en\" /></filter>' })";
            var resultEn = new XTLInterpreter(formulaEn, contact, null, service, tracing).Produce();

            var formulaDe = "Snippet ('salutation', { filter: '<filter><condition attribute=\"new_customlanguage\" operator=\"eq\" value=\"de\" /></filter>' })";
            var resultDe = new XTLInterpreter(formulaDe, contact, null, service, tracing).Produce();

            Assert.That(resultEn, Is.EqualTo("This contains text: Frodo"));
            Assert.That(resultDe, Is.EqualTo("Hier steht text: Frodo"));
        }

        [Test]
        public void It_Should_Honour_Filter_With_Xtl_Expressions()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var contact = new Entity
            {
                LogicalName = "contact",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "firstname", "Frodo" },
                    { "new_contactlanguage", "en" }
                }
            };

            var snippetDe = new Entity
            {
                LogicalName = "oss_xtlsnippet",
                Id = Guid.NewGuid(),
                ["oss_name"] = "salutation",
                ["new_customlanguage"] = "de",
                ["oss_xtlexpression"] = "Hier steht text: ${{Value('firstname')}}",
                ["oss_containsplaintext"] = true
            };

            var snippetEn = new Entity
            {
                LogicalName = "oss_xtlsnippet",
                Id = Guid.NewGuid(),
                ["oss_name"] = "salutation",
                ["new_customlanguage"] = "en",
                ["oss_xtlexpression"] = "This contains text: ${{Value('firstname')}}",
                ["oss_containsplaintext"] = true
            };

            context.Initialize(new Entity[] { snippetDe, snippetEn });

            var formula = "Snippet ('salutation', { filter: '<filter><condition attribute=\"new_customlanguage\" operator=\"eq\" value=\"${{Value(\"new_contactlanguage\")}}\" /></filter>' })";
            var result = new XTLInterpreter(formula, contact, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("This contains text: Frodo"));
        }
    }
}
