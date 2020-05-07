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
    public class SubstringTests
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

            var formula = "Substring ( Value ( \"firstname\" ) )";
            Assert.That(() => new XTLInterpreter(formula, contact, null, service, tracing).Produce(), Throws.TypeOf<InvalidPluginExecutionException>());
        }

        [Test]
        public void It_Should_Throw_If_Not_Enough_Params_IndexOf()
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

            var formula = "IndexOf ( Value ( \"firstname\" ) )";
            Assert.That(() => new XTLInterpreter(formula, contact, null, service, tracing).Produce(), Throws.TypeOf<InvalidPluginExecutionException>());
        }

        [Test]
        public void It_Should_Throw_If_Index_Is_Not_An_Int()
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

            var formula = "Substring ( Value ( \"firstname\" ), \"1\" )";
            Assert.That(() => new XTLInterpreter(formula, contact, null, service, tracing).Produce(), Throws.TypeOf<InvalidPluginExecutionException>());
        }

        [Test]
        public void It_Should_Take_Everything_From_Start_Without_Length()
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

            var formula = "Substring ( Value ( \"firstname\" ), 1 )";
            var result = new XTLInterpreter(formula, contact, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("rodo"));
        }

        [Test]
        public void It_Should_Take_Given_Length_Only_If_Length_Is_Passed()
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

            var formula = "Substring ( Value ( \"firstname\" ), 1, 2 )";
            var result = new XTLInterpreter(formula, contact, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("ro"));
        }

        [Test]
        public void It_Should_Return_Correct_Index()
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
                    { "firstname", "Frodo Beutlin" }
                }
            };

            var formula = "IndexOf ( Value ( \"firstname\" ), \"Beutlin\")";
            var result = new XTLInterpreter(formula, contact, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("6"));
        }
    }
}
