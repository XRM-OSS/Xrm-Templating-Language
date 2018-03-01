using System;
using System.Text;
using FakeItEasy;
using FakeXrmEasy;
using Microsoft.Xrm.Sdk;
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
        public void It_Should_Return_First_Level_Text ()
        {
            var email = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection 
                {
                    { "subject", "TestSubject" }
                }  
            };

            var formula = "Text (\"subject\")";
            var result = new XTLInterpreter(formula, email, null, null).Produce();

            Assert.That(result, Is.EqualTo("TestSubject"));
        }

        [Test]
        public void It_Should_Execute_Parameterless_Function()
        {
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
            Assert.That(() => new XTLInterpreter(formula, email, new OrganizationConfig { OrganizationUrl = "https://crm/" }, null).Produce(), Throws.Nothing);
        }

        [Test]
        public void It_Should_Apply_If_Condition()
        {
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

            var formula = "If( IsNull ( Text(\"subject\") ), \"Fallback\", Text(\"subject\") )";

            var result1 = new XTLInterpreter(formula, emailWithSubject, null, null).Produce();
            var result2 = new XTLInterpreter(formula, emailWithoutSubject, null, null).Produce();

            Assert.That(result1, Is.EqualTo("TestSubject"));
            Assert.That(result2, Is.EqualTo("Fallback"));
        }

        [Test]
        public void It_Should_Apply_Not_Operator()
        {
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

            var formula = "If( Not ( IsNull ( Text(\"subject\") ) ), Text(\"subject\"), \"Fallback\" )";

            var result1 = new XTLInterpreter(formula, emailWithSubject, null, null).Produce();
            var result2 = new XTLInterpreter(formula, emailWithoutSubject, null, null).Produce();

            Assert.That(result1, Is.EqualTo("TestSubject"));
            Assert.That(result2, Is.EqualTo("Fallback"));
        }

        [Test]
        public void It_Should_Evaluate_Or_Correctly()
        {
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

            var formula = "If( Or ( IsNull ( Text(\"subject\") ), IsNull( Text (\"description\") ) ), \"Something was null\", \"Nothing null\" )";

            var result1 = new XTLInterpreter(formula, emailWithSubject, null, null).Produce();
            var result2 = new XTLInterpreter(formula, emailWithoutSubject, null, null).Produce();
            var result3 = new XTLInterpreter(formula, emailWithBoth, null, null).Produce();

            Assert.That(result1, Is.EqualTo("Something was null"));
            Assert.That(result2, Is.EqualTo("Something was null"));
            Assert.That(result3, Is.EqualTo("Nothing null"));
        }

        [Test]
        public void It_Should_Evaluate_And_Correctly()
        {
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

            var formula = "If( And ( IsNull ( Text(\"subject\") ), IsNull( Text (\"description\") ) ), \"Both null\", \"Not both null\" )";

            var result1 = new XTLInterpreter(formula, emailWithSubject, null, null).Produce();
            var result2 = new XTLInterpreter(formula, emailWithoutSubject, null, null).Produce();
            var result3 = new XTLInterpreter(formula, emailWithBoth, null, null).Produce();

            Assert.That(result1, Is.EqualTo("Not both null"));
            Assert.That(result2, Is.EqualTo("Both null"));
            Assert.That(result3, Is.EqualTo("Not both null"));
        }

        [Test]
        public void It_Should_Retrieve_Related_Entity_Values()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();

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

            var formula = "Text(\"regardingobjectid.firstname\")";

            var result1 = new XTLInterpreter(formula, emailWithSubject, null, service).Produce();

            Assert.That(result1, Is.EqualTo("Frodo"));
        }

        [Test, Ignore("Will be fixed later on")]
        public void It_Should_Only_Execute_Relevant_SubTree()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();

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

            var formula = "If ( Not ( IsNull ( Value(\"regardingobjectid\") ) ), Text(\"regardingobjectid.firstname\"), Text(\"regardingobjectid.lastname\") )";
            var result1 = new XTLInterpreter(formula, emailWithSubject, null, service).Produce();

            Assert.That(result1, Is.EqualTo("Frodo"));
            A.CallTo(() => service.RetrieveMultiple(A<QueryBase>._)).MustHaveHappened(Repeated.Exactly.Once);
        }
    }
}
