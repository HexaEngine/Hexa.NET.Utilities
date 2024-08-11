namespace Hexa.NET.Unsafe.Analyzers
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using System.Collections.Immutable;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PointerFreeAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor AllocationWithoutFreeRule = new DiagnosticDescriptor(
            "PFA001",
            "Allocation without Free",
            "Memory allocated with {0} is not freed or written to a field",
            "MemoryManagement",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [AllocationWithoutFreeRule];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeMethodInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeMethodInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var methodName = (invocation.Expression as IdentifierNameSyntax)?.Identifier.Text;

            if (methodName != "Alloc" && methodName != "AllocT")
                return;

            var containingMethod = invocation.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (containingMethod == null)
                return;

            var hasFree = false;
            var assignsToField = false;

            foreach (var descendant in containingMethod.DescendantNodes())
            {
                if (descendant is InvocationExpressionSyntax freeInvocation)
                {
                    var freeMethodName = (freeInvocation.Expression as IdentifierNameSyntax)?.Identifier.Text;
                    if (freeMethodName == "Free")
                    {
                        hasFree = true;
                        break;
                    }
                }
                else if (descendant is AssignmentExpressionSyntax assignment)
                {
                    if (assignment.Left is IdentifierNameSyntax leftAccess)
                    {
                        var symbol = context.SemanticModel.GetSymbolInfo(leftAccess).Symbol;
                        if (symbol is IFieldSymbol || symbol is IPropertySymbol)
                        {
                            assignsToField = true;
                            break;
                        }
                    }
                }
            }

            if (!hasFree && !assignsToField)
            {
                var diagnostic = Diagnostic.Create(AllocationWithoutFreeRule, invocation.GetLocation(), methodName);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}