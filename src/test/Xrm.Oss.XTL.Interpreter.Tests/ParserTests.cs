using System;
using System.Text;
using Microsoft.Xrm.Sdk;
using NUnit.Framework;
using Xrm.Oss.XTL.Interpreter;

namespace Xrm.Oss.RecursiveDescentParser.Tests
{
    [TestFixture]
    public class ParserTests
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
            var result = new XTLInterpreter(formula, email, null).Produce();

            Assert.That(result, Is.EqualTo("TestSubject"));
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

            var result1 = new XTLInterpreter(formula, emailWithSubject, null).Produce();
            var result2 = new XTLInterpreter(formula, emailWithoutSubject, null).Produce();

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

            var result1 = new XTLInterpreter(formula, emailWithSubject, null).Produce();
            var result2 = new XTLInterpreter(formula, emailWithoutSubject, null).Produce();

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

            var result1 = new XTLInterpreter(formula, emailWithSubject, null).Produce();
            var result2 = new XTLInterpreter(formula, emailWithoutSubject, null).Produce();
            var result3 = new XTLInterpreter(formula, emailWithBoth, null).Produce();

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

            var result1 = new XTLInterpreter(formula, emailWithSubject, null).Produce();
            var result2 = new XTLInterpreter(formula, emailWithoutSubject, null).Produce();
            var result3 = new XTLInterpreter(formula, emailWithBoth, null).Produce();

            Assert.That(result1, Is.EqualTo("Not both null"));
            Assert.That(result2, Is.EqualTo("Both null"));
            Assert.That(result3, Is.EqualTo("Not both null"));
        }
    }
}
