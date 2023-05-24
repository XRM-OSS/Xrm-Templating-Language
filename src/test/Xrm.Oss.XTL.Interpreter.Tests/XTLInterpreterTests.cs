using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using FakeItEasy;
using FakeXrmEasy;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using NUnit.Framework;
using Xrm.Oss.XTL.Interpreter;
using Xrm.Oss.XTL.Templating;

namespace Xrm.Oss.XTL.Interpreter.Tests
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
        public void It_Should_Be_True_When_All_Conditions_In_And_Are_True()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();
            
            var formula = "And(true, true, true, true)";
            var result = new XTLInterpreter(formula, new Entity(), null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo(bool.TrueString));
        }

        [Test]
        public void It_Should_Be_False_When_One_Condition_In_And_Is_False()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "And(true, true, true, false)";
            var result = new XTLInterpreter(formula, new Entity(), null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo(bool.FalseString));
        }

        [Test]
        public void It_Should_Be_True_When_One_Condition_In_Or_Is_True()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "Or(false, false, true, true, true)";
            var result = new XTLInterpreter(formula, new Entity(), null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo(bool.TrueString));
        }

        [Test]
        public void It_Should_Be_False_When_All_Conditions_In_Or_Are_False()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "And(false, false, true, false)";
            var result = new XTLInterpreter(formula, new Entity(), null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo(bool.FalseString));
        }

        [Test]
        public void Strings_Should_Also_Be_Possible_With_Single_Quotes()
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

            var formula = "Concat(Value ('subject'), '\"', Value(\"subject\"), \"'\")";
            var result = new XTLInterpreter(formula, email, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("TestSubject\"TestSubject'"));
        }

        [Test]
        public void It_Should_Retrieve_Old_Values_From_Audit()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var contact = new Entity
            {
                LogicalName = "contact",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "name", "newName" }
                }
            };

            context.AddExecutionMock<RetrieveRecordChangeHistoryRequest>((OrganizationRequest req) =>
            {
                var auditDetail = new AttributeAuditDetail
                {
                    OldValue = new Entity
                    {
                        LogicalName = "contact",
                        Id = Guid.NewGuid(),
                        ["name"] = "oldName"
                    }
                };

                var collection = new AuditDetailCollection();
                collection.AuditDetails.Add(auditDetail);

                return new RetrieveRecordChangeHistoryResponse
                {
                    Results = new ParameterCollection
                    {
                        { "AuditDetailCollection", collection }
                    }
                };
            });

            var formula = "RetrieveAudit (PrimaryRecord(), \"name\")";
            var result = new XTLInterpreter(formula, contact, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("oldName"));
        }

        [Test]
        public void It_Should_Get_Id_From_Entity()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var contact = new Entity
            {
                LogicalName = "contact",
                Id = new Guid("a99b0170-d463-4f70-8db9-e2d8ee348f5f"),
                Attributes = new AttributeCollection
                {
                    { "name", "newName" }
                }
            };

            var formula = "RecordId (PrimaryRecord())";
            var result = new XTLInterpreter(formula, contact, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("a99b0170-d463-4f70-8db9-e2d8ee348f5f"));
        }

        [Test]
        public void It_Should_Get_LogicalName_From_Entity()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var contact = new Entity
            {
                LogicalName = "contact",
                Id = new Guid("a99b0170-d463-4f70-8db9-e2d8ee348f5f"),
                Attributes = new AttributeCollection
                {
                    { "name", "newName" }
                }
            };

            var formula = "RecordLogicalName (PrimaryRecord())";
            var result = new XTLInterpreter(formula, contact, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("contact"));
        }

        [Test]
        public void It_Should_Get_Id_From_Entity_Reference()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var contact = new Entity
            {
                LogicalName = "contact",
                Id = new Guid("a99b0170-d463-4f70-8db9-e2d8ee348f5f"),
                Attributes = new AttributeCollection
                {
                    { "name", "newName" }
                }
            };

            var account = new Entity
            {
                LogicalName = "account",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "primarycontactid", contact.ToEntityReference() }
                }
            };

            var formula = "RecordId (Value(\"primarycontactid\"))";
            var result = new XTLInterpreter(formula, account, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("a99b0170-d463-4f70-8db9-e2d8ee348f5f"));
        }

        [Test]
        public void It_Should_Get_Id_From_Entity_Reference_With_Format()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var contact = new Entity
            {
                LogicalName = "contact",
                Id = new Guid("a99b0170-d463-4f70-8db9-e2d8ee348f5f"),
                Attributes = new AttributeCollection
                {
                    { "name", "newName" }
                }
            };

            var account = new Entity
            {
                LogicalName = "account",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "primarycontactid", contact.ToEntityReference() }
                }
            };

            var formula = "RecordId (Value(\"primarycontactid\"), { format: \"N\" })";
            var result = new XTLInterpreter(formula, account, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("a99b0170d4634f708db9e2d8ee348f5f"));
        }

        [Test]
        public void It_Should_Compare_Ids_In_IsEqual()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var contact = new Entity
            {
                LogicalName = "contact",
                Id = new Guid("a99b0170-d463-4f70-8db9-e2d8ee348f5f"),
                Attributes = new AttributeCollection
                {
                    { "name", "newName" }
                }
            };

            var formula = "If ( IsEqual ( RecordId ( PrimaryRecord() ), \"a99b0170-d463-4f70-8db9-e2d8ee348f5f\"), \"Match\", \"No Match\")";
            var result = new XTLInterpreter(formula, contact, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("Match"));

            formula = "If ( IsEqual ( RecordId ( PrimaryRecord() ), \"b99b0170-d463-4f70-8db9-e2d8ee348f5f\"), \"Match\", \"No Match\")";
            result = new XTLInterpreter(formula, contact, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("No Match"));
        }

        [Test]
        public void It_Should_Get_LogicalName_From_Entity_Reference()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var contact = new Entity
            {
                LogicalName = "contact",
                Id = new Guid("a99b0170-d463-4f70-8db9-e2d8ee348f5f"),
                Attributes = new AttributeCollection
                {
                    { "name", "newName" }
                }
            };

            var account = new Entity
            {
                LogicalName = "account",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "primarycontactid", contact.ToEntityReference() }
                }
            };

            var formula = "RecordLogicalName (Value(\"primarycontactid\"))";
            var result = new XTLInterpreter(formula, account, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("contact"));
        }

        [Test]
        public void It_Should_Union_Arrays()
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

            var formula = "Join(\" \", Union ([\"Lord\"], [\"of\", \"the\"], [\"Rings\"]))";
            var result = new XTLInterpreter(formula, email, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("Lord of the Rings"));
        }

        [Test]
        public void It_Should_Join_Non_Text_values()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var regardingObject = new Entity
            {
                LogicalName = "account",
                Id = Guid.NewGuid(),
                ["name"] = "Test"
            };

            var email = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "subject", "TestSubject" },
                    { "oss_optionset", new OptionSetValue(1) },
                    { "regardingobjectid", regardingObject.ToEntityReference() }
                }
            };

            var metadata = new EntityMetadata { LogicalName = "email" };
            var field = typeof(EntityMetadata).GetField("_attributes", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(metadata, new AttributeMetadata[] { new PicklistAttributeMetadata { LogicalName = "oss_optionset", OptionSet = new OptionSetMetadata { Options = { new OptionMetadata { Value = 1, Label = new Label("Value1", 1031) } } } } });

            context.InitializeMetadata(metadata);

            context.Initialize(new[] { regardingObject });

            var formula = "Join(\" \", [ Value(\"subject\"), Value(\"oss_optionset\", { optionSetLcid: 1031 }), Value(\"regardingobjectid.name\") ], true)";
            var result = new XTLInterpreter(formula, email, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("TestSubject Value1 Test"));
        }

        [Test]
        public void It_Should_Execute_Lambdas_On_Map()
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

            var formula = "Join(\" \", Map([\"Lord\", \"of\", \"the\", \"Rings\"], (e) => Substring(e, 0, 1)))";
            var result = new XTLInterpreter(formula, email, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("L o t R"));
        }

        [Test]
        public void It_Should_Execute_Lambdas_On_Filter()
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

            var formula = "Join(\" \", Filter([\"Lord\", \"of\", \"the\", \"Rings\"], (e) => Not(IsEqual(IndexOf(e, \"o\"), -1))))";
            var result = new XTLInterpreter(formula, email, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("Lord of"));
        }

        [Test]
        public void Coalesce_Should_Return_First_Non_Null_Value()
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

            var formula = "Coalesce(Value(\"subject2\"), Value(\"subject\"))";
            var result = new XTLInterpreter(formula, email, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("TestSubject"));
        }

        [Test]
        public void It_Should_Sort_Native_Value_Array()
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

            var formula = "Join(\" \", Sort( Union ([\"Lord\"], [\"of\", \"the\"], [\"Rings\"])))";
            var result = new XTLInterpreter(formula, email, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("Lord of Rings the"));
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

        public void It_Should_Use_Custom_Link_Text()
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

            var formula = "RecordUrl ( PrimaryRecord ( ), { linkText: \"Click me\" } )";
            var result = string.Empty;

            Assert.That(() => result = new XTLInterpreter(formula, email, new OrganizationConfig { OrganizationUrl = "https://crm/" }, service, tracing).Produce(), Throws.Nothing);

            Assert.That(result, Is.EqualTo($"<a href=\"https://crm/main.aspx?etn={email.LogicalName}&id={email.Id}&newWindow=true&pagetype=entityrecord\">Click me</a>"));
        }

        public void It_Should_Insert_Organization_Url()
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

            var formula = "OrganizationUrl ()";
            var result = string.Empty;

            Assert.That(() => result = new XTLInterpreter(formula, email, new OrganizationConfig { OrganizationUrl = "https://crm/" }, service, tracing).Produce(), Throws.Nothing);

            Assert.That(result, Is.EqualTo("https://crm/"));
        }

        [Test]
        public void It_Should_Insert_Organization_Url_As_Html()
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

            var formula = "OrganizationUrl ({ asHtml: true, linkText: \"Link Text\", urlSuffix: \"suffix\"})";
            var result = string.Empty;

            Assert.That(() => result = new XTLInterpreter(formula, email, new OrganizationConfig { OrganizationUrl = "https://crm/" }, service, tracing).Produce(), Throws.Nothing);

            Assert.That(result, Is.EqualTo("<a href=\"https://crm/suffix\">Link Text</a>"));
        }

        [Test]
        public void It_Should_Insert_Organization_Url_As_Html_With_Missing_Parameters()
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

            var formula = "OrganizationUrl ({ asHtml: true })";
            var result = string.Empty;

            Assert.That(() => result = new XTLInterpreter(formula, email, new OrganizationConfig { OrganizationUrl = "https://crm/" }, service, tracing).Produce(), Throws.Nothing);

            Assert.That(result, Is.EqualTo("<a href=\"https://crm/\">https://crm/</a>"));
        }

        [Test]
        public void It_Should_Insert_AppId_Into_Link()
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

            var formula = "RecordUrl ( PrimaryRecord ( ), { appId: \"123456\", linkText: \"Click me\" } )";
            var result = string.Empty;

            Assert.That(() => result = new XTLInterpreter(formula, email, new OrganizationConfig { OrganizationUrl = "https://crm/" }, service, tracing).Produce(), Throws.Nothing);

            Assert.That(result, Is.EqualTo($"<a href=\"https://crm/main.aspx?etn={email.LogicalName}&id={email.Id}&newWindow=true&pagetype=entityrecord&appid=123456\">Click me</a>"));
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
        public void It_Should_Resolve_Link_Text_Formulas()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var contact = new Entity
            {
                LogicalName = "contact",
                Id = new Guid("a99b0170-d463-4f70-8db9-e2d8ee348f5f"),
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

            var formula = "RecordUrl ( Value(\"regardingobjectid\"), { linkText: Value(\"regardingobjectid.firstname\") })";

            var result1 = new XTLInterpreter(formula, emailWithSubject, new OrganizationConfig { OrganizationUrl = "https://test.local" }, service, tracing).Produce();

            Assert.That(result1, Is.EqualTo("<a href=\"https://test.local/main.aspx?etn=contact&id=a99b0170-d463-4f70-8db9-e2d8ee348f5f&newWindow=true&pagetype=entityrecord\">Frodo</a>"));
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

            var formula = "Value(\"subject\", { explicitTarget: First(Fetch(\"<fetch no-lock='true'><entity name='task'><attribute name='description' /><attribute name='subject' /><filter><condition attribute='regardingobjectid' operator='eq' value='{1}' /></filter></entity></fetch>\", Array(Value(\"regardingobjectid\"))))})";

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

            var formula = "Value(\"subject\", { explicitTarget: First(Fetch(\"<fetch no-lock='true'><entity name='task'><attribute name='description' /><attribute name='subject' /><filter><condition attribute='regardingobjectid' operator='eq' value='{1}' /></filter></entity></fetch>\", Array (Value(\"regardingobjectid\"))))})";

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

        [Test]
        public void It_Should_Concatenate_Strings()
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

            var formula = "Concat(Value (\"subject\"), \" \", Value (\"subject\"))";
            var result = new XTLInterpreter(formula, email, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("TestSubject TestSubject"));
        }

        [Test]
        public void It_Should_Not_Fail_On_Null_Valued_Formula()
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

            string result = null;
            Assert.That(() => result = new XTLInterpreter(null, email, null, service, tracing).Produce(), Throws.Nothing);
            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void It_Should_Concatenate_Array_Values_For_String_Representation()
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

            string result = null;
            Assert.That(() => result = new XTLInterpreter("Array(\"This\", null, \"is\", \"a\", \"test\")", email, null, service, tracing).Produce(), Throws.Nothing);
            Assert.That(result, Is.EqualTo("This, , is, a, test"));
        }
        
        [Test]
        public void It_Should_Not_Fail_If_No_Primary_Entity_Available_In_Fetch()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var task = new Entity
            {
                LogicalName = "task",
                Id = Guid.NewGuid()
            };

            context.Initialize(task);
            
            var formula = "Fetch(\"<fetch no-lock='true'><entity name='task'><attribute name='description' /><attribute name='subject' /></entity></fetch>\")";

            Assert.That(() => new XTLInterpreter(formula, null, null, service, tracing).Produce(), Throws.Nothing);
        }

        [Test]
        public void It_Should_Allow_To_Use_Concat_In_Fetch()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "Concat(\"<fetch no-lock='true'><entity name='task'><attribute name='description' /><attribute name='subject' /><filter>\", If( IsEqual ( true, true ), \"<condition attribute='regardingobjectid' operator='eq' value='{1}' />\", \"<condition attribute='regardingobjectid' operator='eq-null' />\"), \"</filter></entity></fetch>\")";

            var result1 = new XTLInterpreter(formula, null, null, service, tracing).Produce();

            Assert.That(result1, Is.EqualTo("<fetch no-lock='true'><entity name='task'><attribute name='description' /><attribute name='subject' /><filter><condition attribute='regardingobjectid' operator='eq' value='{1}' /></filter></entity></fetch>"));
        }

        [Test]
        public void It_Should_Join_Values()
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

            string result = null;
            Assert.That(() => result = new XTLInterpreter(@"Join ( "","", Array ( Value(""subject""), Value(""none""), Value(""subject"")))", email, null, service, tracing).Produce(), Throws.Nothing);
            Assert.That(result, Is.EqualTo("TestSubject,,TestSubject"));
        }

        [Test]
        public void It_Should_Join_Values_And_Remove_Empty_Entries()
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

            string result = null;
            Assert.That(() => result = new XTLInterpreter(@"Join ( "","", Array ( Value(""subject""), Value(""none""), Value(""subject"")), true)", email, null, service, tracing).Produce(), Throws.Nothing);
            Assert.That(result, Is.EqualTo("TestSubject,TestSubject"));
        }

        [Test]
        public void It_Should_Join_Values_And_Remove_Empty_Entries_With_Native_Array()
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

            string result = null;
            Assert.That(() => result = new XTLInterpreter(@"Join ( "","", [ Value(""subject""), Value(""none""), Value(""subject"") ], true)", email, null, service, tracing).Produce(), Throws.Nothing);
            Assert.That(result, Is.EqualTo("TestSubject,TestSubject"));
        }

        [Test]
        public void It_Should_Insert_New_Line()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "NewLine()";

            var result1 = new XTLInterpreter(formula, null, null, service, tracing).Produce();

            Assert.That(result1, Is.EqualTo(Environment.NewLine));
        }

        [Test]
        public void It_Should_Parse_Dictionaries()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "{ retrieveLabels: true, returnOptionSetValue: IsEqual(true, false) }";

            var result = new XTLInterpreter(formula, null, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("retrieveLabels: True, returnOptionSetValue: False"));
        }

        [Test]
        public void It_Should_Count_Length_Of_Arrays()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "Length( [ \"A\", \"B\", \"C\" ] )";

            var result = new XTLInterpreter(formula, null, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("3"));
        }

        [Test]
        public void It_Should_Count_Length_Of_String()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = "Length( \"ABC\" )";

            var result = new XTLInterpreter(formula, null, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("3"));
        }

        [Test]
        public void It_Should_Not_Fail_On_Complex_Template_Field()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var formula = @"If (
    IsNull(Value(""oss_htmltemplate"")),
    Value(""oss_subject"", { explicitTarget: First(Fetch(""<fetch no-lock='true'><entity name='oss_htmltemplate'><attribute name='oss_subject' /><filter><condition attribute='oss_uniquename' operator='eq' value='{1}' /></filter></entity></fetch>"", [Value(""oss_htmltemplateuniquename"")])) }),
    Value(""oss_htmltemplate.oss_subject"")
)";

            var template = new Entity
            {
                Id = Guid.NewGuid(),
                LogicalName = "oss_htmltemplate",
                ["oss_uniquename"] = "tst",
                ["oss_subject"] = "Test",
                ["oss_html"] = "<html />"
            };

            var email = new Entity
            {
                LogicalName = "email",
                ["oss_htmltemplate"] = template.ToEntityReference()
            };

            context.Initialize(template);

            var result = new XTLInterpreter(formula, email, null, service, tracing).Produce();

            Assert.That(result, Is.EqualTo("Test"));
        }

        [Test]
        public void First_Should_Work_On_Entity_Collection()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var user = new Entity
            {
                LogicalName = "systemuser",
                Id = Guid.NewGuid(),
                Attributes =
                {
                    { "fullname", "Bilbo Baggins" }
                }
            };

            var email = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "to", new EntityCollection(
                        new List<Entity>
                        {
                            new Entity
                            {
                                LogicalName = "activityparty",
                                Id = Guid.NewGuid(),
                                Attributes =
                                {
                                    { "partyid", user.ToEntityReference() }
                                }
                            }
                        })
                    }
                }
            };

            context.Initialize(new Entity[] { user, email });

            var result = new XTLInterpreter(@"Value ( ""partyid.fullname"", { explicitTarget: First( Value(""to"") ) } )", email, null, service, tracing).Produce();
            Assert.That(result, Is.EqualTo("Bilbo Baggins"));
        }

        [Test]
        public void Last_Should_Work_On_Entity_Collection()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var user = new Entity
            {
                LogicalName = "systemuser",
                Id = Guid.NewGuid(),
                Attributes =
                {
                    { "fullname", "Bilbo Baggins" }
                }
            };

            var email = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "to", new EntityCollection(
                        new List<Entity>
                        {
                            new Entity
                            {
                                LogicalName = "activityparty",
                                Id = Guid.NewGuid(),
                                Attributes =
                                {
                                    { "partyid", null }
                                }
                            },
                            new Entity
                            {
                                LogicalName = "activityparty",
                                Id = Guid.NewGuid(),
                                Attributes =
                                {
                                    { "partyid", user.ToEntityReference() }
                                }
                            }
                        })
                    }
                }
            };

            context.Initialize(new Entity[] { user, email });

            var result = new XTLInterpreter(@"Value ( ""partyid.fullname"", { explicitTarget: Last( Value(""to"") ) } )", email, null, service, tracing).Produce();
            Assert.That(result, Is.EqualTo("Bilbo Baggins"));
        }
    }
}
