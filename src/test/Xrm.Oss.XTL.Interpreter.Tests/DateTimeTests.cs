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
    public class DateTimeTests
    {
        [Test]
        public void It_Should_Insert_DateTime_Now()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "DateTimeNow()";
            Assert.That(() => DateTime.Parse(new XTLInterpreter(formula, null, null, service, tracing).Produce()), Is.EqualTo(DateTime.Now).Within(TimeSpan.FromSeconds(5)));
        }

        [Test]
        public void It_Should_Insert_DateTime_Utc_Now()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "DateTimeUtcNow()";
            Assert.That(() => DateTime.Parse(new XTLInterpreter(formula, null, null, service, tracing).Produce()), Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(5)));
        }

        [Test]
        public void It_Should_Stringify_DateTime()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "DateToString(DateTimeUtcNow(), \"yyyyMMdd\")";
            Assert.That(() => new XTLInterpreter(formula, null, null, service, tracing).Produce(), Is.EqualTo(DateTime.UtcNow.ToString("yyyyMMdd")));
        }
    }
}
