namespace Hexa.NET.Unsafe.Analyzers
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using System.Collections.Immutable;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MemberNotFreedAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor MemberNotFreedRule = new DiagnosticDescriptor(
            "MNF001",
            "Class/Struct Member Not Freed",
            "The member '{0}' is not freed in any method of the containing class/struct",
            "MemoryManagement",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [MemberNotFreedRule];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var typeDeclaration = (TypeDeclarationSyntax)context.Node;

            // Find all methods in the class/struct
            var methods = typeDeclaration.Members.OfType<MethodDeclarationSyntax>();

            foreach (var field in typeDeclaration.Members.OfType<FieldDeclarationSyntax>().Where(field => field.Declaration.Type is PointerTypeSyntax))
            {
                var fieldSymbol = context.SemanticModel.GetDeclaredSymbol(field.Declaration.Variables.First(), context.CancellationToken);
                if (fieldSymbol != null && fieldSymbol.GetAttributes().Any(attr => attr.AttributeClass?.Name == "SuppressFreeWarningAttribute"))
                {
                    continue;
                }

                var fieldName = field.Declaration.Variables.First().Identifier.Text;

                HandleMember(ref context, methods, field, fieldName);
            }

            foreach (var property in typeDeclaration.Members.OfType<PropertyDeclarationSyntax>().Where(property => property.Type is PointerTypeSyntax))
            {
                var propertySymbol = context.SemanticModel.GetDeclaredSymbol(property, context.CancellationToken);
                if (propertySymbol != null && propertySymbol.GetAttributes().Any(attr => attr.AttributeClass?.Name == "SuppressFreeWarningAttribute"))
                {
                    continue;
                }

                var propertyName = property.Identifier.Text;

                HandleMember(ref context, methods, property, propertyName);
            }
        }

        private static void HandleMember(ref SyntaxNodeAnalysisContext context, IEnumerable<MethodDeclarationSyntax> methods, MemberDeclarationSyntax member, string memberName)
        {
            var isFreed = false;

            foreach (var method in methods)
            {
                var methodBody = method.Body;
                if (methodBody == null) continue;

                // Check if the field is freed in any method
                if (methodBody.DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .Any(invocation => invocation.Expression.ToString().Contains("Free") &&
                                       invocation.ArgumentList.Arguments.Any(arg => arg.ToString().Equals(memberName))))
                {
                    isFreed = true;
                    break;
                }
            }

            if (!isFreed)
            {
                var diagnostic = Diagnostic.Create(MemberNotFreedRule, member.GetLocation(), memberName);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}