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

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        private static readonly string AwaiterConfiguredType = typeof(ConfiguredTaskAwaitable).FullName;

        private const string AspNetCoreMvcControllerBaseFullName = "Microsoft.AspNetCore.Mvc.ControllerBase";

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(nodeContext =>
            {
                INamedTypeSymbol aspNetCoreMvcControllerType = nodeContext.Compilation.GetTypeByMetadataName(AspNetCoreMvcControllerBaseFullName);
                AnalyzeNode(nodeContext, aspNetCoreMvcControllerType);
            }, SyntaxKind.AwaitExpression);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context, INamedTypeSymbol aspNetCoreMvcControllerType)
        {
            if (context.Compilation.GetDiagnostics().Any(issue => issue.Severity == DiagnosticSeverity.Error || issue.IsWarningAsError))
                return;

            bool isAspNetCore = !(aspNetCoreMvcControllerType is null);

            if(isAspNetCore && IsAspNetCoreControllerContext(context, aspNetCoreMvcControllerType))
                return;

            var node = (AwaitExpressionSyntax)context.Node;

            var child = node.Expression;

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

        private static bool IsAspNetCoreControllerContext(SyntaxNodeAnalysisContext context, INamedTypeSymbol aspNetCoreMvcControllerType)
        {
            var node  = context.Node;   
            while(!(node is null) && !(node is ClassDeclarationSyntax))
                node = node.Parent;
            
            var parentClass = node as ClassDeclarationSyntax;

            var baseTypes = parentClass.BaseList?.Types.Select(t => t.Type) ?? Enumerable.Empty<TypeSyntax>();

            foreach (var baseType in baseTypes)
            {
                var baseTypeSymbol = context.SemanticModel.GetTypeInfo(baseType).Type;

                if(baseTypeSymbol is null || baseTypeSymbol.TypeKind == TypeKind.Interface)
                    continue;

                if(InheritsFrom(baseTypeSymbol, aspNetCoreMvcControllerType))
                    return true;
            }

            return false;
        }

        private static bool InheritsFrom(ITypeSymbol symbol, ITypeSymbol type)
        {
            var baseType = symbol;
            while (baseType != null)
            {
                if (type.Equals(baseType))
                    return true;

                baseType = baseType.BaseType;
            }

            return false;
        }
    }
}
