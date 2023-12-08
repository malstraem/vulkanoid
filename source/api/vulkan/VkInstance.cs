using System.Diagnostics;
using System.Runtime.InteropServices;

using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Vulkanoid.Vulkan;

public sealed class VkInstance
{
    private readonly Vk vk;

    private ExtDebugUtils debugExt;
    private DebugUtilsMessengerEXT debugMessenger;

    internal readonly KhrSurface? surfaceExt;

    internal readonly Instance handle;

    [Conditional("DEBUG")]
    private void DebugSetup()
    {
        static unsafe uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity, DebugUtilsMessageTypeFlagsEXT messageTypes,
                                         DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData)
        {
            if (messageSeverity > DebugUtilsMessageSeverityFlagsEXT.InfoBitExt)
                Console.WriteLine($"{messageSeverity} {messageTypes}" + Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage) + '\n');

            return 0u;
        }

        if (!vk.TryGetInstanceExtension(handle, out debugExt))
            return;

        var messageSeverity = DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt
                            | DebugUtilsMessageSeverityFlagsEXT.WarningBitExt
                            | DebugUtilsMessageSeverityFlagsEXT.InfoBitExt
                            | DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt;

        var messageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt
                        | DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt
                        | DebugUtilsMessageTypeFlagsEXT.ValidationBitExt;

        unsafe
        {
            var createInfo = new DebugUtilsMessengerCreateInfoEXT(messageSeverity: messageSeverity,
                                                                  messageType: messageType,
                                                                  pfnUserCallback: (PfnDebugUtilsMessengerCallbackEXT)DebugCallback);

            debugExt.CreateDebugUtilsMessenger(handle, createInfo, null, out debugMessenger).Check();
        }
    }

    public VkInstance(Vk vk, string[] extensions)
    {
        this.vk = vk;
#if DEBUG
        extensions = extensions.Append(ExtDebugUtils.ExtensionName).ToArray();
#endif
        //string?[] availableLayerNames;

        unsafe
        {
            /*uint layerCount = default;
            vk.EnumerateInstanceLayerProperties(ref layerCount, null);

            var availableLayers = new LayerProperties[layerCount];

            fixed (LayerProperties* availableLayersPtr = availableLayers)
                vk.EnumerateInstanceLayerProperties(ref layerCount, availableLayersPtr);

            availableLayerNames = availableLayers.Select(x => Marshal.PtrToStringAnsi((nint)x.LayerName)).ToArray();*/

            var appInfo = new ApplicationInfo(apiVersion: Vk.Version11);

            var createInfo = new InstanceCreateInfo(
                pApplicationInfo: &appInfo,
                ppEnabledExtensionNames: (byte**)SilkMarshal.StringArrayToPtr(extensions),
                enabledExtensionCount: (uint)extensions.Length);
#if DEBUG
            createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(new string[] { "VK_LAYER_KHRONOS_validation" });
            createInfo.EnabledLayerCount = 1u;
#endif
            vk.CreateInstance(createInfo, null, out handle).Check();
        }

        vk.CurrentInstance = handle;
        vk.TryGetInstanceExtension(handle, out surfaceExt);

        DebugSetup();
    }

    public static implicit operator Instance(VkInstance resource) => resource.handle;

    public IEnumerable<VkPhysicalDevice> GetPhysicalDevices()
    {
        unsafe
        {
            var deviceHandles = vk.GetPhysicalDevices(handle);

            if (deviceHandles.Count == 0)
                throw new NotSupportedException("GPU with Vulkan support not found");

            var devices = new List<VkPhysicalDevice>();

            foreach (var deviceHandle in deviceHandles)
                devices.Add(new VkPhysicalDevice(deviceHandle, vk));

            return devices;
        }
    }

    ~VkInstance()
    {
        unsafe
        {
            vk.DestroyInstance(handle, null);
        }
    }
}
