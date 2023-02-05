# Exploring Roslyn: Demos

This repository contains the demo code from the article series [Exploring Roslyn](https://denace.dev/series/exploring-roslyn).

## Getting started

To get started with this solution, 

- make sure you have `net7.0` installed. You can download the dotnet SDK [here](https://dotnet.microsoft.com/en-us/download/dotnet/7.0). 
- navigate to the root of the repository and run `dotnet build`.
- open the `ExploringRoslyn.sln` solution in your favourite IDE **
- explore the demos

** [Visual Studio](https://visualstudio.microsoft.com/vs/community/), [Rider](https://www.jetbrains.com/rider/download/), and [VSCode](https://code.visualstudio.com/download) should work without issues.

## What is included

The solution consists of several projects intended to showcase Roslyn.

- `DemoConsoleApp` - a simple console application intended to be used as a playground and explore the implemented Roslyn components
- `EmptyLinesAnalyzerAndCodeFix` - Roslyn analyzer project containing the analyzer and a code fix for *multiple empty lines* diagnostic

## Contributing

Feel free to create an issue or [reach out](mailto:denis.ekart@gmail.com) if you feel like you could contribute to this project in any way.

✌