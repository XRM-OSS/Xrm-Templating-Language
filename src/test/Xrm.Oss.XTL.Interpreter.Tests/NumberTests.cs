using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FakeXrmEasy;
using NUnit.Framework;

namespace Xrm.Oss.XTL.Interpreter.Tests
{
    [TestFixture]
    public class NumberTests
    {
        [Test]
        public void It_Should_Parse_Int()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "Static( 1 )";
            var result = new XTLInterpreter(formula, null, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("1"));
        }

        [Test]
        public void It_Should_Parse_Decimal()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "Static( 1.234m )";
            var result = new XTLInterpreter(formula, null, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo(1.234m.ToString(CultureInfo.InvariantCulture)));
        }

        [Test]
        public void It_Should_Parse_Double()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "Static( 1.2345d )";
            var result = new XTLInterpreter(formula, null, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo(1.2345d.ToString(CultureInfo.InvariantCulture)));
        }

        [Test]
        public void It_Should_Throw_On_Int_With_Fractional_Part()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "Static( 1.0 )";
            var interpreter = new XTLInterpreter(formula, null, null, service, tracing);

            Assert.That(() => interpreter.Produce(), Throws.Exception);
        }
    }
}
