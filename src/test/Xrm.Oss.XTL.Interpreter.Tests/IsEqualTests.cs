using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FakeXrmEasy;
using Microsoft.Xrm.Sdk;
using NUnit.Framework;

namespace Xrm.Oss.XTL.Interpreter.Tests
{
    [TestFixture]
    public class IsEqualTests
    {
        [Test]
        public void It_Should_Recognize_Matching_Integers()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var email = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "int", 1 }
                }
            };

            var formula = "If ( IsEqual ( Value ( \"int\" ), 1 ), \"true\", \"false\" )";
            var result = new XTLInterpreter(formula, email, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("true"));
        }

        [Test]
        public void It_Should_Recognize_Not_Matching_Integers()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var email = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "int", 2 }
                }
            };

            var formula = "If ( IsEqual ( Value ( \"int\" ), 1 ), \"true\", \"false\" )";
            var result = new XTLInterpreter(formula, email, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("false"));
        }

        [Test]
        public void It_Should_Throw_On_Invalid_Int_Comparison()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var email = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "int", 2 }
                }
            };

            var formula = "If ( IsEqual ( Value ( \"int\" ), \"1\" ), \"true\", \"false\" )";
            Assert.That(() => new XTLInterpreter(formula, email, null, service, tracing).Produce(), Throws.TypeOf<InvalidPluginExecutionException>());
        }

        [Test]
        public void It_Should_Recognize_Matching_OptionSet_Values()
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
                    { "gendercode", new OptionSetValue(1) }
                }
            };

            var formula = "If ( IsEqual ( Value ( \"gendercode\" ), 1 ), \"true\", \"false\" )";
            var result = new XTLInterpreter(formula, contact, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("true"));
        }

        [Test]
        public void It_Should_Recognize_Not_Matching_OptionSet_Values()
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
                    { "gendercode", new OptionSetValue(1) }
                }
            };

            var formula = "If ( IsEqual ( Value ( \"gendercode\" ), 2 ), \"true\", \"false\" )";
            var result = new XTLInterpreter(formula, contact, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("false"));
        }

        [Test]
        public void It_Should_Throw_On_Invalid_OptionSet_Comparison()
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
                    { "gendercode", new OptionSetValue(1) }
                }
            };

            var formula = "If ( IsEqual ( Value ( \"gendercode\" ), \"1\" ), \"true\", \"false\" )";
            Assert.That(() => new XTLInterpreter(formula, contact, null, service, tracing).Produce(), Throws.TypeOf<InvalidPluginExecutionException>());
        }

        [Test]
        public void It_Should_Recognize_Matching_Bool_Values()
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
                    { "donotsendbulkemails", true }
                }
            };

            var formula = "If ( IsEqual ( Value ( \"donotsendbulkemails\" ), true ), \"true\", \"false\" )";
            var result = new XTLInterpreter(formula, contact, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("true"));
        }

        [Test]
        public void It_Should_Recognize_Not_Matching_Bool_Values()
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
                    { "donotsendbulkemails", true }
                }
            };

            var formula = "If ( IsEqual ( Value ( \"donotsendbulkemails\" ), false ), \"true\", \"false\" )";
            var result = new XTLInterpreter(formula, contact, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("false"));
        }

        [Test]
        public void It_Should_Throw_On_Not_Matching_Bool_Comparison()
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
                    { "donotsendbulkemails", true }
                }
            };

            var formula = "If ( IsEqual ( Value ( \"donotsendbulkemails\" ), \"true\" ), \"true\", \"false\" )";
            Assert.That(() => new XTLInterpreter(formula, contact, null, service, tracing).Produce(), Throws.TypeOf<InvalidPluginExecutionException>());
        }
    }
}
