using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Vulkanoid.Generators;

/// <summary>
/// Generate another part of partial API object with specific handle and device fields, implicit cast and constructors
/// </summary>
public abstract class ApiGenerator : IIncrementalGenerator
{
    private sealed record Target
    {
        public string Filename;

        public string HandleTypeName;

        public string[] Usings;

        public INamedTypeSymbol Symbol;

        public Target(string filename, string handleTypeName, string[] usings, INamedTypeSymbol symbol)
        {
            Filename = filename;
            HandleTypeName = handleTypeName;
            Usings = usings;
            Symbol = symbol;
        }
    }

#pragma warning disable CS8618
    protected string deviceTypeName;
    protected string @namespace;
#pragma warning restore CS8618

    private const string HandleAttributeName = "HandleAttribute";
    private const string HandleAttributeNamespace = "Vulkanoid";

    private void Generate(SourceProductionContext context, ImmutableArray<Target> targets)
    {
        foreach (var target in targets)
        {
            var symbol = target.Symbol;

            string handleTypeName = target.HandleTypeName;

            string handleFieldAccessibility = symbol.IsSealed ? "private" : "protected";
            string constructorAccessibility = symbol.Constructors[0].IsImplicitlyDeclared ? "public" : "private";

            string symbolDefinition = symbol.Name;

            if (symbol.IsGenericType)
            {
                symbolDefinition += '<';

                foreach (var parameter in symbol.TypeParameters.Take(symbol.TypeParameters.Length - 1))
                    symbolDefinition += $"{parameter}, ";

                symbolDefinition += $"{symbol.TypeParameters.Last()}>";
            }

            var builder = new StringBuilder("#pragma warning disable CS8618").AppendLine().AppendLine();

            foreach (string @using in target.Usings)
                _ = builder.AppendLine(@using);

            if (target.Usings.Length > 0)
                _ = builder.AppendLine();

            _ = builder.AppendLine($@"namespace {@namespace};

public partial class {symbolDefinition}
{{
    {handleFieldAccessibility} readonly {handleTypeName} handle;

    {handleFieldAccessibility} readonly {deviceTypeName} device;

    public static implicit operator {handleTypeName}({symbolDefinition} resource) => resource.handle;");

            if (symbol.BaseType is not null)
            {
                foreach (var baseConstructor in symbol.BaseType.Constructors)
                {
                    _ = builder.Append($@"
    {constructorAccessibility} {symbol.Name}({handleTypeName} handle, {deviceTypeName} device");

                    foreach (var parameter in baseConstructor.Parameters)
                        _ = builder.Append($", {parameter.Type.Name} {parameter.Name}");

                    _ = builder.Append(')');

                    if (baseConstructor.Parameters.Length > 0)
                    {
                        _ = builder.Append(" : base(");

                        foreach (var parameter in baseConstructor.Parameters.Take(symbol.TypeParameters.Length - 1))
                            _ = builder.Append($"{parameter.Name}, ");

                        _ = builder.Append($"{baseConstructor.Parameters.Last().Name})");
                    }

                    _ = builder.AppendLine($@"
    {{
        this.handle = handle;
        this.device = device;
    }}");
                }
            }

            _ = builder.Append('}');

            string source = builder.ToString();

            context.AddSource($"{target.Filename}.gen.cs", SourceText.From(source, Encoding.UTF8));
        }
    }

    private static string GetNamespace(BaseTypeDeclarationSyntax syntax)
    {
        string @namespace = string.Empty;

        var potentialNamespaceSyntax = syntax.Parent;

        while (potentialNamespaceSyntax is not null and not NamespaceDeclarationSyntax and not FileScopedNamespaceDeclarationSyntax)
            potentialNamespaceSyntax = potentialNamespaceSyntax.Parent;

        if (potentialNamespaceSyntax is BaseNamespaceDeclarationSyntax namespaceSyntax)
        {
            @namespace = namespaceSyntax.Name.ToString();

            while (true)
            {
                if (namespaceSyntax.Parent is not NamespaceDeclarationSyntax parentNamespaceSyntax)
                    break;

                @namespace = $"{namespaceSyntax.Name}.{@namespace}";
                namespaceSyntax = parentNamespaceSyntax;
            }
        }

        return @namespace;
    }

    protected virtual string GetHandleTypeName(GeneratorAttributeSyntaxContext context)
        => context.Attributes.First(x => x.AttributeClass!.Name == HandleAttributeName).AttributeClass!.TypeArguments[0].Name;

    private IncrementalValueProvider<ImmutableArray<Target>> GetProvider(IncrementalGeneratorInitializationContext context)
    {
        return context.SyntaxProvider.ForAttributeWithMetadataName(
            $"{HandleAttributeNamespace}.{HandleAttributeName}`1", // <T> -> `1 in metadata name
            (node, _) => node is ClassDeclarationSyntax classSyntax && GetNamespace(classSyntax) == @namespace,
            (context, _) =>
            {
                var classSyntax = (ClassDeclarationSyntax)context.TargetNode;

                string filename = Path.GetFileNameWithoutExtension(classSyntax.SyntaxTree.FilePath);
                string handleTypeName = GetHandleTypeName(context);

                string[] usings = ((CompilationUnitSyntax)classSyntax.Ancestors()
                    .First(x => x is CompilationUnitSyntax))
                        .Usings.Select(x => x.ToString()).ToArray();

                return new Target(filename, handleTypeName, usings, (INamedTypeSymbol)context.TargetSymbol);
            }).Collect();
    }

    public void Initialize(IncrementalGeneratorInitializationContext context) => context.RegisterSourceOutput(GetProvider(context), Generate);
}
