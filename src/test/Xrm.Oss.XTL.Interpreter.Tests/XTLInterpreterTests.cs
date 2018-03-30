using System;
using System.Reflection;
using System.Text;
using FakeItEasy;
using FakeXrmEasy;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using NUnit.Framework;
using Xrm.Oss.XTL.Interpreter;
using Xrm.Oss.XTL.Templating;

namespace Xrm.Oss.RecursiveDescentParser.Tests
{
    [TestFixture]
    public class XTLInterpreterTests
    {
        [Test]
        public void It_Should_Return_First_Level_Value ()
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
                    { "subject", "TestSubject" }
                }  
            };

            var formula = "Value (\"subject\")";
            var result = new XTLInterpreter(formula, email, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("TestSubject"));
        }

        [Test]
        public void It_Should_Ignore_Whitespace()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var email = new Entity
            {
                LogicalName = "account",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "name", "TestSubject" }
                }
            };

            var formula = "\nValue\n(\n\"name\"\n)\n";
            var result = new XTLInterpreter(formula, email, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("TestSubject"));
        }

        [Test]
        public void It_Should_Execute_Parameterless_Function()
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
                    { "subject", "TestSubject" }
                }
            };

            var formula = "RecordUrl ( PrimaryRecord ( ) )";
            var result = string.Empty;

            Assert.That(() => result = new XTLInterpreter(formula, email, new OrganizationConfig { OrganizationUrl = "https://crm/" }, service, tracing).Produce(), Throws.Nothing);

            Assert.That(result, Is.EqualTo($"<a href=\"https://crm/main.aspx?etn={email.LogicalName}&id={email.Id}&newWindow=true&pagetype=entityrecord\">https://crm/main.aspx?etn={email.LogicalName}&id={email.Id}&newWindow=true&pagetype=entityrecord</a>"));
        }
        
        [Test]
        public void It_Should_Retrieve_Related_Entity_Values()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var contact = new Entity
            {
                LogicalName = "contact",
                Id = Guid.NewGuid(),
                Attributes =
                {
                    { "firstname", "Frodo" }
                }
            };

            var emailWithSubject = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "subject", "TestSubject" },
                    { "regardingobjectid", contact.ToEntityReference() }
                }
            };

            context.Initialize(new Entity[] { contact, emailWithSubject });

            var formula = "Value(\"regardingobjectid.firstname\")";

            var result1 = new XTLInterpreter(formula, emailWithSubject, null, service, tracing).Produce();

            Assert.That(result1, Is.EqualTo("Frodo"));
        }

        [Test]
        public void It_Should_Use_Additional_References_In_Fetch()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var contact = new Entity
            {
                LogicalName = "contact",
                Id = Guid.NewGuid(),
                Attributes =
                {
                    { "firstname", "Frodo" }
                }
            };

            var task = new Entity
            {
                LogicalName = "task",
                Id = Guid.NewGuid(),
                Attributes =
                {
                    { "subject", "Task 1" },
                    { "description", "Description 1" },
                    { "regardingobjectid", contact.ToEntityReference() }
                }
            };

            var emailWithSubject = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "subject", "TestSubject" },
                    { "regardingobjectid", contact.ToEntityReference() }
                }
            };

            context.Initialize(new Entity[] { contact, task, emailWithSubject });

            var formula = "Value(\"subject\", First(Fetch(\"<fetch no-lock='true'><entity name='task'><attribute name='description' /><attribute name='subject' /><filter><condition attribute='regardingobjectid' operator='eq' value='{1}' /></filter></entity></fetch>\", Value(\"regardingobjectid\"))))";

            var result1 = new XTLInterpreter(formula, emailWithSubject, null, service, tracing).Produce();

            Assert.That(result1, Is.EqualTo("Task 1"));
        }

        [Test]
        public void It_Should_Not_Fail_If_Value_Target_Is_Null()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var contact = new Entity
            {
                LogicalName = "contact",
                Id = Guid.NewGuid(),
                Attributes =
                {
                    { "firstname", "Frodo" }
                }
            };
            
            var emailWithSubject = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "subject", "TestSubject" },
                    { "regardingobjectid", contact.ToEntityReference() }
                }
            };

            context.Initialize(new Entity[] { contact, emailWithSubject });

            var formula = "Value(\"subject\", First(Fetch(\"<fetch no-lock='true'><entity name='task'><attribute name='description' /><attribute name='subject' /><filter><condition attribute='regardingobjectid' operator='eq' value='{1}' /></filter></entity></fetch>\", Value(\"regardingobjectid\"))))";

            var result1 = new XTLInterpreter(formula, emailWithSubject, null, service, tracing).Produce();

            Assert.That(result1, Is.EqualTo(string.Empty));
        }

        [Test]
        public void It_Should_Only_Execute_Relevant_SubTree()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var contact = new Entity
            {
                LogicalName = "contact",
                Id = Guid.NewGuid(),
                Attributes =
                {
                    { "firstname", "Frodo" }
                }
            };

            var emailWithSubject = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "subject", "TestSubject" },
                    { "directioncode", true },
                    { "regardingobjectid", contact.ToEntityReference() }
                }
            };

            context.Initialize(new Entity[] { contact, emailWithSubject });

            var formula = "If ( IsEqual ( Value(\"directioncode\"), true ), Value(\"regardingobjectid.firstname\"), Value(\"regardingobjectid.lastname\") )";
            var result1 = new XTLInterpreter(formula, emailWithSubject, null, service, tracing).Produce();

            Assert.That(result1, Is.EqualTo("Frodo"));
            A.CallTo(() => service.Retrieve(A<string>._, A<Guid>._, A<ColumnSet>._)).MustHaveHappened(Repeated.Exactly.Once);
        }
    }
}
