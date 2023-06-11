namespace Vulkanoid.Vulkan;

public sealed partial class VkDevice : GraphicsDevice
{
    public Shader CreateShader(byte[] code, string entryPoint, ShaderStageFlags shaderStage)
    {
        unsafe
        {
            var createInfo = new ShaderModuleCreateInfo(codeSize: (nuint)code.Length);

            fixed (byte* codePtr = code)
                createInfo.PCode = (uint*)codePtr;

            vk.CreateShaderModule(handle, &createInfo, null, out var moduleHandle);

            var shaderModule = new VkShaderModule(moduleHandle, this);

            return new Shader { EntryPoint = entryPoint, Module = shaderModule, Stage = shaderStage };
        }
    }

    public void DestroyShaderModule(VkShaderModule module)
    {
        unsafe
        {
            vk.DestroyShaderModule(handle, module, null);
        }
    }
}