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
    public class PropertyStringifierTests
    {
        [Test]
        public void It_Should_Return_Null_If_Attribute_Not_Found()
        {
            var email = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "oss_OptionSet", null }
                }
            };

            var text = PropertyStringifier.Stringify(email, "oss_OptionSet");
            Assert.That(text, Is.Null);
        }

        [Test]
        public void It_Should_Stringify_OptionSet_Using_Value_Without_Formatted_Value()
        {
            var email = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "oss_OptionSet", new OptionSetValue(1) }
                }
            };

            var text = PropertyStringifier.Stringify(email, "oss_OptionSet");
            Assert.That(text, Is.EqualTo("1"));
        }

        [Test]
        public void It_Should_Stringify_OptionSet_Using_Formatted_Value_If_Available()
        {
            var email = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "oss_OptionSet", new OptionSetValue(1) }
                }
            };

            email.FormattedValues.Add("oss_OptionSet", "Value1");

            var text = PropertyStringifier.Stringify(email, "oss_OptionSet");
            Assert.That(text, Is.EqualTo("Value1"));
        }

        [Test]
        public void It_Should_Stringify_EntityReference_Using_Id_Without_Formatted_Value()
        {
            var reference = new EntityReference("account", Guid.NewGuid());

            var email = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "oss_Ref", reference }
                }
            };

            var text = PropertyStringifier.Stringify(email, "oss_Ref");
            Assert.That(text, Is.EqualTo(reference.Id.ToString()));
        }

        [Test]
        public void It_Should_Stringify_EntityReference_Using_Formatted_Value_If_Available()
        {
            var reference = new EntityReference("account", Guid.NewGuid());

            var email = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "oss_Ref", reference }
                }
            };

            email.FormattedValues.Add("oss_Ref", "EntityReference Name");

            var text = PropertyStringifier.Stringify(email, "oss_Ref");
            Assert.That(text, Is.EqualTo("EntityReference Name"));
        }

        [Test]
        public void It_Should_Stringify_Money_Using_Value_Without_Formatted_Value()
        {
            var email = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "oss_Money", new Money(1000m) }
                }
            };

            var text = PropertyStringifier.Stringify(email, "oss_Money");
            Assert.That(text, Is.EqualTo("1000"));
        }

        [Test]
        public void It_Should_Stringify_Money_Using_Formatted_Value_If_Available()
        {
            var email = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "oss_Money", new Money(1000m) }
                }
            };

            email.FormattedValues.Add("oss_Money", "1000.00€");

            var text = PropertyStringifier.Stringify(email, "oss_Money");
            Assert.That(text, Is.EqualTo("1000.00€"));
        }
    }
}
