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
    protected sealed record Target
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

    private const string HandleAttributeName = "HandleAttribute";
    private const string HandleAttributeNamespace = "Vulkanoid";

#pragma warning disable CS8618
    protected string deviceTypeName;
    protected string @namespace;
#pragma warning restore CS8618

    private void Generate(SourceProductionContext context, ImmutableArray<Target> targets)
    {
        foreach (var target in targets)
        {
            var symbol = target.Symbol;

            string handleTypeName = target.HandleTypeName;

            string handleFieldAccessibility = symbol.IsSealed ? "private" : "protected";
            string constructorAccessibility = symbol.Constructors[0].IsImplicitlyDeclared ? "public" : "private";
            string symbolAccesibility = symbol.DeclaredAccessibility switch
            {
                Accessibility.Private => "private",
                Accessibility.Protected => "protected",
                Accessibility.Internal => "internal",
                Accessibility.ProtectedAndInternal => "protected internal",
                Accessibility.Public => "public",
                _ => string.Empty,
            };

            string symbolDefinition = symbol.Name;

            if (symbol.IsGenericType)
            {
                symbolDefinition += '<';

                foreach (var parameter in symbol.TypeParameters.Take(symbol.TypeParameters.Length - 1))
                    symbolDefinition += $"{parameter}, ";

                symbolDefinition += $"{symbol.TypeParameters.Last()}>";
            }

            string source = "#pragma warning disable CS8618\r\n\r\n";

            foreach (string @using in target.Usings)
                source += $"{@using}\r\n";

            if (target.Usings.Length > 0)
                source += "\r\n";

            source += $@"namespace {@namespace};

{symbolAccesibility} partial class {symbolDefinition}
{{
    {handleFieldAccessibility} readonly {handleTypeName} handle;

    {handleFieldAccessibility} readonly {deviceTypeName} device;

    public static implicit operator {handleTypeName}({symbolDefinition} resource) => resource.handle;";

            if (symbol.BaseType is not null)
            {
                foreach (var baseConstructor in symbol.BaseType.Constructors)
                {
                    source += $@"

    {constructorAccessibility} {symbol.Name}({handleTypeName} handle, {deviceTypeName} device";

                    foreach (var parameter in baseConstructor.Parameters)
                    {
                        source += ',';
                        source += $" {parameter.Type.Name} {parameter.Name}";
                    }

                    source += $")";

                    if (baseConstructor.Parameters.Length > 0)
                    {
                        source += " : base(";

                        foreach (var parameter in baseConstructor.Parameters.Take(symbol.TypeParameters.Length - 1))
                            source += $"{parameter.Name}, ";

                        source += $"{baseConstructor.Parameters.Last().Name})";
                    }

                    source += $@"
    {{
        this.handle = handle;
        this.device = device;
    }}";
                }
            }

            source += "\r\n}";

            context.AddSource($"{target.Filename}.gen.cs", SourceText.From(source, Encoding.UTF8));
        }
    }

    private static string GetNamespace(BaseTypeDeclarationSyntax syntax)
    {
        string @namespace = string.Empty;

        var potentialNamespaceParent = syntax.Parent;

        while (potentialNamespaceParent is not null and not NamespaceDeclarationSyntax and not FileScopedNamespaceDeclarationSyntax)
            potentialNamespaceParent = potentialNamespaceParent.Parent;

        if (potentialNamespaceParent is BaseNamespaceDeclarationSyntax namespaceParent)
        {
            @namespace = namespaceParent.Name.ToString();

            while (true)
            {
                if (namespaceParent.Parent is not NamespaceDeclarationSyntax parent)
                    break;

                @namespace = $"{namespaceParent.Name}.{@namespace}";
                namespaceParent = parent;
            }
        }

        return @namespace;
    }

    protected virtual string GetHandleTypeName(GeneratorAttributeSyntaxContext context)
        => context.Attributes.First(x => x.AttributeClass!.Name == HandleAttributeName).AttributeClass!.TypeArguments[0].Name;

    private IncrementalValueProvider<ImmutableArray<Target>> GetProvider(IncrementalGeneratorInitializationContext context)
    {
        return context.SyntaxProvider.ForAttributeWithMetadataName(
            $"{HandleAttributeNamespace}.{HandleAttributeName}`1", // <T> -> `1 in metadata name (why?)
            (node, _) => node is ClassDeclarationSyntax classSyntax && GetNamespace(classSyntax) == @namespace,
            (context, _) =>
            {
                var classSyntax = (ClassDeclarationSyntax)context.TargetNode;

                string filename = Path.GetFileNameWithoutExtension(classSyntax.SyntaxTree.FilePath);
                string handleTypeName = GetHandleTypeName(context);

                string[] usings = ((CompilationUnitSyntax)classSyntax.Ancestors().First(x => x is CompilationUnitSyntax)).Usings.Select(x => x.ToString()).ToArray();

                return new Target(filename, handleTypeName, usings, (INamedTypeSymbol)context.TargetSymbol);
            }).Collect();
    }

    public void Initialize(IncrementalGeneratorInitializationContext context) => context.RegisterSourceOutput(GetProvider(context), Generate);
}
