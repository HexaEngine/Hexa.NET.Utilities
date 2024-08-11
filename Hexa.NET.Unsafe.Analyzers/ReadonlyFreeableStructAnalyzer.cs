namespace Hexa.NET.Unsafe.Analyzers
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using System.Collections.Immutable;
    using System.Linq;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ReadonlyFreeableStructAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor ReadonlyFreeableStructRule = new DiagnosticDescriptor(
            "RFS001",
            "Readonly IFreeable Struct",
            "The struct '{0}' implements IFreeable and should not be used with the readonly modifier",
            "MemoryManagement",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ReadonlyFreeableStructRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeFieldDeclaration, SyntaxKind.FieldDeclaration);
        }

        private static void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
        {
            var fieldDeclaration = (FieldDeclarationSyntax)context.Node;

            // Check if the field is readonly
            if (!fieldDeclaration.Modifiers.Any(SyntaxKind.ReadOnlyKeyword))
            {
                return;
            }

            var semanticModel = context.SemanticModel;
            var typeSymbol = semanticModel.GetTypeInfo(fieldDeclaration.Declaration.Type).Type;

            // Check if the type implements IFreeable and is a struct
            if (typeSymbol != null &&
                typeSymbol.TypeKind == TypeKind.Struct &&
                typeSymbol.AllInterfaces.Any(i => i.Name == "IFreeable"))
            {
                // Issue a diagnostic for each variable in the declaration
                foreach (var variable in fieldDeclaration.Declaration.Variables)
                {
                    var diagnostic = Diagnostic.Create(ReadonlyFreeableStructRule, variable.GetLocation(), typeSymbol.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}