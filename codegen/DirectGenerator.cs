using Microsoft.CodeAnalysis;

namespace Vulkanoid.Generators;

[Generator]
public class DirectGenerator : ApiGenerator
{
    protected override string GetHandleTypeName(GeneratorAttributeSyntaxContext context) => $"ComPtr<{base.GetHandleTypeName(context)}>";

    public DirectGenerator()
    {
        deviceTypeName = "D12Device";
        @namespace = "Vulkanoid.DirectX";
    }
}
