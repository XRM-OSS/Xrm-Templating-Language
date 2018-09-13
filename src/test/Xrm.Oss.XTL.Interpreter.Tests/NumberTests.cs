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
        public void It_Should_Parse_Negative_Numbers()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "Static( -1 )";
            var result = new XTLInterpreter(formula, null, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo((-1).ToString(CultureInfo.InvariantCulture)));
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

        [Test]
        public void It_Should_Compare_Double_That_Is_Less()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "IsLess ( 1.0d, 1.1d )";
            var interpreter = new XTLInterpreter(formula, null, null, service, tracing);

            Assert.That(() => interpreter.Produce(), Is.EqualTo(bool.TrueString));
        }

        [Test]
        public void It_Should_Compare_Double_That_Is_Not_Less()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "IsLess ( 1.2d, 1.1d )";
            var interpreter = new XTLInterpreter(formula, null, null, service, tracing);

            Assert.That(() => interpreter.Produce(), Is.EqualTo(bool.FalseString));
        }

        [Test]
        public void It_Should_Compare_Double_That_Is_Less_Equal()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "IsLessEqual ( 1.0d, 1.0d )";
            var interpreter = new XTLInterpreter(formula, null, null, service, tracing);

            Assert.That(() => interpreter.Produce(), Is.EqualTo(bool.TrueString));
        }

        [Test]
        public void It_Should_Compare_Double_That_Is_Greater()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "IsGreater ( 1.2d, 1.1d )";
            var interpreter = new XTLInterpreter(formula, null, null, service, tracing);

            Assert.That(() => interpreter.Produce(), Is.EqualTo(bool.TrueString));
        }

        [Test]
        public void It_Should_Compare_Double_That_Is_Not_Greater()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "IsGreater ( 1.0d, 1.1d )";
            var interpreter = new XTLInterpreter(formula, null, null, service, tracing);

            Assert.That(() => interpreter.Produce(), Is.EqualTo(bool.FalseString));
        }

        [Test]
        public void It_Should_Compare_Double_That_Is_Greater_Equal()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "IsGreaterEqual ( 0.0d, 0.0d )";
            var interpreter = new XTLInterpreter(formula, null, null, service, tracing);

            Assert.That(() => interpreter.Produce(), Is.EqualTo(bool.TrueString));
        }

        [Test]
        public void It_Should_Compare_Decimal_That_Is_Less()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "IsLess ( 1.0m, 1.1m )";
            var interpreter = new XTLInterpreter(formula, null, null, service, tracing);

            Assert.That(() => interpreter.Produce(), Is.EqualTo(bool.TrueString));
        }

        [Test]
        public void It_Should_Compare_Decimal_That_Is_Not_Less()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "IsLess ( 1.2m, 1.1m )";
            var interpreter = new XTLInterpreter(formula, null, null, service, tracing);

            Assert.That(() => interpreter.Produce(), Is.EqualTo(bool.FalseString));
        }

        [Test]
        public void It_Should_Compare_Decimal_That_Is_Less_Equal()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "IsLessEqual ( 1.0m, 1.0m )";
            var interpreter = new XTLInterpreter(formula, null, null, service, tracing);

            Assert.That(() => interpreter.Produce(), Is.EqualTo(bool.TrueString));
        }

        [Test]
        public void It_Should_Compare_Decimal_That_Is_Greater()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "IsGreater ( 1.2m, 1.1m )";
            var interpreter = new XTLInterpreter(formula, null, null, service, tracing);

            Assert.That(() => interpreter.Produce(), Is.EqualTo(bool.TrueString));
        }

        [Test]
        public void It_Should_Compare_Decimal_That_Is_Not_Greater()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "IsGreater ( 1.0m, 1.1m )";
            var interpreter = new XTLInterpreter(formula, null, null, service, tracing);

            Assert.That(() => interpreter.Produce(), Is.EqualTo(bool.FalseString));
        }

        [Test]
        public void It_Should_Compare_Decimal_That_Is_Greater_Equal()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "IsGreaterEqual ( 0.0m, 0.0m )";
            var interpreter = new XTLInterpreter(formula, null, null, service, tracing);

            Assert.That(() => interpreter.Produce(), Is.EqualTo(bool.TrueString));
        }

        [Test]
        public void It_Should_Compare_Int_That_Is_Less()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "IsLess ( 0, 1 )";
            var interpreter = new XTLInterpreter(formula, null, null, service, tracing);

            Assert.That(() => interpreter.Produce(), Is.EqualTo(bool.TrueString));
        }

        [Test]
        public void It_Should_Compare_Int_That_Is_Not_Less()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "IsLess ( 2, 1 )";
            var interpreter = new XTLInterpreter(formula, null, null, service, tracing);

            Assert.That(() => interpreter.Produce(), Is.EqualTo(bool.FalseString));
        }

        [Test]
        public void It_Should_Compare_Int_That_Is_Less_Equal()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "IsLessEqual ( 1, 1 )";
            var interpreter = new XTLInterpreter(formula, null, null, service, tracing);

            Assert.That(() => interpreter.Produce(), Is.EqualTo(bool.TrueString));
        }

        [Test]
        public void It_Should_Compare_Int_That_Is_Greater()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "IsGreater ( 2, 1 )";
            var interpreter = new XTLInterpreter(formula, null, null, service, tracing);

            Assert.That(() => interpreter.Produce(), Is.EqualTo(bool.TrueString));
        }

        [Test]
        public void It_Should_Compare_Int_That_Is_Not_Greater()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "IsGreater ( 0, 1 )";
            var interpreter = new XTLInterpreter(formula, null, null, service, tracing);

            Assert.That(() => interpreter.Produce(), Is.EqualTo(bool.FalseString));
        }

        [Test]
        public void It_Should_Compare_Int_That_Is_Greater_Equal()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "IsGreaterEqual ( 0, 0 )";
            var interpreter = new XTLInterpreter(formula, null, null, service, tracing);

            Assert.That(() => interpreter.Produce(), Is.EqualTo(bool.TrueString));
        }
    }
}
