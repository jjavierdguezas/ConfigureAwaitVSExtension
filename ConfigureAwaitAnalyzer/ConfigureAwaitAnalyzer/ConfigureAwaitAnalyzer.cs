using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ConfigureAwaitAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ConfigureAwaitAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ConfigureAwaitAnalyzer";
        private const string Category = "Usage";
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.Compilation.GetDiagnostics().Any(issue => issue.Severity == DiagnosticSeverity.Error || issue.IsWarningAsError))
                return;

            var node = (InvocationExpressionSyntax)context.Node;
            var exprStr = node.ToString();

            if (node.Parent is AwaitExpressionSyntax && !exprStr.EndsWith(".ConfigureAwait(true)") && !exprStr.EndsWith(".ConfigureAwait(false)"))
            {
                //var parent = (AwaitExpressionSyntax)node.Parent;
                //var awaitExpressionInfo = context.SemanticModel.GetAwaitExpressionInfo(parent);

                context.ReportDiagnostic(Diagnostic.Create(Rule, node.GetLocation(), node.ToString()));
            }
        }
    }
}
