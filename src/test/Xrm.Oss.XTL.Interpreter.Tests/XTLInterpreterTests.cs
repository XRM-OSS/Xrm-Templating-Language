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
            var result = string.Empty;

            Assert.That(() => result = new XTLInterpreter(formula, email, new OrganizationConfig { OrganizationUrl = "https://crm/" }, null).Produce(), Throws.Nothing);

            Assert.That(result, Is.EqualTo($"<a href=\"https://crm/main.aspx?etn={email.LogicalName}&id={email.Id}&newWindow=true&pagetype=entityrecord\">https://crm/main.aspx?etn={email.LogicalName}&id={email.Id}&newWindow=true&pagetype=entityrecord</a>"));
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

        [Test]
        public void It_Should_Create_Sub_Record_Table_Without_Url()
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

            context.Initialize(new Entity[] { contact, task, task2 });

            var formula = "SubRecordTable(PrimaryRecord(), \"task\", \"regardingobjectid\", false, \"subject\", \"description\")";

            var expected = @"<table>
<tr><th style=""border:1px solid black;text-align:left;padding:1px 15px 1px 5px"">Subject Label</th>
<th style=""border:1px solid black;text-align:left;padding:1px 15px 1px 5px"">Description Label</th>
<tr />
<tr>
<td style=""border:1px solid black;padding:1px 15px 1px 5px"">Task 1</td>
<td style=""border:1px solid black;padding:1px 15px 1px 5px"">Description 1</td>
<tr />
<tr>
<td style=""border:1px solid black;padding:1px 15px 1px 5px"">Task 2</td>
<td style=""border:1px solid black;padding:1px 15px 1px 5px"">Description 2</td>
<tr />
</table>".Replace("\r", "").Replace("\n", "");

            var result = new XTLInterpreter(formula, contact, null, service).Produce();

            Assert.That(result.Replace("\r", "").Replace("\n", ""), Is.EqualTo(expected));
        }

        [Test]
        public void It_Should_Create_Sub_Record_Table_With_Url()
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

            context.Initialize(new Entity[] { contact, task, task2 });

            var formula = "SubRecordTable(PrimaryRecord(), \"task\", \"regardingobjectid\", true, \"subject\", \"description\")";

            var expected = @"<table>
<tr><th style=""border:1px solid black;text-align:left;padding:1px 15px 1px 5px"">Subject Label</th>
<th style=""border:1px solid black;text-align:left;padding:1px 15px 1px 5px"">Description Label</th>
<th style=""border:1px solid black;text-align:left;padding:1px 15px 1px 5px"">URL</th>
<tr />
<tr>
<td style=""border:1px solid black;padding:1px 15px 1px 5px"">Task 1</td>
<td style=""border:1px solid black;padding:1px 15px 1px 5px"">Description 1</td>
<td style=""border:1px solid black;padding:1px 15px 1px 5px""><a href=""https://test.local/main.aspx?etn=task&id=76f167d6-35b3-44ae-b2a0-9373dee13e82&newWindow=true&pagetype=entityrecord"">https://test.local/main.aspx?etn=task&id=76f167d6-35b3-44ae-b2a0-9373dee13e82&newWindow=true&pagetype=entityrecord</a></td>
<tr />
<tr>
<td style=""border:1px solid black;padding:1px 15px 1px 5px"">Task 2</td>
<td style=""border:1px solid black;padding:1px 15px 1px 5px"">Description 2</td>
<td style=""border:1px solid black;padding:1px 15px 1px 5px""><a href=""https://test.local/main.aspx?etn=task&id=5c0370f2-9b79-4abc-86d6-09260d5bbfed&newWindow=true&pagetype=entityrecord"">https://test.local/main.aspx?etn=task&id=5c0370f2-9b79-4abc-86d6-09260d5bbfed&newWindow=true&pagetype=entityrecord</a></td>
<tr />
</table>".Replace("\r", "").Replace("\n", "");

            var result = new XTLInterpreter(formula, contact, new OrganizationConfig { OrganizationUrl = "https://test.local" }, service).Produce();

            Assert.That(result.Replace("\r", "").Replace("\n", ""), Is.EqualTo(expected));
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
