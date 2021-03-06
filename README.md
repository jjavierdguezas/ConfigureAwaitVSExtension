# ConfigureAwaitVSExtension

A Visual Studio extension that warn to configure an awaiter in await calls

⚠️ this branch is for VS2017 only ⚠️ if you want to try in VS2019 go to [feature/vs-2019 branch](https://github.com/jjavierdguezas/ConfigureAwaitVSExtension/tree/feature/vs-2019)

## Motivation

We always forget the `ConfigureAwait(...)`, so this extension warns us about this and fixes it! 😎
I made this project just for fun. I'm sure there are some edge cases that I didn't take into account
Meanwhile, enjoy it! 😉

![extension gif](https://i.ibb.co/gSyXBgr/Configure-Await-Extension.gif)

## About the code

I used the Visual Studio Extensibility Template _Analyzer with Code Fix (.NET Standard)_.
In theory, this extension can be deployed as either a NuGet package or a VSIX extension.
It was tested using Microsoft Visual Studio 2017 Version 15.9.11

### The `ConfigureAwaitAnalyzer`

It only runs if there are no compilation errors.
It analyzes the `AwaitExpressionSyntax` nodes and check if their expression (method call or variable) is of type `ConfiguredTaskAwaitable`.
That's it.

### The `ConfigureAwaitAnalyzerCodeFixProvider`

It modifies the `AwaitExpressionSyntax`'s `ExpressionNode` in order to add the `.ConfigureAwait([true|false])` expression nodes:

![await expresion tree img](https://i.ibb.co/W2TzLsh/Await-Expression-Tree.png)

this way we make the fix.
There are two code fixes: 'Add `ConfigureAwait(false)`' and 'Add `ConfigureAwait(true)`'

## Todos

- Check edge cases like:
  - ~~Await an already configured task (invoked as variable) :~~
      ```csharp
      var task = MethodAsync().ConfigureAwait(false);
      var result = await task;  // <- this generates an unnecesary warning
                   ~~~~~~~~~~~
      ```
  - ~~Await calls in `Controller`'s `Action` in `ASP.NET Core` apps~~
      ```csharp
      class HomeController : Controller 
      {
            public async Task<IActionAsync> GetData()
            {
                return Ok(await GetDataAsync()) // <- this generates an unnecesary warning
                          ~~~~~~~~~~~~~~~~~~~~
            }
      }
      ```

- ~~Maybe do something smarter than just check the suffix on the `string` representation~~
- Allow user settings to:
  - activate/deactivate the extension
  - change if the extension should report Warnings or Errors

## Useful links for development

- [Creating a .NET Standard Roslyn Analyzer in Visual Studio 2017](https://andrewlock.net/creating-a-roslyn-analyzer-in-visual-studio-2017/)
- [Starting to Develop Visual Studio Extensions](https://docs.microsoft.com/en-us/visualstudio/extensibility/starting-to-develop-visual-studio-extensions?view=vs-2019)
- [How To Write a C# Analyzer and Code Fix](https://github.com/dotnet/roslyn/wiki/How-To-Write-a-C%23-Analyzer-and-Code-Fix)
- [Working with types in a Roslyn analyzer](https://www.meziantou.net/2019/02/04/working-with-types-in-a-roslyn-analyzer)

## Releases

If you want just the `*.vsix` file download it from [the releases](https://github.com/jjavierdguezas/ConfigureAwaitVSExtension/releases)

---
Coded by JJ - 2019

_Thanks to [@carlosbonillabirchman](https://github.com/carlosbonillabirchman)_ for telling me about this idea

Licensed under the [MIT license](LICENSE)
