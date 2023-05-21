namespace Vulkanoid.Vulkan;

[Handle<Pipeline>]
public sealed partial class VkPipeline : IDisposable
{
    public VkPipeline(Pipeline handle, PipelineLayout layout, VkDevice device) : this(handle, device) => Layout = layout;

    public PipelineLayout Layout { get; }

    public void Dispose() => throw new NotImplementedException();
}