using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
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

        private static readonly string AwaiterConfiguredType = typeof(ConfiguredTaskAwaitable).FullName;

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.AwaitExpression);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.Compilation.GetDiagnostics().Any(issue => issue.Severity == DiagnosticSeverity.Error || issue.IsWarningAsError))
                return;

            var node = (AwaitExpressionSyntax)context.Node;

            var child = node.ChildNodes().First();

            var typedNode = child is InvocationExpressionSyntax invocationExpr ? invocationExpr : child;

            var info = context.SemanticModel.GetSymbolInfo(typedNode, context.CancellationToken).Symbol;

            if (info is null)
                return;

            ITypeSymbol type = null;

            if (info is IMethodSymbol methodSymbol)
                type = methodSymbol.ReturnType;
            else if (info is ILocalSymbol localSymbol)
                type = localSymbol.Type;
            else if (info is IParameterSymbol parameterSymbol)
                type = parameterSymbol.Type;

            if (type is null)
                return;

            var typeWithoutGenerics = $"{type.ContainingNamespace.ToDisplayString()}.{type.Name}";

            if (Equals(typeWithoutGenerics, AwaiterConfiguredType))
                return;
                
            context.ReportDiagnostic(Diagnostic.Create(Rule, node.GetLocation(), node.ToString()));
        }
    }
}
