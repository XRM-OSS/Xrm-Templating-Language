using System;
using System.Text;
using Microsoft.Xrm.Sdk;
using NUnit.Framework;

namespace Xrm.Oss.RecursiveDescentParser.Tests
{
    [TestFixture]
    public class ParserTests
    {
        [Test]
        public void It_Should_Pass_Valid_Expression()
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

            var formula = "GetValue (\"subject\")";
            var result = new XTLInterpreter(formula, email, null).Produce();

            Assert.That(result, Is.EqualTo("TestSubject"));
        }
    }
}
