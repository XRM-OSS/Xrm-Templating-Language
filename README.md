# XTL - Xrm-Templating-Language
A domain specific language for Dynamics CRM allowing for easy text template processing

## Purpose
XTL is a domain specific language created for easing text processing inside Dynamics CRM.
It is an interpreted programming language, with easy syntax for allowing everyone to use it.

The parsing and interpreting is done using a custom recursive descent parser implemented in C#.
It is embedded inside a plugin and does not need any external references, so that execution works in CRM online and on-premises environments.

## Benefits
When dealing with the default e-mail editor of Dynamics CRM, the borders of what's possible are reached fast.
XTL aims to integrate flawlessly into Dynamics CRM, to enhance the text processing capabilities. It is not limited to any specific CRM entity.
Using XTL provides the following benefits:

- Using of the primary entity's related entity values, no matter how "far" they are away (i.e. regardingobject.parentaccountid.ownerid.fullname for using the full name of the owner of the company that the contact receiving an email belongs to).
- Using of child entity values, where the primary record does not hold the lookup (i.e. all tasks associated to an account)
- Conditional Branching (If-Then-Else constructs)
- Generating Record URLs for all records reachable using above expressions
- Easy to learn syntax

## Sample
Consider the following e-mail template content:
```
Hello ${Text("regardingobjectid.customerid.ownerid.firstname")},

a new case was associated with your account ${Text("regardingobjectid.customerid.name)}, you can open it using the following URL:
${RecordUrl(Value("regardingobjectid"))}
```

When creating an e-mail from above template or even just an outgoing e-mail with the template content as text it will resolve to:
```
Hello Frodo,

a new case was associated with your account TheShire Limited, you can open it using the following URL:
[Ring Delivery Stuck](https://imagine-creative-url.local)
```

## License
Licensed using the MIT license, enjoy!

## Credits
I learnt writing the interpreter using the [Let's Build a Compiler Tutorial](https://compilers.iecc.com/crenshaw/) by Jack Crenshaw.
Although being quite old, the technics are still the same and the advice is invaluable, it's a great resource for learning.
