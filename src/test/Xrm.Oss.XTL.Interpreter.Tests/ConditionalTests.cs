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
    public class ConditionalTests
    {
        [Test]
        public void It_Should_Apply_If_Condition()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var emailWithSubject = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "subject", "TestSubject" }
                }
            };

            var emailWithoutSubject = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                }
            };

            var formula = "If( IsNull ( Value(\"subject\") ), \"Fallback\", Value(\"subject\") )";

            var result1 = new XTLInterpreter(formula, emailWithSubject, null, service, tracing).Produce();
            var result2 = new XTLInterpreter(formula, emailWithoutSubject, null, service, tracing).Produce();

            Assert.That(result1, Is.EqualTo("TestSubject"));
            Assert.That(result2, Is.EqualTo("Fallback"));
        }

        [Test]
        public void It_Should_Apply_Not_Operator()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var emailWithSubject = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "subject", "TestSubject" }
                }
            };

            var emailWithoutSubject = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                }
            };

            var formula = "If( Not ( IsNull ( Value(\"subject\") ) ), Value(\"subject\"), \"Fallback\" )";

            var result1 = new XTLInterpreter(formula, emailWithSubject, null, service, tracing).Produce();
            var result2 = new XTLInterpreter(formula, emailWithoutSubject, null, service, tracing).Produce();

            Assert.That(result1, Is.EqualTo("TestSubject"));
            Assert.That(result2, Is.EqualTo("Fallback"));
        }

        [Test]
        public void It_Should_Evaluate_Or_Correctly()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var emailWithSubject = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "subject", "TestSubject" }
                }
            };

            var emailWithoutSubject = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                }
            };

            var emailWithBoth = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "subject", "TestSubject" },
                    { "description", "description" }
                }
            };

            var formula = "If( Or ( IsNull ( Value(\"subject\") ), IsNull( Value (\"description\") ) ), \"Something was null\", \"Nothing null\" )";

            var result1 = new XTLInterpreter(formula, emailWithSubject, null, service, tracing).Produce();
            var result2 = new XTLInterpreter(formula, emailWithoutSubject, null, service, tracing).Produce();
            var result3 = new XTLInterpreter(formula, emailWithBoth, null, service, tracing).Produce();

            Assert.That(result1, Is.EqualTo("Something was null"));
            Assert.That(result2, Is.EqualTo("Something was null"));
            Assert.That(result3, Is.EqualTo("Nothing null"));
        }

        [Test]
        public void It_Should_Evaluate_And_Correctly()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var emailWithSubject = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "subject", "TestSubject" }
                }
            };

            var emailWithoutSubject = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                }
            };

            var emailWithBoth = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "subject", "TestSubject" },
                    { "description", "description" }
                }
            };

            var formula = "If( And ( IsNull ( Value(\"subject\") ), IsNull( Value (\"description\") ) ), \"Both null\", \"Not both null\" )";

            var result1 = new XTLInterpreter(formula, emailWithSubject, null, service, tracing).Produce();
            var result2 = new XTLInterpreter(formula, emailWithoutSubject, null, service, tracing).Produce();
            var result3 = new XTLInterpreter(formula, emailWithBoth, null, service, tracing).Produce();

            Assert.That(result1, Is.EqualTo("Not both null"));
            Assert.That(result2, Is.EqualTo("Both null"));
            Assert.That(result3, Is.EqualTo("Not both null"));
        }
    }
}
