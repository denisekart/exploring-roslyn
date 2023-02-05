using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace EmptyLinesAnalyzerAndCodeFix;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
[SuppressMessage(category: "MicrosoftCodeAnalysisReleaseTracking",
    checkId: "RS2008:Enable analyzer release tracking",
    Justification = "I'm only doing this for fun")]
public class EmptyLinesAnalyzer : DiagnosticAnalyzer
{
    internal const int NumberOfEmptyLinesToTriggerDiagnostic = 2;
    internal const string DiagnosticId = "DM0001";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Multiple redundant empty lines",
        messageFormat: "Remove multiple sequential empty lines",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Multiple sequential empty lines should be removed.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
    }

    private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
    {
        if (context.IsGeneratedCode)
            return;

        Recurse(context, context.Tree.GetRoot());
    }

    private void Recurse(SyntaxTreeAnalysisContext context, SyntaxNode node)
    {
        foreach (var nodeOrToken in node.ChildNodesAndTokens())
        {
            if (nodeOrToken.IsNode)
                Recurse(context, nodeOrToken.AsNode());
            else if (nodeOrToken.IsToken)
                AnalyzeToken(context, nodeOrToken.AsToken());
        }
    }

    private void AnalyzeToken(SyntaxTreeAnalysisContext context, SyntaxToken token)
    {
        if (!token.HasLeadingTrivia)
            return;
        if (token.LeadingTrivia.Count < 2)
            return;

        var spanStart = -1;
        var spanEnd = -1;

        for (var i = 0; i < token.LeadingTrivia.Count; i++)
        {
            var isNewLine = token.LeadingTrivia[i].IsKind(SyntaxKind.EndOfLineTrivia);
            var isWhitespace = token.LeadingTrivia[i].IsKind(SyntaxKind.WhitespaceTrivia);
            var isEmptyToken = isNewLine || isWhitespace;

            // found empty token - start new candidate span
            if (isEmptyToken && spanStart == -1)
            {
                spanStart = token.LeadingTrivia[i].SpanStart;
                spanEnd = token.LeadingTrivia[i].Span.End;
            }
            // found another empty token - expand candidate span
            else if (isNewLine)
            {
                spanEnd = token.LeadingTrivia[i].Span.End;
            }
            // found a non-empty token - end candidate span and check for diagnostics
            else if (!isWhitespace)
            {
                if (spanStart != -1)
                {
                    ReportDiagnosticIfNeeded(new TextSpan(spanStart, spanEnd - spanStart), token, context);
                }

                // restart the process at this point
                spanStart = spanEnd = -1;
            }
        }

        // if there are any leftover diagnostic candidates - report them
        if (spanStart != -1)
        {
            ReportDiagnosticIfNeeded(new TextSpan(spanStart, spanEnd - spanStart), token, context);
        }
    }

    private static void ReportDiagnosticIfNeeded(TextSpan span, SyntaxToken token, SyntaxTreeAnalysisContext context)
    {
        var lines = token.SyntaxTree!.GetLineSpan(span);
        // attempt to isolate the needed change span to only empty lines
        var firstEmptyLine = lines.StartLinePosition.Character == 0
            ? lines.StartLinePosition.Line
            : lines.StartLinePosition.Line + 1;
        var lastEmptyLine = lines.EndLinePosition.Character == 0
            ? lines.EndLinePosition.Line
            : lines.EndLinePosition.Line - 1;

        if (lastEmptyLine - firstEmptyLine < NumberOfEmptyLinesToTriggerDiagnostic)
        {
            return;
        }

        // in the case when the start line character at the end of a comment/token
        // move to the next token which is in the next line
        var startOffset = lines.StartLinePosition.Character == 0
            ? 0
            : 1;
        var reportSpan = new TextSpan(span.Start + startOffset, span.Length - lines.EndLinePosition.Character - startOffset);
        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            Location.Create(
                context.Tree,
                reportSpan
            ),
            // the additional location is useful when determining the syntax token to fix in the code fix.
            // Report it as an additional location.
            additionalLocations: new[] { token.GetLocation() }));
    }
}
