namespace Hexa.NET.Unsafe.Analyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using System.Linq;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UnreleasedIFreeableAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor UnreleasedFreeableRule = new DiagnosticDescriptor(
            "UF001",
            "IFreeable not released",
            "The field or property '{0}' implements IFreeable but is not released via Release()",
            "MemoryManagement",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [UnreleasedFreeableRule];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var typeDeclaration = (TypeDeclarationSyntax)context.Node;
            var semanticModel = context.SemanticModel;

            var methods = typeDeclaration.Members.OfType<MethodDeclarationSyntax>();

            foreach (var field in typeDeclaration.Members.OfType<BaseFieldDeclarationSyntax>())
            {
                var variable = field.Declaration.Variables.First();
                string memberName = variable.Identifier.Text;
                ITypeSymbol? typeSymbol = semanticModel.GetTypeInfo(field.Declaration.Type).Type;

                // Skip if the field is marked with the suppression attribute
                var fieldSymbol = context.SemanticModel.GetDeclaredSymbol(field.Declaration.Variables.First(), context.CancellationToken);
                if (fieldSymbol != null && fieldSymbol.GetAttributes().Any(attr => attr.AttributeClass?.Name == "SuppressFreeWarningAttribute"))
                {
                    continue;
                }

                HandleMember(ref context, methods, field, typeSymbol, memberName);
            }

            foreach (var property in typeDeclaration.Members.OfType<PropertyDeclarationSyntax>())
            {
                string memberName = property.Identifier.Text;
                ITypeSymbol? typeSymbol = semanticModel.GetTypeInfo(property.Type).Type;

                // Skip if the property is marked with the suppression attribute
                var propertySymbol = context.SemanticModel.GetDeclaredSymbol(property, context.CancellationToken);
                if (propertySymbol != null && propertySymbol.GetAttributes().Any(attr => attr.AttributeClass?.Name == "SuppressFreeWarningAttribute"))
                {
                    continue;
                }

                HandleMember(ref context, methods, property, typeSymbol, memberName);
            }
        }

        private static void HandleMember(ref SyntaxNodeAnalysisContext context, IEnumerable<MethodDeclarationSyntax> methods, MemberDeclarationSyntax member, ITypeSymbol? typeSymbol, string? memberName)
        {
            if (typeSymbol == null)
                return;

            // Check if the type implements IFreeable
            var implementsIFreeable = typeSymbol.AllInterfaces.Any(i => i.Name == "IFreeable");

            if (implementsIFreeable)
            {
                var isReleased = false;

                // Find all methods in the class/struct

                foreach (var method in methods)
                {
                    var methodBody = method.Body;
                    if (methodBody == null) continue;

                    // Check if Release() is called on the member
                    if (methodBody.DescendantNodes()
                        .OfType<InvocationExpressionSyntax>()
                        .Any(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                                           memberAccess.Name.Identifier.Text == "Release" &&
                                           memberAccess.Expression.ToString() == memberName))
                    {
                        isReleased = true;
                        break;
                    }
                }

                if (!isReleased)
                {
                    var diagnostic = Diagnostic.Create(UnreleasedFreeableRule, member.GetLocation(), memberName);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}