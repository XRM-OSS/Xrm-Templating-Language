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
    public class FormattingTests
    {
        [Test]
        public void It_Should_Throw_If_Not_Enough_Params()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "Format ( )";
            Assert.That(() => new XTLInterpreter(formula, null, null, service, tracing).Produce(), Throws.TypeOf<InvalidPluginExecutionException>());
        }

        [Test]
        public void It_Should_Format_Int()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "Format ( 1,  { format: \"{0:00000}\" } )";
            var result = new XTLInterpreter(formula, null, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("00001"));
        }

        [Test]
        public void It_Should_Format_Double()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "Format ( 123456789.2d,  { format: \"{0:0,0.0}\" } )";
            var result = new XTLInterpreter(formula, null, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("123,456,789.2"));
        }

        [Test]
        public void It_Should_Format_Decimal()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "Format ( 123456789.2m,  { format: \"{0:0,0.0}\" } )";
            var result = new XTLInterpreter(formula, null, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("123,456,789.2"));
        }

        [Test]
        public void It_Should_Format_Money()
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
                    { "revenue", new Money(123456789.2m) }
                }
            };

            var formula = "Format ( Value(\"revenue\"),  { format: \"{0:0,0.0}\" } )";
            var result = new XTLInterpreter(formula, contact, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("123,456,789.2"));
        }

        [Test]
        public void It_Should_Format_DateTime()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "Format(DateTimeUtcNow(), { format: \"{0:yyyyMMdd}\" })";
            Assert.That(() => new XTLInterpreter(formula, null, null, service, tracing).Produce(), Is.EqualTo(DateTime.UtcNow.ToString("yyyyMMdd")));
        }
    }
}
