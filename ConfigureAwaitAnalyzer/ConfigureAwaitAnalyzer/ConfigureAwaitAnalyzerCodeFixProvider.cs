using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace ConfigureAwaitAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConfigureAwaitAnalyzerCodeFixProvider)), Shared]
    public class ConfigureAwaitAnalyzerCodeFixProvider : CodeFixProvider
    {
        private static readonly LocalizableString AddConfigureAwaitTitleFormat = new LocalizableResourceString(nameof(Resources.AddConfigureAwaitTitleFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly string AddConfigureAwaitFalseTitle = string.Format(AddConfigureAwaitTitleFormat.ToString(), "false");
        private static readonly string AddConfigureAwaitTrueTitle = string.Format(AddConfigureAwaitTitleFormat.ToString(), "true");

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(ConfigureAwaitAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var awaitExpr = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<AwaitExpressionSyntax>().First();

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: AddConfigureAwaitFalseTitle,
                    createChangedDocument: c => AddConfigureAwaitFalseAsync(context.Document, awaitExpr, c),
                    equivalenceKey: AddConfigureAwaitFalseTitle),
                diagnostic);

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: AddConfigureAwaitTrueTitle,
                    createChangedDocument: c => AddConfigureAwaitTrueAsync(context.Document, awaitExpr, c),
                    equivalenceKey: AddConfigureAwaitTrueTitle),
                diagnostic);
        }

        private async Task<Document> AddConfigureAwaitFalseAsync(Document document, AwaitExpressionSyntax awaitExpr, CancellationToken cancellationToken)
        {
            // Add an annotation to format the new node.
            var formatted = AddConfigureAwait(awaitExpr).WithAdditionalAnnotations(Formatter.Annotation);

            // Replace the old node with the new node.
            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = oldRoot.ReplaceNode(awaitExpr, formatted);

            // Return document with transformed tree.
            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> AddConfigureAwaitTrueAsync(Document document, AwaitExpressionSyntax awaitExpr, CancellationToken cancellationToken)
        {
            // Add an annotation to format the new node.
            var formatted = AddConfigureAwait(awaitExpr, true).WithAdditionalAnnotations(Formatter.Annotation);

            // Replace the old node with the new node.
            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = oldRoot.ReplaceNode(awaitExpr, formatted);

            // Return document with transformed tree.
            return document.WithSyntaxRoot(newRoot);
        }

        private AwaitExpressionSyntax AddConfigureAwait(AwaitExpressionSyntax awaitExpr, bool passTrueArg = false)
        {
            var firstToken = awaitExpr.GetFirstToken();
            var leadingTrivia = firstToken.LeadingTrivia;

            var kind = passTrueArg ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression;

            var configureAwait = SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        awaitExpr.Expression,
                        SyntaxFactory.IdentifierName("ConfigureAwait")
                    ),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.Token(SyntaxKind.OpenParenToken),
                        SyntaxFactory.SeparatedList(new[] { SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(kind)) }),
                        SyntaxFactory.Token(SyntaxKind.CloseParenToken
                        )
                    )
                );

            return SyntaxFactory.AwaitExpression(configureAwait).WithLeadingTrivia(leadingTrivia);
        }
    }
}
