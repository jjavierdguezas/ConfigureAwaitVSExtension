# ConfigureAwaitVSExtension

A Visual Studio extension that warn to configure an awaiter in await calls

⚠️ this branch is for VS2019 only ⚠️

## Motivation

We always forget the `ConfigureAwait(...)`, so this extension warns us about this and fixes it! 😎
I made this project just for fun. I'm sure there are some edge cases that I didn't take into account
Meanwhile, enjoy it! 😉

## About the code

I used the Visual Studio Extensibility Template _Analyzer with Code Fix (.NET Standard)_.
In theory, this extension can be deployed as either a NuGet package or a VSIX extension.
It was tested using Microsoft Visual Studio 2017 Version 15.9.11

### The `ConfigureAwaitAnalyzer`

It only runs if there are no compilation errors.
It analyzes the `AwaitExpressionSyntax` nodes and check if their `string` representation ends with the text `"ConfigureAwait([true|false])"`
That's it, just that

### The `ConfigureAwaitAnalyzerCodeFixProvider`

It modifies the `AwaitExpressionSyntax`'`ExpressionNode` in order to add the `ConfigureAwait([true|false])` expression nodes:

![asd](https://i.ibb.co/W2TzLsh/Await-Expression-Tree.png)

this way we make the fix.
There are two code fixes: 'Add `ConfigureAwait(false)`' and 'Add `ConfigureAwait(true)`'

## Todos

- Check edge cases like:

  ```csharp
  var task = MethodAsync().ConfigureAwait(false);
  var result = await task;  // <- this generates an unnecesary warning
                ~~~~~~~~~~~
  ```

- Maybe do something smarter than just check the suffix on the `string` representation
- Allow user settings to:
  - activate/deactivate the extension
  - change if the extension should report Warnings or Errors

## Useful links for development:

- [Creating a .NET Standard Roslyn Analyzer in Visual Studio 2017](https://andrewlock.net/creating-a-roslyn-analyzer-in-visual-studio-2017/)
- [Starting to Develop Visual Studio Extensions](https://docs.microsoft.com/en-us/visualstudio/extensibility/starting-to-develop-visual-studio-extensions?view=vs-2019)
- [How To Write a C# Analyzer and Code Fix](https://github.com/dotnet/roslyn/wiki/How-To-Write-a-C%23-Analyzer-and-Code-Fix)
- [How to upgrade extensions to support Visual Studio 2019](https://devblogs.microsoft.com/visualstudio/how-to-upgrade-extensions-to-support-visual-studio-2019/)

---
Coded by JJ - 2019

_Thanks to [@carlosbonillabirchman](https://github.com/carlosbonillabirchman)_ for talking me about this idea

Licensed under the [MIT license](LICENSE)
