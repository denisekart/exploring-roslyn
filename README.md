# Exploring Roslyn: Demos

This repository contains the demo code from the article series [Exploring Roslyn](https://denace.dev/series/exploring-roslyn).

For your convenience, here are the articles:

- [Exploring Roslyn .NET Compiler Platform SDK](https://denace.dev/exploring-roslyn-net-compiler-platform-sdk)
- [Getting Started With Roslyn Analyzers](https://denace.dev/getting-started-with-roslyn-analyzers)
- [Fixing Mistakes With Roslyn Code Fixes](https://denace.dev/fixing-mistakes-with-roslyn-code-fixes)
- [Testing Roslyn Analyzers and Code Fixes](https://denace.dev/testing-roslyn-analyzers-and-code-fixes)

## Getting started

To get started with this solution, 

- Make sure you have `net7.0` installed. You can download the dotnet SDK [here](https://dotnet.microsoft.com/en-us/download/dotnet/7.0). 
- Navigate to the root of the repository and run `dotnet build`.
- Run tests using `dotnet test` command from the repository root.
- Open the `ExploringRoslyn.sln` solution in your favourite IDE. **
- Explore the demos.

** [Visual Studio](https://visualstudio.microsoft.com/vs/community/), [Rider](https://www.jetbrains.com/rider/download/), and [VSCode](https://code.visualstudio.com/download) should work without issues.

## What is included

The solution consists of several projects intended to showcase Roslyn.

- `DemoConsoleApp` - a simple console application intended to be used as a playground and explore the implemented Roslyn components
- `EmptyLinesAnalyzerAndCodeFix` - Roslyn analyzer project containing the analyzer and a code fix for *multiple empty lines* diagnostic
- `RoslynTests` - NUnit test project intended for testing Roslyn components developed in this solution

## Contributing

Feel free to create an issue or [reach out](mailto:denis.ekart@gmail.com) if you feel like you could contribute to this project in any way.

`#dotnet` `#roslyn` `#csharp` `#codingisfun`

✌