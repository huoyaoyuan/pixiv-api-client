using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Meowtrix.Generators
{
    [Generator]
    public class DeepNullTestGenerator : ISourceGenerator
    {
        public void Initialize(InitializationContext context)
            => context.RegisterForSyntaxNotifications(() => new DeepNullSyntaxReceiver());

        public void Execute(SourceGeneratorContext context)
        {
            var receiver = (DeepNullSyntaxReceiver)context.SyntaxReceiver!;
            var attr = context.Compilation.GetTypeByMetadataName("Meowtrix.GenerateNullTestAttribute");
            if (attr is null)
                return;

            var sourceBuilder = new StringBuilder();
            sourceBuilder.Append(
@"#nullable enable

namespace Meowtrix.Generated
{
    public static class DeepNullHelper
    {");

            foreach (var classDeclaration in receiver.ClassesWithAttributes)
            {
                var semanticModel = context.Compilation.GetSemanticModel(classDeclaration.SyntaxTree);
                var typeSymbol = (INamedTypeSymbol)semanticModel.GetDeclaredSymbol(classDeclaration, context.CancellationToken);
                if (typeSymbol.GetAttributes().Any(a => attr.Equals(a.AttributeClass, SymbolEqualityComparer.Default)))
                {
                    sourceBuilder.Append(@$"
        public static {typeSymbol.Name} DeepNullTest(this {typeSymbol.Name}? obj)
        {{");

                    sourceBuilder.Append(@"
            return obj!;
        }");
                }
            }
            sourceBuilder.Append(
@"
    }
}");

            context.AddSource("DeepNullTest.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }
    }

    internal class DeepNullSyntaxReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> ClassesWithAttributes { get; } = new List<ClassDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is ClassDeclarationSyntax classDeclaration
                && classDeclaration.AttributeLists.Count > 0)
                ClassesWithAttributes.Add(classDeclaration);
        }
    }
}
