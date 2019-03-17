# XTL - Xrm Templating Language [![Build status](https://ci.appveyor.com/api/projects/status/skqv53ykh62587qp?svg=true)](https://ci.appveyor.com/project/DigitalFlow/xrm-templating-language) [![NuGet Badge](https://buildstats.info/nuget/Xrm.Oss.TemplatingLanguage.Sources)](https://www.nuget.org/packages/Xrm.Oss.TemplatingLanguage.Sources)

|Line Coverage|Branch Coverage|
|-----|-----------------|
|[![Line coverage](https://cdn.rawgit.com/digitalflow/xrm-templating-language/master/reports/badge_linecoverage.svg)](https://cdn.rawgit.com/digitalflow/xrm-templating-language/master/reports/index.htm)|[![Branch coverage](https://cdn.rawgit.com/digitalflow/xrm-templating-language/master/reports/badge_branchcoverage.svg)](https://cdn.rawgit.com/digitalflow/xrm-templating-language/master/reports/index.htm)|

A domain specific language for Dynamics CRM allowing for easy text template processing

## Purpose
XTL is a domain specific language created for easing text processing inside Dynamics CRM.
It is an interpreted programming language, with easy syntax for allowing everyone to use it.

The parsing and interpreting is done using a custom recursive descent parser implemented in C#.
It is embedded inside a plugin and does not need any external references, so that execution works in CRM online and on-premises environments.

## Where to get it
You can always download the latest release from the [releases page](https://github.com/DigitalFlow/Xrm-Templating-Language/releases).
Beware that the solution only supports CRM >= v8.0, as the XTL Editor makes use of the WebAPI. If you want to use it in CRM <= v7.0, you are able to use the Plugin Assemblies, but will have to configure everything manually.

As alternative:
Build it yourself by running `build.cmd`, or simply download from [AppVeyor](https://ci.appveyor.com/project/DigitalFlow/xrm-templating-language/build/artifacts).

## Requirements
XTL itself does not use any specific CRM features and is compatible with Dynamics CRM 2011 and higher.
Currently the Plugin is built against Dynamics 365 SDK however. Future releases may target specific CRM versions.
The template editor is only available in CRM 2016 and later, as it uses the Web Api.
The solution which can be downloaded from the releases supports CRM >= v8.0 starting from XTL v3.0.3 upwards.

## Examples
Examples of how to use XTL can be found in our [Wiki](https://github.com/DigitalFlow/Xrm-Templating-Language/wiki).

## How To Register
### Using XTL Editor
Inside the solution you imported, you'll find that there is a configuration page.
You can use this configuration page for testing templates as well as managing existing ones.
So creating of new template handlers inside your organization can be done completely using the editor.
It creates SDK message processing steps in the background, applying your custom settings combined with the needed default configs.

Impression:
![xtleditor](https://user-images.githubusercontent.com/4287938/38904112-b887b490-42a8-11e8-8b0b-1ccce115728e.png)

### Manual Way
Register the assemblies using the Plugin Registration Tool.
You can then create steps in the pre operation stage. If you're on update message, be sure to register a preimg with all attributes needed for generating your texts.

You'll need an unsecure json configuration per step.

Properties in there:
- target: The target field for the generated text
- templateField: The source field for the template (for "per record" templates, for example if replacing place holders inside emails). Since release v3.0.1 you can use XTL expressions in here as well, for getting your per record templates from a parent record for example. You could for example set `Value("parentcustomerid.oss_templatefield")` for getting your template from the oss_templatefield of a contact's account.
- template: A constant template that will be used for all records (for example when formatting addresses)
- executionCriteria: An XTL expression that should return true if the template should be applied, or false otherwise. If not set, default is to apply the logic. Comparison Operators such as IsEqual automatically return booleans.

Target always has to be set, in addition to either template or templateField.

Sample (For formatting emails):
```JSON
{
    "target": "description",
    "templateField": "description",
    "executionCriteria": "IsEqual(Value(\"directioncode\"), true)"
}
```

## Benefits
When dealing with the default e-mail editor of Dynamics CRM, the borders of what's possible are reached fast.
XTL aims to integrate flawlessly into Dynamics CRM, to enhance the text processing capabilities. It is not limited to any specific CRM entity.
Using XTL provides the following benefits:

- Using of the primary entity's related entity values, no matter how "far" they are away (i.e. regardingobject.parentaccountid.ownerid.fullname for using the full name of the owner of the company that the contact receiving an email belongs to).
- Using of child entity values, where the primary record does not hold the lookup (i.e. all tasks associated to an account)
- Conditional Branching (If-Then-Else constructs)
- Generating Record URLs for all records reachable using above expressions
- Easy to learn syntax (I admit that there are currently quite a few brackets needed for complex expressions)

## General Information
- Make sure that your functions only return strings (i.e. string constants) or values generated by Value() at the top level.
- XTL placeholders are identified using ${{placeholder}}  patterns currently. Please take care that your placeholder does not contain any }} patterns, as this would break the placeholder.
- Be careful when pasting text into records from this page or other formatted content. The text will contain format instructions, which break the placeholders. It is advised to copy the examples in here to NotePad++ or similar before inserting in e-mail HTML editor.

## Types
Native Types:
- String Constants (Alpha numeric text inside quotes)
- Integers (Digit expression)
- Doubles (Digits are separated by '.', a 'd' is appended to separate from decimals. E.g: 1.4d)
- Decimals (Digits are separated by '.', a 'm' is appended to separate from doubles. E.g: 1.4m)
- Booleans (true or false)
- Functions (Identifiers without quotes followed by parenthesis with parameters)
- Dictionaries (JS style objects, such as { propertyName: value }. Value can be a constant or another expression.

In addition, null is also a reserved keyword.

## Functions
### If
Takes 3 Parameters: A condition to check, a true action and a false action.
If the condition resolves to true, the true action is executed, otherwise the false action.

Example:
``` JavaScript
If( IsNull ( Value("subject") ), "No subject passed", Value("subject") )
```

### Or
Takes 2 ... n parameters and checks if any of them resolves to true.
If yes, then true is returned, otherwise false

Example:
``` JavaScript
Or ( IsNull( Value ("parentaccountid") ), IsNull( Value ("parentcontactid") ) )  
```

### And
Takes 2 ... n parameters and checks if all of them resolve to true.
If yes, then true is returned, otherwise false

Example:
``` JavaScript
And ( IsNull( Value ("parentaccountid") ), IsNull( Value ("parentcontactid") ) )  
```

### Not
Takes a parameter that resolves to a boolean and reverts its value.

Example:
``` JavaScript
Not ( IsNull( Value ("parentaccountid") ) )  
```

### IsNull
Checks if a parameter is null. If yes, true is returned, otherwise false.

Example:
``` JavaScript
IsNull( Value ("parentaccountid") )  
```

### IsEqual
Checks if two parameters are equal. If yes, true is returned, otherwise false.
The parameters are required to be of the same type.
OptionSetValues are compared with their respective integer values.

Example:
``` JavaScript
IsEqual ( Value ( "gendercode" ), 1 )
```

### IsLess
Checks if first parameter is less than the second. If yes, true is returned, otherwise false.

Example:
``` JavaScript
IsLess ( 1, 2 )
```

### IsLessEqual
Checks if first parameter is less or equal than the second. If yes, true is returned, otherwise false.

Example:
``` JavaScript
IsLessEqual ( 1, 2 )
```

### IsGreater
Checks if first parameter is greater than the second. If yes, true is returned, otherwise false.

Example:
``` JavaScript
IsGreater ( 1, 2 )
```

### IsGreaterEqual
Checks if first parameter is greater or equal than the second. If yes, true is returned, otherwise false.

Example:
``` JavaScript
IsGreaterEqual ( 1, 2 )
```

### Value
Returns the object value of the given property. If it is used as top level function, the matching string representation is returned.
The text function which was present in early releases was replaced by this, as the Value function hosts both the string representation and the actual value now.
You can jump to entities that are connected by a lookup by concatenating field names with a '.' as separator.
Default base entity for all operations is the primary entity. You can override this behaviour by passing an entity object as config parameter named `explicitTarget`, like so: `Value("regardingobjectid.firstname", { explicitTarget: PrimaryRecord() })`.

When working with option sets, you can instruct XTL to use a specific language's label by passing adding a key `optionSetLcid` to your configuration object and setting the desired lcid (1033 = english, 1031 = german, etc...) as value, as follows: ` Value("someOptionSet", { optionSetLcid: 1033 })`. It is recommended to pass a fixed lcid matching the language you're currently creating your template for.
When not passing the optionSetLcid setting, the option set integer value will be returned. If passing the optionSetLcid and no label is found for the specified language, the user language label is used. So if you just want to use your current user's label, you can pass an invalid value such as -1. 

Example:
``` JavaScript
Value ("regardingobjectid.firstname")
```

### RecordUrl
Returns the record urls of all entities or entity references that are passed as paramters.
For this to work, you have to set a secure json configuration with property organizationUrl set to your organization's url. When using the XTL editor, this will be done automatically.
By default, the URL will have the ref as link text as well. You can pass a configuration object with key `linkText` for defining a custom text to show link: `RecordUrl(PrimaryRecord(), { linkText: Value("name") })` (of course a normal string can be passed to linkText as well, the function call should just show the possibilities).
Similar to the linkText, you can also pass an appId (since v3.2.0) as configuration, which allows you to open the record directly in UCI for example (The configuration object would look something like `{ appId: "deadbeef-b85c-4fda-80da-d2f63baf9d7a" }`.


__Important Remark: When using RecordUrl, the organization url is automatically saved to the step secure config. When importing the step into another organization, you will initially have to open the editor in the new organnization, select each new step and save it once, so that the secure configs will be created. This is only necessary once, on the first import of a new step to an organization.__


Example:
``` JavaScript
RecordUrl ( Value ( "regardingobjectid") )
```

### Fetch
Returns a list of records, which were retrieved using supplied query.
First parameter is the fetch xml. You can optionally pass a list of further parameters, such as entity references, entities or option set values as references.
Inside the fetch you can then reference them by {0}, {1}, ... and so on.
The primary entity is always {0}, so additional references start at {1}.
 
Example:
``` JavaScript
Fetch ( "<fetch no-lock='true'><entity name='task'><attribute name='description' /><attribute name='subject' /><filter><condition attribute='regardingobjectid' operator='eq' value='{1}' /></filter></entity></fetch>", Array( Value ( "regardingobjectid" ) ) )
```

If there is the possibility of one of the references for the fetch being null, you need to handle those cases.
Passing a null value inside a fetch equal constraint for example, such as ```operator="eq" value="null"``` will lead to CRM exceptions.
You could now create your FetchXML expression using the Concat function or you'll have to wrap the fetch inside an if-clause that only executes the fetch, if your reference is not null. Otherwise the function will fail and replace the whole placeholder with an empty string.

Example for Concat:
``` JavaScript
Fetch ( Concat("<fetch no-lock='true'><entity name='task'><attribute name='description' /><attribute name='subject' /><filter>", If( Not ( IsNull ( Value ("regardingobjectid") ) ), "<condition attribute='regardingobjectid' operator='eq' value='{1}' />", "<condition attribute='regardingobjectid' operator='eq-null' />"), "</filter></entity></fetch>"), Array ( Value ("regardingobjectid") ))
```

Example for conditional fetch:
``` JavaScript
${{If(IsNull(Value("regardingobjectid" ) ), "No tasks", RecordTable ( Fetch ( "<fetch no-lock='true'><entity name='task'><attribute name='description' /><attribute name='subject' /><filter><condition attribute='regardingobjectid' operator='eq' value='{1}' /></filter></entity></fetch>", Value ( "regardingobjectid" ) ), "task", true, "subject", "description"))}}
```

Example(Please note that in recent releases (>= v1.0.31) the Text function is removed. You can replace it by the value function):
![email_table](https://user-images.githubusercontent.com/4287938/36945291-e5173fa0-1fab-11e8-86e6-3007eac254c9.gif)


### First
Receives a list and returns the first object found in it.

Example:
``` JavaScript
First ( Fetch ( "<fetch no-lock='true'><entity name='task'><attribute name='description' /><attribute name='subject' /><filter><condition attribute='regardingobjectid' operator='eq' value='{1}' /></filter></entity></fetch>", Value ( "regardingobjectid" ) ) )
```

Example of using it for getting the full name of the first to recipient on an email:
``` JavaScript
Value ( "partyid.fullname", { explicitTarget: First( Value("to") ) } )
``` 

### Last
Receives a list and returns the last object found in it.

Example:
``` JavaScript
Last ( Fetch ( "<fetch no-lock='true'><entity name='task'><attribute name='description' /><attribute name='subject' /><filter><condition attribute='regardingobjectid' operator='eq' value='{1}' /></filter></entity></fetch>", Value ( "regardingobjectid" ) ) )
```

### RecordTable
Returns a table of records with specified columns and record url if wanted.
First parameter is a list of records, for displaying inside the table. Consider retrieving them using the Fetch function.
Second is the name of the sub entity.
Third is an array expression containing the columns to retrieve, which will be added in the same order.
By default the columns are named like the display names of their respective CRM columns. If you want to override that, append a colon and the text you want to use, such as "subject:Overridden Subject Label".

If you wish to append the record url as last column, pass a configuration object as last parameter as follows: `{ addRecordUrl: true }`.
You can also set a custom label for the url in each column by using the `linkText` property.
There is also the possibility to configure table, header and row styling. Pass a style property inside your configuration parameter, as follows: `{ tableStyle: "border:1px solid green;", headerStyle: "border:1px solid orange;", dataStyle: "border:1px solid red;"}`.
Only pass one configuration object, it can contain information on addRecordUrl as well as on table styling.
You can also define different styles for even and uneven rows by using the configuration keys `unevenDataStyle` and `evenDataStyle`.

Below you can find an example, which executes on e-mail creation and searches for tasks associated to the mails regarding contact.
It will then print the task subject and description, including an url, into the mail.

In release v3.0.2 you will also be able to configure each column's style separately.
This will allow for defining a custom width to each column.
You can set it by passing the columns not as array of strings, but as array of objects.
They may containt the keys `name` for the column name, which you previously passed directly as string, `label` as custom label to show in the header, `style` for your style information for this column and `mergeStyle` for defining, whether your style information should be appended to the line (or header) style or not (defaults to true).

Passing all column names as array of strings is still possible.
 
Example:
``` JavaScript
RecordTable ( Fetch ( "<fetch no-lock='true'><entity name='task'><attribute name='description' /><attribute name='subject' /><filter><condition attribute='regardingobjectid' operator='eq' value='{1}' /></filter></entity></fetch>", Value ( "regardingobjectid" ) ), "task", Array( "subject:Overridden Subject Label", "description" ), { addRecordUrl: true })
```

Example of objects as columns:
``` JavaScript
RecordTable ( Fetch ( "<fetch no-lock='true'><entity name='task'><attribute name='description' /><attribute name='subject' /><filter><condition attribute='regardingobjectid' operator='eq' value='{1}' /></filter></entity></fetch>", Value ( "regardingobjectid" ) ), "task", [ { name: "subject", label: "Overridden Subject Label", style: "width:70%" }, {name: "description"} ], { addRecordUrl: true })
```

### PrimaryRecord
Returns the current primary entity as Entity object.
No parameters are needed.

Example:
``` JavaScript
PrimaryRecord ()
```

### Concat
Concatenates all parameters that were passed into one string.

Example:
``` JavaScript
Concat(Value("lastname"), ", ", Value("firstname"))
```
Above example could return something like 'Baggins, Frodo'.

### Substring
Takes the substring of your input starting from a given index. Length of substring can be passed optionally as well.

Example:
```
Substring(Value("firstname"), 1, 2 )
```
Above example returns 'ro' when input is 'Frodo'.

### Replace
Replaces text in an input string using your pattern and replacement regexes.
You can use the whole .NET regex syntax for your pattern and replacement regex.

Example:
``` JavaScript
Replace(Value("firstname"), "o", "a" )
```
Above example returns 'Frada' when input is 'Frodo'.

### Array
Creates an array from the parameters that you passed. Some functions do need arrays as input parameters, use this function for generating them.
You can enter static values such as string values, or even use XTL functions for passing in values.
Static strings are for example used for column names inside the RecordTable function, XTL functions as array values are used as fetch parameters in the Fetch function.

Example:
``` JavaScript
Array("those", "are", "test", "parameters")
```
Info: Arrays have a default textual representation, which is all of the array value text representations delimited by ", ". Above examples textual representation would therefore be "those, are, test, parameters". Null values are not removed, but show up as empty string.

### Join
Joins multiple strings together using the separator that you passed. You can pass dynamic expressions whose text representations will be used. The first parameter is the separator, the second one is the array of values to concatenate.
You can pass a boolean as third parameter, stating whether empty parameters should be left out when concatenating (defaults to false).

Example:
``` JavaScript
Join(NewLine(), Array ( Value("address1_line1"), Value("address1_line2")), true)
```

### NewLine
Inserts a new line. 

Example:
``` JavaScript
NewLine()
```
Info: This is needed when wanting to concatenate multiple texts using concat or join, since passing "\n" as .NET line break string is interpreted as plain string.

### DateTimeNow
Gets the current local time.

Example:
``` JavaScript
DateTimeNow()
```

### DateTimeUtcNow
Gets the current UTC time.

Example:
``` JavaScript
DateTimeUtcNow()
```

### DateToString
Prints the date that is passed as first parameter. A custom format can be appended using a configuration object with format key.

Example:
``` JavaScript
DateToString(DateTimeUtcNow(), { format: "yyyyMMdd" })
```

Refer to the .NET style for date formatting.

### ConvertDateTime
Converts a UTC DateTime (which is what you'll usually get from CRM) to a timezoned DateTime.
You can either pass a user reference as userId config property, or a fixed timezone as timeZoneId config property.
The timeZoneId fixed property is one of the default .NET timezone IDs.
An optional format config property can directly be set for defining the format of the timezoned DateTime.

Example by userId:
```JavaScript
ConvertDateTime(Value("createdon"), { userId: Value("ownerid") })
```

Example by fixed timezone:
```JavaScript
ConvertDateTime(Value("createdon"), { timeZoneId: "Eastern Standard Time" })
```
You can use the same format strings as in the DateToString function.

### Format
Formats values using .NET string.Format. This way you can format numeric, Money and DateTime values.

Example (Decimal, Double and Money will work the same):
``` JavaScript
Format( Value("revenue"),  { format: "{0:0,0.0}" } ) // Will print 123,456,789.2 for revenue equal 123456789.2
```

Example Int:
``` JavaScript
Format( Value("index"),  { format: "{0:00000}" } ) // Will print 00001 for index equal 1
```

Refer to the .NET style for date formatting.

## Sample
Consider the following e-mail template content:
```
Hello ${{Value("regardingobjectid.parentcustomerid.ownerid.firstname")}},

a new case was associated with your account ${{Value("regardingobjectid.parentcustomerid.name")}}, you can open it using the following URL:
${{RecordUrl(Value("regardingobjectid"))}} 
```

When creating an e-mail from above template or even just an outgoing e-mail with the template content as text it will resolve to:
```
Hello Frodo,

a new case was associated with your account TheShire Limited, you can open it using the following URL:
https://imagine-creative-url.local
```

## Template Editor
The solution that you can find inside the releases section also contains a live editor that can be used for creating, testing and serializing configurations for XTL. In addition to that, existing XTL plugin execution steps can be loaded, tested and updated.
You just need to import the xtl solution, open it and select the configuration page.
Inside this page, you can select which entity you want your template to be executed on, which record it should use for previews and set execution criteria and your template. When selecting to load an existing plugin execution step, this information is parsed from the existing step.
After clicking "Preview", the template will be processed and you'll be presented the result and the interpreter trace for debugging purposes.

The example below targets an E-mail, which has an opportunity as regarding object:
![xtl_template_editor](https://user-images.githubusercontent.com/4287938/37870319-d9e516dc-2fca-11e8-9e78-6c52d842d535.gif)

Source snippet of the template being used:
```
Hi ${{Value("regardingobjectid.parentaccountid.ownerid")}},

${{Value("regardingobjectid.createdby")}} created a new opportunity for your account ${{Value("regardingobjectid.parentaccountid.name")}}.

You can open it using the following link: ${{RecordUrl(Value("regardingobjectid"))}}.

Below products have been configured:
${{RecordTable ( Fetch ( "<fetch no-lock='true' ><entity name='opportunityproduct' ><attribute name='productdescription' /><attribute name='priceperunit' /><attribute name='quantity' /><attribute name='extendedamount' /><filter type='and' ><condition attribute='opportunityid' operator='eq' value='{1}' /></filter></entity></fetch>", Value ( "regardingobjectid" ) ), "opportunityproduct", true, "productdescription", "quantity", "priceperunit", "extendedamount")}}

In case any questions are left, please give ${{Value("regardingobjectid.createdby")}} a call: ${{Value("regardingobjectid.createdby.homephone")}}.

This E-Mail was automatically generated by the system.
```

## License
Licensed using the MIT license, enjoy!

## Credits
I learnt writing the interpreter using the [Let's Build a Compiler Tutorial](https://compilers.iecc.com/crenshaw/) by Jack Crenshaw.
Although being quite old, the technics are still the same and the advice is invaluable, it's a great resource for learning.
