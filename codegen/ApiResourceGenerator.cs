using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Vulkanoid.Generators;

/// <summary>
/// Generate another part of partial API object with specific handle type, implicit cast and constructors
/// </summary>
[Generator]
public class ApiResourceGenerator : IIncrementalGenerator
{
    private sealed record Target
    {
        public string FileName;

        public string HandleTypeName;

        public string[] Usings;

        public INamedTypeSymbol Class;

        public Target(string fileName, string handleName, string[] usings, INamedTypeSymbol @class)
        {
            FileName = fileName;
            HandleTypeName = handleName;
            Usings = usings;
            Class = @class;
        }
    }

    private readonly string HandleAttributeName = "HandleAttribute";
    private readonly string HandleAttributeNamespace = "Vulkanoid";
    private readonly string DeviceTypeName = "VkDevice";

    private void Generate(SourceProductionContext context, Target target)
    {
        string fileName = target.FileName;
        string handleTypeName = target.HandleTypeName;

        var @class = target.Class;

        string classAccesibility = @class.DeclaredAccessibility switch
        {
            Accessibility.Private => "private",
            Accessibility.Protected => "protected",
            Accessibility.Internal => "internal",
            Accessibility.ProtectedAndInternal => "protected internal",
            Accessibility.Public => "public",
            _ => string.Empty,
        };

        string handleDeviceAccesibility = @class.IsSealed ? "private" : "protected";

        string classDefinition = @class.Name;

        if (@class.IsGenericType)
        {
            classDefinition += '<';

            foreach (var parameter in @class.TypeParameters.Take(@class.TypeParameters.Length - 1))
                classDefinition += $"{parameter}, ";

            classDefinition += $"{@class.TypeParameters.Last()}>";
        }

        string source = string.Empty;

        foreach (string @using in target.Usings)
            source += $"{@using}\r\n";

        if (target.Usings.Length > 0)
            source += "\r\n";

        source += $@"namespace {@class.ContainingNamespace};

{classAccesibility} partial class {classDefinition}
{{
    {handleDeviceAccesibility} readonly {handleTypeName} handle;

    {handleDeviceAccesibility} readonly {DeviceTypeName} device;

    public static implicit operator {handleTypeName}({classDefinition} resource) => resource.handle;";

        if (@class.BaseType is not null)
        {
            foreach (var baseConstructor in @class.BaseType.Constructors)
            {
                source += $@"

    public {@class.Name}({handleTypeName} handle, {DeviceTypeName} device";

                foreach (var parameter in baseConstructor.Parameters)
                {
                    source += ',';
                    source += $" {parameter.Type.Name} {parameter.Name}";
                }

                source += $")";

                if (baseConstructor.Parameters.Length > 0)
                {
                    source +=  " : base(";

                    foreach (var parameter in baseConstructor.Parameters.Take(@class.TypeParameters.Length - 1))
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

        context.AddSource($"{fileName}.gen.cs", SourceText.From(source, Encoding.UTF8));
    }

    private void GenerateAttribute(IncrementalGeneratorPostInitializationContext context)
    {
        string source = $@"namespace {HandleAttributeNamespace};

[AttributeUsage(AttributeTargets.Class)]
public class {HandleAttributeName}<THandle> : Attribute where THandle : struct {{ }}";

        context.AddSource($"{HandleAttributeName}.gen.cs", SourceText.From(source, Encoding.UTF8));
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(GenerateAttribute);

        var targetsProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
            $"{HandleAttributeNamespace}.{HandleAttributeName}`1", // <T> -> "`1" in metadata name (why?)
            (node, _) => node is ClassDeclarationSyntax,
            (context, _) =>
            {
                var classSyntax = (ClassDeclarationSyntax)context.TargetNode;

                string fileName = Path.GetFileNameWithoutExtension(classSyntax.SyntaxTree.FilePath);
                string handleTypeName = context.Attributes.First(x => x.AttributeClass!.Name == HandleAttributeName).AttributeClass!.TypeArguments[0].Name;

                string[] usings = ((CompilationUnitSyntax)classSyntax.Ancestors().First(x => x is CompilationUnitSyntax)).Usings
                    .Select(x => x.ToString()).ToArray();

                return new Target(fileName, handleTypeName, usings, (INamedTypeSymbol)context.TargetSymbol);
            });

        context.RegisterSourceOutput(targetsProvider, Generate);
    }
}