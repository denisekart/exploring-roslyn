using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmptyLinesAnalyzerAndCodeFix;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;

namespace EmptyLinesAnalyzerAndFix;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EmptyLinesCodeFix)), Shared]
public class EmptyLinesCodeFix : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(EmptyLinesAnalyzer.DiagnosticId);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        const string action = "Remove redundant empty lines";

        var diagnostic = context.Diagnostics.First();
        context.RegisterCodeFix(
            CodeAction.Create(
                title: action,
                createChangedDocument: c => RemoveEmptyLines(context.Document, diagnostic, c),
                equivalenceKey: action),
            diagnostic);

        return Task.CompletedTask;
    }

    private static async Task<Document> RemoveEmptyLines(Document document,
        Diagnostic diagnostic,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken);

        // the document does not have a syntax tree - nothing to do
        if (root is null)
            return document;

        // find the token at the additional location we reported in the analyzer
        var token = root.FindToken(diagnostic.AdditionalLocations[0].SourceSpan.Start);
        var updatedToken = token.WithLeadingTrivia(UpdateTrivia(token.LeadingTrivia, diagnostic.Location));
        var newRoot = root.ReplaceToken(token, updatedToken);

        return document.WithSyntaxRoot(newRoot);
    }

    private static SyntaxTriviaList UpdateTrivia(SyntaxTriviaList trivia, Location conflictingLocation)
    {
        // this span covers the entire range of whitespace between significant syntax
        var conflictingSpan = conflictingLocation.SourceSpan;
        var updatedTrivia = new List<SyntaxTrivia>();
        var firstOverlap = true;

        for (var i = 0; i < trivia.Count; i++)
        {
            var isOverlapping = trivia[i].Span.OverlapsWith(conflictingSpan);
            if (!isOverlapping)
            {
                updatedTrivia.Add(trivia[i]);
            }
            else if (firstOverlap)
            {
                // we should still keep the most amount of empty lines that do not trigger the diagnostic
                firstOverlap = false;
                updatedTrivia.AddRange(
                    Enumerable.Repeat(SyntaxFactory.CarriageReturnLineFeed,
                        // note, the first empty line was kept from the original trivia, this is why "-1"
                        // this allows us to keep the proper indentation
                        EmptyLinesAnalyzer.NumberOfEmptyLinesToTriggerDiagnostic - 1));
            }
        }

        return new SyntaxTriviaList(updatedTrivia);
    }
}
