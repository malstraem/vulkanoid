using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Vulkanoid.Generators;

[Generator]
public class AttributesGenerator : IIncrementalGenerator
{
    private const string HandleAttributeName = "HandleAttribute";
    private const string HandleAttributeNamespace = "Vulkanoid";

    private void GenerateAttribute(IncrementalGeneratorPostInitializationContext context)
    {
        string source = $@"namespace {HandleAttributeNamespace};

/// <summary>
/// Defines an associated API handle type
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class {HandleAttributeName}<THandle> : Attribute where THandle : unmanaged {{ }}";

        context.AddSource($"{HandleAttributeName}.gen.cs", SourceText.From(source, Encoding.UTF8));
    }

    public void Initialize(IncrementalGeneratorInitializationContext context) => context.RegisterPostInitializationOutput(GenerateAttribute);
}
