using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace RoslynTests;

internal static class RoslynTestExtensions
{
    private const string DefaultProjectName = "ProjectUnderTest.csproj";
    private const string DefaultDocumentName = "SourceUnderTest.cs";

    /// <summary>
    /// These are the common references to be added to the test solution. They are sourced from the actual references loaded into this project.
    /// </summary>
    private static readonly MetadataReference[] CommonReferences =
    {
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
    };

    /// <summary>
    /// Creates a temporary workspace using the <see cref="DefaultProjectName"/> as the project name
    /// and <see cref="DefaultDocumentName"/> containing the <paramref name="source"/> as a single existing document.
    /// <see cref="CommonReferences"/> are the only external references added to the project.
    /// </summary>
    private static AdhocWorkspace CreateWorkspace(this SourceText source)
    {
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId, DefaultDocumentName);

        var sourceTextLoader = TextLoader.From(TextAndVersion.Create(source, VersionStamp.Create()));
        var document = DocumentInfo.Create(documentId, DefaultDocumentName)
                                   .WithTextLoader(sourceTextLoader);
        var project = ProjectInfo.Create(id: projectId,
                                     version: VersionStamp.Create(),
                                     name: DefaultProjectName,
                                     assemblyName: DefaultProjectName,
                                     language: LanguageNames.CSharp)
                                 .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var workspace = new AdhocWorkspace();
        var updatedSolution = workspace
                              .CurrentSolution
                              .AddProject(project)
                              .AddMetadataReferences(projectId, CommonReferences)
                              .AddDocument(document);

        workspace.TryApplyChanges(updatedSolution);

        return workspace;
    }

    /// <summary>
    /// Gets the <see cref="DefaultProjectName"/> project
    /// </summary>
    private static Project GetProjectUnderTest(this AdhocWorkspace workspace)
    {
        return workspace.CurrentSolution.Projects.First(x => x.Name == DefaultProjectName);
    }

    /// <summary>
    /// Gets the <see cref="DefaultDocumentName"/> document
    /// </summary>
    private static Document GetDocumentUnderTest(this Project project)
    {
        return project.Documents.First(x => x.Name == DefaultDocumentName);
    }

    /// <summary>
    /// Creates a compilation where an instanced of <paramref name="analyzer"/> is included
    /// </summary>
    private static async Task<CompilationWithAnalyzers> GetCompilationWithAnalyzerAsync(this Project project, DiagnosticAnalyzer analyzer)
    {
        var compilation = await project.GetCompilationAsync();

        return (compilation ?? throw new InvalidOperationException("Could not get compilation."))
            .WithAnalyzers(ImmutableArray.Create(analyzer));
    }

    /// <summary>
    /// Returns the diagnostic instances of the <typeparamref name="TAnalyzer"/> that were discovered in the <paramref name="source"/>.
    /// Ignores all other types of diagnostics returned.
    /// Ignores all locations other than <paramref name="source"/>.
    /// </summary>
    public static async Task<ImmutableArray<Diagnostic>> GetDiagnostics<TAnalyzer>(this SourceText source)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var workspace = source.CreateWorkspace();
        var document = workspace.GetProjectUnderTest().GetDocumentUnderTest();
        var analyzer = new TAnalyzer();
        var diagnosticDescriptor = analyzer.SupportedDiagnostics.Single();
        var compilation = await workspace.GetProjectUnderTest().GetCompilationWithAnalyzerAsync(analyzer);
        var allDiagnostics = await compilation.GetAllDiagnosticsAsync();

        return allDiagnostics.Where(x => x.Id == diagnosticDescriptor.Id &&
                                         x.Location.SourceTree?.FilePath == document.Name)
                             .ToImmutableArray();
    }

    /// <summary>
    /// Returns a source code after all occurrences of <typeparamref name="TAnalyzer"/> reported diagnostics were fixed by <typeparamref name="TCodeFixProvider"/>.
    /// </summary>
    /// <remarks>
    /// The functionality is fairly limited.
    /// <li> The <typeparamref name="TAnalyzer"/> should only support a single fixable diagnostic. </li>
    /// <li> A diagnostic should be expected and reported for the <paramref name="source"/> document. </li>
    /// <li> The <typeparamref name="TCodeFixProvider"/> is expected to fix a single reported diagnostic in a single iteration. </li>
    /// <li> Syntax validity is not verified after the fix is applied. </li>
    /// <li> Etc. </li>
    /// </remarks>
    internal static async Task<SourceText> ApplyCodeFixes<TAnalyzer, TCodeFixProvider>(this SourceText source)
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFixProvider : CodeFixProvider, new()
    {
        var workspace = source.CreateWorkspace();
        var document = workspace.GetProjectUnderTest().GetDocumentUnderTest();
        var analyzer = new TAnalyzer();
        var diagnosticDescriptor = analyzer.SupportedDiagnostics.Single();
        var compilation = await workspace.GetProjectUnderTest().GetCompilationWithAnalyzerAsync(analyzer);
        var allDiagnostics = await compilation.GetAllDiagnosticsAsync();
        var singleDiagnostic = allDiagnostics.Single(x => x.Id == diagnosticDescriptor.Id &&
                                                          x.Location.SourceTree?.FilePath == document.Name);

        await workspace.ApplyCodeFix<TCodeFixProvider>(document, singleDiagnostic);

        return await workspace.GetProjectUnderTest().GetDocumentUnderTest().GetTextAsync();
    }

    /// <summary>
    /// Applies the code fix for a <paramref name="singleDiagnostic"/> in the <paramref name="document"/>
    /// and updates the <paramref name="workspace"/> with the fixed source.
    /// The code fix is applied by using an instance of <typeparamref name="TCodeFixProvider"/>
    /// </summary>
    private static async Task ApplyCodeFix<TCodeFixProvider>(this AdhocWorkspace workspace, Document document, Diagnostic singleDiagnostic)
        where TCodeFixProvider : CodeFixProvider, new()
    {
        var codeFixProvider = new TCodeFixProvider();
        List<CodeAction> actions = new();
        var context = new CodeFixContext(document,
            singleDiagnostic,
            (a, _) => actions.Add(a),
            CancellationToken.None);
        await codeFixProvider.RegisterCodeFixesAsync(context);
        foreach (var codeAction in actions)
        {
            var operations = await codeAction.GetOperationsAsync(CancellationToken.None);
            if (operations.IsDefaultOrEmpty)
            {
                continue;
            }

            var changedSolution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
            workspace.TryApplyChanges(changedSolution);
        }
    }

    /// <summary>
    /// Asserts that the <paramref name="actual"/> source is equal to <paramref name="expected"/> source.
    /// </summary>
    internal static void ShouldBeEqualTo(this SourceText actual, SourceText expected)
    {
        actual.GetChecksum().Should().Equal(expected.GetChecksum());
        actual.ToString().Should().Be(expected.ToString());
    }

    /// <summary>
    /// Asserts that the <paramref name="diagnostics"/> contain at least one diagnostic with <paramref name="diagnosticId"/>.
    /// </summary>
    internal static void ShouldContainDiagnosticWithId(this ImmutableArray<Diagnostic> diagnostics, string diagnosticId)
    {
        diagnostics.Should().NotBeNull().And.HaveCountGreaterOrEqualTo(1);
        diagnostics.Should().Contain(diagnostic => diagnostic.Id == diagnosticId);
    }
}
