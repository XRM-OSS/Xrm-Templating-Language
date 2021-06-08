using FakeXrmEasy;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xrm.Oss.XTL.Templating;

namespace Xrm.Oss.XTL.Interpreter.Tests
{
    [TestFixture]
    public class RecordTableTests
    {
        private void SetupContext(XrmFakedContext context)
        {
            context.AddExecutionMock<RetrieveEntityRequest>(req =>
            {
                var entityMetadata = new EntityMetadata();

                var property = entityMetadata
                    .GetType()
                    .GetProperty("Attributes");

                var subjectLabel = new StringAttributeMetadata
                {
                    LogicalName = "subject",
                    DisplayName = new Label
                    {
                        UserLocalizedLabel = new LocalizedLabel
                        {
                            LanguageCode = 1033,
                            Label = "Subject Label"
                        }
                    }
                };

                var descriptionLabel = new StringAttributeMetadata
                {
                    LogicalName = "description",
                    DisplayName = new Label
                    {
                        UserLocalizedLabel = new LocalizedLabel
                        {
                            LanguageCode = 1033,
                            Label = "Description Label"
                        }
                    }
                };

                var attributes = new AttributeMetadata[] { subjectLabel, descriptionLabel };
                property.GetSetMethod(true).Invoke(entityMetadata, new object[] { attributes });

                return new RetrieveEntityResponse
                {
                    Results = new ParameterCollection
                    {
                        { "EntityMetadata", entityMetadata }
                    }
                };
            });
        }

        [Test]
        public void It_Should_Not_Fail_On_Empty_Table()
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

            SetupContext(context);
            context.Initialize(new Entity[] { contact });

            var formula = "RecordTable(Fetch(\"<fetch no-lock='true'><entity name='task'><attribute name='description' /><attribute name='subject' /><filter><condition attribute='regardingobjectid' operator='eq' value='{0}' /></filter></entity></fetch>\"), \"task\", Array(\"subject\", \"description\"))";

            var expected = @"<table>
<tr><th style=""border:1px solid black;text-align:left;padding:1px 15px 1px 5px;"">Subject Label</th>
<th style=""border:1px solid black;text-align:left;padding:1px 15px 1px 5px;"">Description Label</th>
</tr>
<tr>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">Task 1</td>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">Description 1</td>
</tr>
<tr>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">Task 2</td>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">Description 2</td>
</tr>
</table>".Replace("\r", "").Replace("\n", "");

            Assert.That(() => new XTLInterpreter(formula, contact, null, service, tracing).Produce(), Throws.Nothing);
        }

        [Test]
        public void It_Should_Use_Custom_Styles()
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

            var task2 = new Entity
            {
                LogicalName = "task",
                Id = Guid.NewGuid(),
                Attributes =
                {
                    { "subject", "Task 2" },
                    { "description", "Description 2" },
                    { "regardingobjectid", contact.ToEntityReference() }
                }
            };

            SetupContext(context);
            context.Initialize(new Entity[] { contact, task, task2 });

            var formula = "RecordTable(Fetch(\"<fetch no-lock='true'><entity name='task'><attribute name='description' /><attribute name='subject' /><filter><condition attribute='regardingobjectid' operator='eq' value='{0}' /></filter></entity></fetch>\"), \"task\", [\"subject\", \"description\"], { tableStyle: \"border:1px solid green;\", headerStyle: \"border:1px solid orange;\", dataStyle: \"border:1px solid red;\"})";

            var expected = @"<table style=""border:1px solid green;"">
<tr><th style=""border:1px solid orange;"">Subject Label</th>
<th style=""border:1px solid orange;"">Description Label</th>
</tr>
<tr>
<td style=""border:1px solid red;"">Task 1</td>
<td style=""border:1px solid red;"">Description 1</td>
</tr>
<tr>
<td style=""border:1px solid red;"">Task 2</td>
<td style=""border:1px solid red;"">Description 2</td>
</tr>
</table>".Replace("\r", "").Replace("\n", "");

            var result = new XTLInterpreter(formula, contact, null, service, tracing).Produce();
            Assert.That(result.Replace("\r", "").Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_Should_Use_Uneven_And_Even_Row_Styles()
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

            var task2 = new Entity
            {
                LogicalName = "task",
                Id = Guid.NewGuid(),
                Attributes =
                {
                    { "subject", "Task 2" },
                    { "description", "Description 2" },
                    { "regardingobjectid", contact.ToEntityReference() }
                }
            };

            SetupContext(context);
            context.Initialize(new Entity[] { contact, task, task2 });

            var formula = "RecordTable(Fetch(\"<fetch no-lock='true'><entity name='task'><attribute name='description' /><attribute name='subject' /><filter><condition attribute='regardingobjectid' operator='eq' value='{0}' /></filter></entity></fetch>\"), \"task\", [\"subject\", \"description\"], { tableStyle: \"border:1px solid green;\", headerStyle: \"border:1px solid orange;\", evenDataStyle: \"border:1px solid white;\", unevenDataStyle: \"border:1px solid blue;\"})";

            var expected = @"<table style=""border:1px solid green;"">
<tr><th style=""border:1px solid orange;"">Subject Label</th>
<th style=""border:1px solid orange;"">Description Label</th>
</tr>
<tr>
<td style=""border:1px solid white;"">Task 1</td>
<td style=""border:1px solid white;"">Description 1</td>
</tr>
<tr>
<td style=""border:1px solid blue;"">Task 2</td>
<td style=""border:1px solid blue;"">Description 2</td>
</tr>
</table>".Replace("\r", "").Replace("\n", "");

            var result = new XTLInterpreter(formula, contact, null, service, tracing).Produce();
            Assert.That(result.Replace("\r", "").Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_Should_Add_Custom_Column_Labels()
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

            var task2 = new Entity
            {
                LogicalName = "task",
                Id = Guid.NewGuid(),
                Attributes =
                {
                    { "subject", "Task 2" },
                    { "description", "Description 2" },
                    { "regardingobjectid", contact.ToEntityReference() }
                }
            };

            SetupContext(context);
            context.Initialize(new Entity[] { contact, task, task2 });

            var formula = "RecordTable(Fetch(\"<fetch no-lock='true'><entity name='task'><attribute name='description' /><attribute name='subject' /><filter><condition attribute='regardingobjectid' operator='eq' value='{0}' /></filter></entity></fetch>\"), \"task\", Array(\"subject:Overridden Subject Label\", \"description\"))";

            var expected = @"<table>
<tr><th style=""border:1px solid black;text-align:left;padding:1px 15px 1px 5px;"">Overridden Subject Label</th>
<th style=""border:1px solid black;text-align:left;padding:1px 15px 1px 5px;"">Description Label</th>
</tr>
<tr>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">Task 1</td>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">Description 1</td>
</tr>
<tr>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">Task 2</td>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">Description 2</td>
</tr>
</table>".Replace("\r", "").Replace("\n", "");

            var result = new XTLInterpreter(formula, contact, null, service, tracing).Produce();

            Assert.That(result.Replace("\r", "").Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_Should_Create_Sub_Record_Table_Without_Url()
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

            var task2 = new Entity
            {
                LogicalName = "task",
                Id = Guid.NewGuid(),
                Attributes =
                {
                    { "subject", "Task 2" },
                    { "description", "Description 2" },
                    { "regardingobjectid", contact.ToEntityReference() }
                }
            };

            SetupContext(context);
            context.Initialize(new Entity[] { contact, task, task2 });

            var formula = "RecordTable(Fetch(\"<fetch no-lock='true'><entity name='task'><attribute name='description' /><attribute name='subject' /><filter><condition attribute='regardingobjectid' operator='eq' value='{0}' /></filter></entity></fetch>\"), \"task\", Array(\"subject\", \"description\"))";

            var expected = @"<table>
<tr><th style=""border:1px solid black;text-align:left;padding:1px 15px 1px 5px;"">Subject Label</th>
<th style=""border:1px solid black;text-align:left;padding:1px 15px 1px 5px;"">Description Label</th>
</tr>
<tr>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">Task 1</td>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">Description 1</td>
</tr>
<tr>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">Task 2</td>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">Description 2</td>
</tr>
</table>".Replace("\r", "").Replace("\n", "");

            var result = new XTLInterpreter(formula, contact, null, service, tracing).Produce();

            Assert.That(result.Replace("\r", "").Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_Should_Be_Able_To_Display_Columns_Per_Entity()
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
                }
            };

            SetupContext(context);
            context.Initialize(new Entity[] { contact, task });

            var formula = "RecordTable(Union(Fetch(\"<fetch no-lock='true'><entity name='contact'><attribute name='firstname' /></entity></fetch>\"), Fetch(\"<fetch no-lock='true'><entity name='task'><attribute name='subject' /></entity></fetch>\")), \"task\", [{ label: \"Combined Column\", nameByEntity: { contact: \"firstname\", task: \"subject\" } }])";

            var expected = @"<table>
<tr><th style=""border:1px solid black;text-align:left;padding:1px 15px 1px 5px;"">Combined Column</th>
</tr>
<tr>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">Frodo</td>
</tr>
<tr>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">Task 1</td>
</tr>
</table>".Replace("\r", "").Replace("\n", "");

            var result = new XTLInterpreter(formula, contact, null, service, tracing).Produce();

            Assert.That(result.Replace("\r", "").Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_Should_Sort_Sub_Record_Table()
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
                    { "regardingobjectid", contact.ToEntityReference() },
                    { "createdon", DateTime.UtcNow }
                }
            };

            var task2 = new Entity
            {
                LogicalName = "task",
                Id = Guid.NewGuid(),
                Attributes =
                {
                    { "subject", "Task 2" },
                    { "description", "Description 2" },
                    { "regardingobjectid", contact.ToEntityReference() },
                    { "createdon", DateTime.UtcNow.AddDays(-1) }

                }
            };

            SetupContext(context);
            context.Initialize(new Entity[] { contact, task, task2 });

            var formula = "RecordTable(Sort(Fetch(\"<fetch no-lock='true'><entity name='task'><attribute name='subject' /><attribute name='description' /><attribute name='createdon' /></entity></fetch>\"), { property: \"createdon\" }), \"task\", Array(\"subject\", \"description\"))";

            var expected = @"<table>
<tr><th style=""border:1px solid black;text-align:left;padding:1px 15px 1px 5px;"">Subject Label</th>
<th style=""border:1px solid black;text-align:left;padding:1px 15px 1px 5px;"">Description Label</th>
</tr>
<tr>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">Task 2</td>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">Description 2</td>
</tr>
<tr>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">Task 1</td>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">Description 1</td>
</tr>
</table>".Replace("\r", "").Replace("\n", "");

            var result = new XTLInterpreter(formula, contact, null, service, tracing).Produce();

            Assert.That(result.Replace("\r", "").Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_Should_Insert_Static_Values()
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
                }
            };

            SetupContext(context);
            context.Initialize(new Entity[] { contact, task });

            var formula = "RecordTable(Union(Fetch(\"<fetch no-lock='true'><entity name='contact'><attribute name='firstname' /></entity></fetch>\"), Fetch(\"<fetch no-lock='true'><entity name='task'><attribute name='subject' /></entity></fetch>\")), \"task\", [{ label: \"Combined Column\", staticValueByEntity: { contact: \"staticContact\", task: \"staticTask\" } }])";

            var expected = @"<table>
<tr><th style=""border:1px solid black;text-align:left;padding:1px 15px 1px 5px;"">Combined Column</th>
</tr>
<tr>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">staticContact</td>
</tr>
<tr>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">staticTask</td>
</tr>
</table>".Replace("\r", "").Replace("\n", "");

            var result = new XTLInterpreter(formula, contact, null, service, tracing).Produce();

            Assert.That(result.Replace("\r", "").Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_Should_Use_Simple_Render_Functions()
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

            SetupContext(context);
            context.Initialize(new Entity[] { contact });

            var formula = "RecordTable(Fetch(\"<fetch no-lock='true'><entity name='contact'><attribute name='firstname' /></entity></fetch>\"), \"contact\", [{ name: \"firstname\", label: \"Custom Column\", renderFunction: (record, column) => Substring(Value(column, { explicitTarget: record }), 0, 1) }])";

            var expected = @"<table>
<tr><th style=""border:1px solid black;text-align:left;padding:1px 15px 1px 5px;"">Custom Column</th>
</tr>
<tr>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">F</td>
</tr>
</table>".Replace("\r", "").Replace("\n", "");

            var result = new XTLInterpreter(formula, contact, null, service, tracing).Produce();

            Assert.That(result.Replace("\r", "").Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_Should_Use_MultiValued_Render_Functions_With_Primary_Fields()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var userId = Guid.NewGuid();

            var userSettings = new Entity
            {
                LogicalName = "usersettings",
                Id = userId,
                ["timezonecode"] = 1
            };

            var timeZoneDefinition = new Entity
            {
                LogicalName = "timezonedefinition",
                Id = Guid.NewGuid(),
                ["standardname"] = "Eastern Standard Time",
                ["timezonecode"] = 1
            };

            var contact = new Entity
            {
                LogicalName = "contact",
                Id = Guid.NewGuid(),
                Attributes =
                {
                    { "createdon", new DateTime(2020, 02, 27, 8, 0, 0, DateTimeKind.Utc) }
                }
            };

            var email = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes =
                {
                    { "regardingobjectid", contact.ToEntityReference() },
                    { "ownerid", new EntityReference("systemuser", userId) }
                }
            };

            SetupContext(context);
            context.Initialize(new Entity[] { contact, email, userSettings, timeZoneDefinition });

            var formula = "RecordTable(Fetch(\"<fetch no-lock='true'><entity name='contact'><attribute name='createdon' /></entity></fetch>\"), \"contact\", [{ name: \"createdon\", label: \"Date\", renderFunction: (record, column) => DateToString(ConvertDateTime(Value(column, { explicitTarget: record }), { userId: Value(\"ownerid\") }), { format: \"yyyy-MM-dd hh:mm:ss\" }) }])";

            var expected = @"<table>
<tr><th style=""border:1px solid black;text-align:left;padding:1px 15px 1px 5px;"">Date</th>
</tr>
<tr>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">2020-02-27 03:00:00</td>
</tr>
</table>".Replace("\r", "").Replace("\n", "");

            var result = new XTLInterpreter(formula, email, null, service, tracing).Produce();

            Assert.That(result.Replace("\r", "").Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_Should_Use_MultiValued_Render_Functions_With_Per_Row_Fields()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            var tracing = context.GetFakeTracingService();

            var userId = Guid.NewGuid();

            var userSettings = new Entity
            {
                LogicalName = "usersettings",
                Id = userId,
                ["timezonecode"] = 1
            };

            var timeZoneDefinition = new Entity
            {
                LogicalName = "timezonedefinition",
                Id = Guid.NewGuid(),
                ["standardname"] = "Eastern Standard Time",
                ["timezonecode"] = 1
            };

            var contact = new Entity
            {
                LogicalName = "contact",
                Id = Guid.NewGuid(),
                Attributes =
                {
                    { "createdon", new DateTime(2020, 02, 27, 8, 0, 0, DateTimeKind.Utc) },
                    { "ownerid", new EntityReference("systemuser", userId) }
                }
            };

            var email = new Entity
            {
                LogicalName = "email",
                Id = Guid.NewGuid(),
                Attributes =
                {
                    { "regardingobjectid", contact.ToEntityReference() }
                }
            };

            SetupContext(context);
            context.Initialize(new Entity[] { contact, email, userSettings, timeZoneDefinition });

            var formula = "RecordTable(Fetch(\"<fetch no-lock='true'><entity name='contact'><attribute name='ownerid' /> <attribute name='createdon' /></entity></fetch>\"), \"contact\", [{ name: \"createdon\", label: \"Date\", renderFunction: (record, column) => DateToString(ConvertDateTime(Value(column, { explicitTarget: record }), { userId: Value(\"ownerid\", { explicitTarget: record }) }), { format: \"yyyy-MM-dd hh:mm:ss\" }) }])";

            var expected = @"<table>
<tr><th style=""border:1px solid black;text-align:left;padding:1px 15px 1px 5px;"">Date</th>
</tr>
<tr>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">2020-02-27 03:00:00</td>
</tr>
</table>".Replace("\r", "").Replace("\n", "");

            var result = new XTLInterpreter(formula, email, null, service, tracing).Produce();

            Assert.That(result.Replace("\r", "").Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_Should_Sort_Sub_Record_Table_Descending()
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
                    { "regardingobjectid", contact.ToEntityReference() },
                    { "createdon", DateTime.UtcNow }
                }
            };

            var task2 = new Entity
            {
                LogicalName = "task",
                Id = Guid.NewGuid(),
                Attributes =
                {
                    { "subject", "Task 2" },
                    { "description", "Description 2" },
                    { "regardingobjectid", contact.ToEntityReference() },
                    { "createdon", DateTime.UtcNow.AddDays(-1) }

                }
            };

            SetupContext(context);
            context.Initialize(new Entity[] { contact, task, task2 });

            var formula = "RecordTable(Sort(Fetch(\"<fetch no-lock='true'><entity name='task'><attribute name='subject' /><attribute name='description' /><attribute name='createdon' /></entity></fetch>\"), { property: \"createdon\", descending: true }), \"task\", Array(\"subject\", \"description\"))";

            var expected = @"<table>
<tr><th style=""border:1px solid black;text-align:left;padding:1px 15px 1px 5px;"">Subject Label</th>
<th style=""border:1px solid black;text-align:left;padding:1px 15px 1px 5px;"">Description Label</th>
</tr>
<tr>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">Task 1</td>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">Description 1</td>
</tr>
<tr>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">Task 2</td>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">Description 2</td>
</tr>
</table>".Replace("\r", "").Replace("\n", "");

            var result = new XTLInterpreter(formula, contact, null, service, tracing).Produce();

            Assert.That(result.Replace("\r", "").Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_Should_Not_Fail_On_Null_Value_When_Sorting()
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
                    { "regardingobjectid", contact.ToEntityReference() },
                    { "createdon", DateTime.UtcNow }
                }
            };

            var task2 = new Entity
            {
                LogicalName = "task",
                Id = Guid.NewGuid(),
                Attributes =
                {
                    { "subject", "Task 2" },
                    { "description", "Description 2" },
                    { "regardingobjectid", contact.ToEntityReference() }

                }
            };

            SetupContext(context);
            context.Initialize(new Entity[] { contact, task, task2 });

            var formula = "RecordTable(Sort(Fetch(\"<fetch no-lock='true'><entity name='task'><attribute name='subject' /><attribute name='description' /><attribute name='createdon' /></entity></fetch>\"), { property: \"createdon\" }), \"task\", Array(\"subject\", \"description\"))";

            var expected = @"<table>
<tr><th style=""border:1px solid black;text-align:left;padding:1px 15px 1px 5px;"">Subject Label</th>
<th style=""border:1px solid black;text-align:left;padding:1px 15px 1px 5px;"">Description Label</th>
</tr>
<tr>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">Task 1</td>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">Description 1</td>
</tr>
<tr>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">Task 2</td>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">Description 2</td>
</tr>
</table>".Replace("\r", "").Replace("\n", "");

            var result = new XTLInterpreter(formula, contact, null, service, tracing).Produce();

            Assert.That(result.Replace("\r", "").Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_Should_Create_Sub_Record_Table_With_Custom_Column_Style()
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

            var task2 = new Entity
            {
                LogicalName = "task",
                Id = Guid.NewGuid(),
                Attributes =
                {
                    { "subject", "Task 2" },
                    { "description", "Description 2" },
                    { "regardingobjectid", contact.ToEntityReference() }
                }
            };

            SetupContext(context);
            context.Initialize(new Entity[] { contact, task, task2 });

            var formula = "RecordTable(Fetch(\"<fetch no-lock='true'><entity name='task'><attribute name='description' /><attribute name='subject' /><filter><condition attribute='regardingobjectid' operator='eq' value='{0}' /></filter></entity></fetch>\"), \"task\", [{name: \"subject\", label: \"Custom Label\", style: \"width:70%\"}, {name: \"description\", style: \"width:30%\", mergeStyle: false}])";

            var expected = @"<table>
<tr><th style=""border:1px solid black;text-align:left;padding:1px 15px 1px 5px;width:70%"">Custom Label</th>
<th style=""width:30%"">Description Label</th>
</tr>
<tr>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;width:70%"">Task 1</td>
<td style=""width:30%"">Description 1</td>
</tr>
<tr>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;width:70%"">Task 2</td>
<td style=""width:30%"">Description 2</td>
</tr>
</table>".Replace("\r", "").Replace("\n", "");

            var result = new XTLInterpreter(formula, contact, null, service, tracing).Produce();

            Assert.That(result.Replace("\r", "").Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_Should_Create_Sub_Record_Table_With_Custom_Column_Style_And_Url()
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
                Id = new Guid("76f167d6-35b3-44ae-b2a0-9373dee13e82"),
                Attributes =
                {
                    { "subject", "Task 1" },
                    { "description", "Description 1" },
                    { "regardingobjectid", contact.ToEntityReference() }
                }
            };

            var task2 = new Entity
            {
                LogicalName = "task",
                Id = new Guid("5c0370f2-9b79-4abc-86d6-09260d5bbfed"),
                Attributes =
                {
                    { "subject", "Task 2" },
                    { "description", "Description 2" },
                    { "regardingobjectid", contact.ToEntityReference() }
                }
            };

            SetupContext(context);
            context.Initialize(new Entity[] { contact, task, task2 });

            var formula = "RecordTable(Fetch(\"<fetch no-lock='true'><entity name='task'><attribute name='description' /><attribute name='subject' /><filter><condition attribute='regardingobjectid' operator='eq' value='{0}' /></filter></entity></fetch>\"), \"task\", [{name: \"subject\", label: \"Custom Label\", style: \"width:70%\"}, {name: \"description\", style: \"width:30%\", mergeStyle: false}], { addRecordUrl: true })";

            var expected = @"<table>
<tr><th style=""border:1px solid black;text-align:left;padding:1px 15px 1px 5px;width:70%"">Custom Label</th>
<th style=""width:30%"">Description Label</th>
<th style=""border:1px solid black;text-align:left;padding:1px 15px 1px 5px;"">URL</th>
</tr>
<tr>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;width:70%"">Task 1</td>
<td style=""width:30%"">Description 1</td>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;""><a href=""https://test.local/main.aspx?etn=task&id=76f167d6-35b3-44ae-b2a0-9373dee13e82&newWindow=true&pagetype=entityrecord"">https://test.local/main.aspx?etn=task&id=76f167d6-35b3-44ae-b2a0-9373dee13e82&newWindow=true&pagetype=entityrecord</a></td>
</tr>
<tr>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;width:70%"">Task 2</td>
<td style=""width:30%"">Description 2</td>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;""><a href=""https://test.local/main.aspx?etn=task&id=5c0370f2-9b79-4abc-86d6-09260d5bbfed&newWindow=true&pagetype=entityrecord"">https://test.local/main.aspx?etn=task&id=5c0370f2-9b79-4abc-86d6-09260d5bbfed&newWindow=true&pagetype=entityrecord</a></td>
</tr>
</table>".Replace("\r", "").Replace("\n", "");

            var result = new XTLInterpreter(formula, contact, new OrganizationConfig { OrganizationUrl = "https://test.local" }, service, tracing).Produce();

            Assert.That(result.Replace("\r", "").Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_Should_Create_Sub_Record_Table_With_Url()
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
                Id = new Guid("76f167d6-35b3-44ae-b2a0-9373dee13e82"),
                Attributes =
                {
                    { "subject", "Task 1" },
                    { "description", "Description 1" },
                    { "regardingobjectid", contact.ToEntityReference() }
                }
            };

            var task2 = new Entity
            {
                LogicalName = "task",
                Id = new Guid("5c0370f2-9b79-4abc-86d6-09260d5bbfed"),
                Attributes =
                {
                    { "subject", "Task 2" },
                    { "description", "Description 2" },
                    { "regardingobjectid", contact.ToEntityReference() }
                }
            };

            SetupContext(context);
            context.Initialize(new Entity[] { contact, task, task2 });

            var formula = "RecordTable(Fetch(\"<fetch no-lock='true'><entity name='task'><attribute name='description' /><attribute name='subject' /><filter><condition attribute='regardingobjectid' operator='eq' value='{0}' /></filter></entity></fetch>\"), \"task\", Array(\"subject\", \"description\"), { addRecordUrl: true })";

            var expected = @"<table>
<tr><th style=""border:1px solid black;text-align:left;padding:1px 15px 1px 5px;"">Subject Label</th>
<th style=""border:1px solid black;text-align:left;padding:1px 15px 1px 5px;"">Description Label</th>
<th style=""border:1px solid black;text-align:left;padding:1px 15px 1px 5px;"">URL</th>
</tr>
<tr>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">Task 1</td>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">Description 1</td>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;""><a href=""https://test.local/main.aspx?etn=task&id=76f167d6-35b3-44ae-b2a0-9373dee13e82&newWindow=true&pagetype=entityrecord"">https://test.local/main.aspx?etn=task&id=76f167d6-35b3-44ae-b2a0-9373dee13e82&newWindow=true&pagetype=entityrecord</a></td>
</tr>
<tr>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">Task 2</td>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">Description 2</td>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;""><a href=""https://test.local/main.aspx?etn=task&id=5c0370f2-9b79-4abc-86d6-09260d5bbfed&newWindow=true&pagetype=entityrecord"">https://test.local/main.aspx?etn=task&id=5c0370f2-9b79-4abc-86d6-09260d5bbfed&newWindow=true&pagetype=entityrecord</a></td>
</tr>
</table>".Replace("\r", "").Replace("\n", "");

            var result = new XTLInterpreter(formula, contact, new OrganizationConfig { OrganizationUrl = "https://test.local" }, service, tracing).Produce();

            Assert.That(result.Replace("\r", "").Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_Should_Create_Sub_Record_Table_With_Url_And_Custom_Link_Text()
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
                Id = new Guid("76f167d6-35b3-44ae-b2a0-9373dee13e82"),
                Attributes =
                {
                    { "subject", "Task 1" },
                    { "description", "Description 1" },
                    { "regardingobjectid", contact.ToEntityReference() }
                }
            };

            var task2 = new Entity
            {
                LogicalName = "task",
                Id = new Guid("5c0370f2-9b79-4abc-86d6-09260d5bbfed"),
                Attributes =
                {
                    { "subject", "Task 2" },
                    { "description", "Description 2" },
                    { "regardingobjectid", contact.ToEntityReference() }
                }
            };

            SetupContext(context);
            context.Initialize(new Entity[] { contact, task, task2 });

            var formula = "RecordTable(Fetch(\"<fetch no-lock='true'><entity name='task'><attribute name='description' /><attribute name='subject' /><filter><condition attribute='regardingobjectid' operator='eq' value='{0}' /></filter></entity></fetch>\"), \"task\", Array(\"subject\", \"description\"), { addRecordUrl: true, linkText: \"Link\" })";

            var expected = @"<table>
<tr><th style=""border:1px solid black;text-align:left;padding:1px 15px 1px 5px;"">Subject Label</th>
<th style=""border:1px solid black;text-align:left;padding:1px 15px 1px 5px;"">Description Label</th>
<th style=""border:1px solid black;text-align:left;padding:1px 15px 1px 5px;"">URL</th>
</tr>
<tr>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">Task 1</td>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">Description 1</td>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;""><a href=""https://test.local/main.aspx?etn=task&id=76f167d6-35b3-44ae-b2a0-9373dee13e82&newWindow=true&pagetype=entityrecord"">Link</a></td>
</tr>
<tr>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">Task 2</td>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;"">Description 2</td>
<td style=""border:1px solid black;padding:1px 15px 1px 5px;""><a href=""https://test.local/main.aspx?etn=task&id=5c0370f2-9b79-4abc-86d6-09260d5bbfed&newWindow=true&pagetype=entityrecord"">Link</a></td>
</tr>
</table>".Replace("\r", "").Replace("\n", "");

            var result = new XTLInterpreter(formula, contact, new OrganizationConfig { OrganizationUrl = "https://test.local" }, service, tracing).Produce();

            Assert.That(result.Replace("\r", "").Replace("\n", ""), Is.EqualTo(expected));
        }
    }
}
