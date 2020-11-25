using Microsoft.Xrm.Sdk;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FakeXrmEasy;
using Microsoft.Xrm.Sdk.Metadata;
using System.Reflection;

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

            var text = PropertyStringifier.Stringify("oss_OptionSet", email, null);
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

            var text = PropertyStringifier.Stringify("oss_OptionSet", email, null);
            Assert.That(text, Is.EqualTo("1"));
        }

        [Test]
        public void It_Should_Stringify_OptionSet_Using_Formatted_Value_If_Available()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();

            var email = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "oss_OptionSet", new OptionSetValue(1) }
                }
            };

            var config = new ConfigHandler(new Dictionary<string, object>
            {
                { "optionSetLcid", 1031 }
            });

            var metadata = new EntityMetadata { LogicalName = "email" };
            var field = typeof(EntityMetadata).GetField("_attributes", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(metadata, new AttributeMetadata[] { new PicklistAttributeMetadata { LogicalName = "oss_OptionSet", OptionSet = new OptionSetMetadata { Options = { new OptionMetadata { Value = 1, Label = new Label("Value1", 1031) } } } } });

            context.InitializeMetadata(metadata);

            var text = PropertyStringifier.Stringify("oss_OptionSet", email, service, config);
            Assert.That(text, Is.EqualTo("Value1"));
        }

        [Test]
        public void It_Should_Stringify_Status_OptionSet_Using_Formatted_Value_If_Available()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();

            var email = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "oss_OptionSet", new OptionSetValue(1) }
                }
            };

            var config = new ConfigHandler(new Dictionary<string, object>
            {
                { "optionSetLcid", 1031 }
            });

            var metadata = new EntityMetadata { LogicalName = "email" };
            var field = typeof(EntityMetadata).GetField("_attributes", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(metadata, new AttributeMetadata[] { new StatusAttributeMetadata { LogicalName = "oss_OptionSet", OptionSet = new OptionSetMetadata { Options = { new OptionMetadata { Value = 1, Label = new Label("Value1", 1031) } } } } });

            context.InitializeMetadata(metadata);

            var text = PropertyStringifier.Stringify("oss_OptionSet", email, service, config);
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

            var text = PropertyStringifier.Stringify("oss_Ref", email, null);
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

            var text = PropertyStringifier.Stringify("oss_Ref", email, null);
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

            var text = PropertyStringifier.Stringify("oss_Money", email, null);
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

            var text = PropertyStringifier.Stringify("oss_Money", email, null);
            Assert.That(text, Is.EqualTo("1000.00€"));
        }

        [Test]
        public void It_Should_Stringify_Aliased_Value()
        {
            var email = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "someGroup.name", new AliasedValue("account", "name", "Baggins Inc." ) }
                }
            };

            var text = PropertyStringifier.Stringify("someGroup.name", email, null);
            Assert.That(text, Is.EqualTo("Baggins Inc."));
        }
    }
}
