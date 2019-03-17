using System;
using System.Collections.Generic;
using System.Globalization;
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
            // DateTime.Parse parses to local time...
            Assert.That(() => DateTime.Parse(new XTLInterpreter(formula, null, null, service, tracing).Produce()).ToUniversalTime(), Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(5)));
        }

        [Test]
        public void It_Should_Stringify_DateTime()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "DateToString(DateTimeUtcNow(), { format: \"yyyyMMdd\" })";
            Assert.That(() => new XTLInterpreter(formula, null, null, service, tracing).Produce(), Is.EqualTo(DateTime.UtcNow.ToString("yyyyMMdd")));
        }

        [Test]
        public void It_Should_Throw_If_Convert_Is_Missing_Arguments()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "Convert(DateTimeUtcNow())";
            Assert.That(() => new XTLInterpreter(formula, null, null, service, tracing).Produce(), Throws.TypeOf<InvalidPluginExecutionException>());

            var formulaMissingConfigValues = "Convert(DateTimeUtcNow(), {})";
            Assert.That(() => new XTLInterpreter(formulaMissingConfigValues, null, null, service, tracing).Produce(), Throws.TypeOf<InvalidPluginExecutionException>());
        }

        [Test]
        public void It_Should_Convert_By_Time_Zone_Id()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var createdOn = new DateTime(2019, 3, 17, 12, 0, 0, DateTimeKind.Utc);

            var target = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes =
                {
                    { "createdon", createdOn }
                }
            };

            var expected = TimeZoneInfo.ConvertTimeFromUtc(createdOn, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")).ToString("g", CultureInfo.InvariantCulture);

            var formula = "ConvertDateTime(Value(\"createdon\"), { timeZoneId: \"Eastern Standard Time\" })";
            Assert.That(() => new XTLInterpreter(formula, target, null, service, tracing).Produce(), Is.EqualTo(expected));
        }

        [Test]
        public void It_Should_Convert_By_User_TimeZone()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var createdOn = new DateTime(2019, 3, 17, 12, 0, 0, DateTimeKind.Utc);

            var userId = Guid.NewGuid();

            var target = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes =
                {
                    { "createdon", createdOn },
                    { "ownerid", new EntityReference("systemuser", userId) }
                }
            };

            var userSettings = new Entity
            {
                LogicalName = "usersettings",
                Id = userId,
                Attributes =
                {
                    { "timezonecode", 110 }
                }
            };

            var timeZoneDefinition = new Entity
            {
                LogicalName = "timezonedefinition",
                Id = userId,
                Attributes =
                {
                    { "timezonecode", 110 },
                    { "standardname", "W. Europe Standard Time" }
                }
            };

            context.Initialize(new Entity[] { userSettings, timeZoneDefinition });

            var expected = TimeZoneInfo.ConvertTimeFromUtc(createdOn, TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time")).ToString("g", CultureInfo.InvariantCulture);

            var formula = "ConvertDateTime(Value(\"createdon\"), { userId: Value(\"ownerid\") })";
            Assert.That(() => new XTLInterpreter(formula, target, null, service, tracing).Produce(), Is.EqualTo(expected));
        }
    }
}
