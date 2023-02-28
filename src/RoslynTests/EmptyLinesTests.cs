using EmptyLinesAnalyzerAndCodeFix;
using EmptyLinesAnalyzerAndFix;
using Microsoft.CodeAnalysis.Text;

namespace RoslynTests;

using Verify = Microsoft.CodeAnalysis.CSharp.Testing.NUnit.CodeFixVerifier<EmptyLinesAnalyzer, EmptyLinesCodeFix>;

public class EmptyLinesTests
{
    [Test]
    public async Task EmptyLinesAnalyzer_ShouldReportDiagnostic_WhenMultipleEmptyLinesExist()
    {
        // Arrange
        // language=csharp
        var source = SourceText.From("""
            namespace DemoConsoleApp;
            
            
            /// <summary> </summary>            
            public class EmptyLines
            {
            }
            """);

        // Act
        var actual = await source.GetDiagnostics<EmptyLinesAnalyzer>();

        // Assert
        actual.ShouldContainDiagnosticWithId("DM0001");
    }

    [Test]
    public async Task EmptyLinesCodeFix_ShouldApplyFix_WhenMultipleEmptyLinesExist()
    {
        // Arrange
        // language=csharp
        var source = SourceText.From("""
        namespace DemoConsoleApp;
        
        
        public class EmptyLines
        { }
        """);
        // language=csharp
        var expected = SourceText.From("""
        namespace DemoConsoleApp;
        
        public class EmptyLines
        { }
        """);

        // Act
        var actual = await source.ApplyCodeFixes<EmptyLinesAnalyzer, EmptyLinesCodeFix>();

        // Assert
        actual.ShouldBeEqualTo(expected);
    }

    [Test]
    public async Task EmptyLinesCodeFix_ShouldApplyFix_WhenMultipleEmptyLinesExist_UsingRoslynLibrary()
    {
        // Arrange
        // language=csharp
        var source = """
            namespace DemoConsoleApp;
            
            
            public class EmptyLines
            { }
            """;
        // language=csharp
        var expected = """
            namespace DemoConsoleApp;
            
            public class EmptyLines
            { }
            """;
        var diagnostic = Verify.Diagnostic()
                               .WithSpan(startLine: 2, startColumn: 1, endLine: 4, endColumn: 1)
                               .WithSpan(startLine: 4, startColumn: 1, endLine: 4, endColumn: 7);

        // Assert
        await Verify.VerifyCodeFixAsync(source: source, expected: diagnostic, fixedSource: expected);
    }

    [Test]
    public async Task EmptyLinesCodeFix_ShouldApplyFix_WhenMultipleEmptyLinesExist_UsingRoslynLibraryCustomSyntax()
    {
        // Arrange
        // language=csharp
        var source = """
            namespace DemoConsoleApp;
            {|DM0001:
            
            |}public class EmptyLines
            { }
            """;
        // language=csharp
        var expected = """
            namespace DemoConsoleApp;
            
            public class EmptyLines
            { }
            """;

        // Assert
        await Verify.VerifyCodeFixAsync(source, expected);
    }

    [Test]
    public async Task EmptyLinesAnalyzer_ShouldReportDiagnostic_WhenMultipleEmptyLinesExist_UsingRoslynLibraryCustomSyntax()
    {
        // Arrange
        // language=csharp
        var source = """
            namespace DemoConsoleApp;
            [|
            
            |]public class EmptyLines
            { }
            """;

        // Assert
        await Verify.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task EmptyLinesAnalyzer_ShouldReportDiagnosticAtPosition_WhenMultipleEmptyLinesExist_UsingRoslynLibraryCustomSyntax()
    {
        // Arrange
        // language=csharp
        var source = """
            namespace DemoConsoleApp;
            $$
            
            public class EmptyLines
            { }
            """;

        // Assert
        await Verify.VerifyAnalyzerAsync(source);
    }
    
    [Test]
    public async Task EmptyLinesAnalyzer_ShouldReportMultipleDiagnostics_WhenMultipleEmptyLinesDiagnosticsExistInSingleDocument()
    {
        // Arrange
        // language=csharp
        var source = """
            namespace DemoConsoleApp;
            $$
            
            /// <summary> structured comment </summary>
            $$
            
            public class EmptyLines
            { }
            """;

        // Assert
        await Verify.VerifyAnalyzerAsync(source);
    }
    
    [Test]
    public async Task EmptyLinesCodeFix_ShouldApplyMultipleFixes_WhenMultipleEmptyLinesDiagnosticsExistInSingleDocument()
    {
        // Arrange
        // language=csharp
        var source = """
            namespace DemoConsoleApp;
            {|DM0001:
            
            |}public class EmptyLines
            {
            {|DM0001:    
                
            |}}
            """;
        // language=csharp
        var expected = """
            namespace DemoConsoleApp;
            
            public class EmptyLines
            {
            
            }
            """;

        // Assert
        await Verify.VerifyCodeFixAsync(source, expected);
    }
}
