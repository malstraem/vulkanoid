using System.Diagnostics;
using System.Runtime.InteropServices;

using Silk.NET.Core.Contexts;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Vulkanoid.Vulkan;

public sealed class VkInstance
{
    private readonly Vk vk;

    private readonly KhrSurface surfaceExtension;
    private ExtDebugUtils debugExtension;

    private DebugUtilsMessengerEXT debugMessenger;

    internal readonly Instance handle;

    private QueueFamilies FindQueueFamilies(PhysicalDevice device, SurfaceKHR? surface)
    {
        uint queueFamilyCount = 0;
        var queueFamilies = new QueueFamilies();

        unsafe
        {
            vk.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilyCount, null);

            var queueFamilyProperties = new Span<QueueFamilyProperties>(new QueueFamilyProperties[queueFamilyCount]);
            vk.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, queueFamilyProperties);

            for (uint i = 0u; i < queueFamilyCount; i++)
            {
                if (queueFamilyProperties[(int)i].QueueFlags.HasFlag(QueueFlags.GraphicsBit))
                {
                    queueFamilies.GraphicsIndex = i;
                    break;
                }
            }

            if (surface is not null)
            {
                for (uint i = 0u; i < queueFamilyCount; i++)
                {
                    surfaceExtension.GetPhysicalDeviceSurfaceSupport(device, i, surface!.Value, out var presentSupported);

                    if (presentSupported)
                    {
                        queueFamilies.PresentIndex = i;
                        break;
                    }
                }
            }
        }
        return queueFamilies;
    }

    private SwapchainSupport GetSwapchainSupport(PhysicalDevice device, SurfaceKHR surface)
    {
        surfaceExtension.GetPhysicalDeviceSurfaceCapabilities(device, surface, out var surfaceCapabilities);

        uint formatCount = 0u, presentModeCount = 0u;

        unsafe
        {
            surfaceExtension.GetPhysicalDeviceSurfaceFormats(device, surface, &formatCount, null);

            Span<SurfaceFormatKHR> formats = stackalloc SurfaceFormatKHR[(int)formatCount];
            surfaceExtension.GetPhysicalDeviceSurfaceFormats(device, surface, &formatCount, formats);

            surfaceExtension.GetPhysicalDeviceSurfacePresentModes(device, surface, &presentModeCount, null);

            Span<PresentModeKHR> presentModes = stackalloc PresentModeKHR[(int)presentModeCount];
            surfaceExtension.GetPhysicalDeviceSurfacePresentModes(device, surface, &presentModeCount, presentModes);

            return new(surfaceCapabilities, formats.ToArray(), presentModes.ToArray());
        }
    }

    [Conditional("VULKAN_VALIDATION")]
    private void DebugSetup()
    {
        if (!vk.TryGetInstanceExtension(handle, out debugExtension))
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

            debugExtension.CreateDebugUtilsMessenger(handle, createInfo, null, out debugMessenger);
        }
    }

    private static unsafe uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity, DebugUtilsMessageTypeFlagsEXT messageTypes,
                                             DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData)
    {
        if (messageSeverity > DebugUtilsMessageSeverityFlagsEXT.InfoBitExt)
            Console.WriteLine($"{messageSeverity} {messageTypes}" + Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage) + '\n');

        return 0u;
    }

    public VkInstance(Vk vk, IList<string> extensions)
    {
        this.vk = vk;

        extensions.Add(ExtDebugUtils.ExtensionName);

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
                ppEnabledExtensionNames: (byte**)SilkMarshal.StringArrayToPtr(extensions.ToArray()), 
                enabledExtensionCount: (uint)extensions.Count,
                enabledLayerCount: 1u,
                ppEnabledLayerNames: (byte**)SilkMarshal.StringArrayToPtr(new string[] { "VK_LAYER_KHRONOS_validation" }));

            var result = vk.CreateInstance(createInfo, null, out handle);
        }

        IsPresentSupported = vk.TryGetInstanceExtension(handle, out surfaceExtension);

        DebugSetup();
    }

    public static implicit operator Instance(VkInstance resource) => resource.handle;

    public IEnumerable<VkPhysicalDevice> GetPhysicalDevices(IVkSurface surface)
    {
        unsafe
        {
            var surfaceHandle = surface.Create<AllocationCallbacks>(handle.ToHandle(), null).ToSurface();

            if (!IsPresentSupported & surface is not null)
                throw new NotSupportedException("Presentation requested but not supported");

            var deviceHandles = vk.GetPhysicalDevices(handle);

            if (deviceHandles.Count == 0)
                throw new NotSupportedException("GPU with Vulkan support not found");

            var devices = new List<VkPhysicalDevice>();

            foreach (var deviceHandle in deviceHandles)
            {
                   var queueFamilies = FindQueueFamilies(deviceHandle, surfaceHandle);

                SwapchainSupport swapchainSupport = default;

                if (surface is not null && queueFamilies.PresentIndex is null)
                    continue;

                if (surface is not null)
                    swapchainSupport = GetSwapchainSupport(deviceHandle, surfaceHandle);

                devices.Add(new VkPhysicalDevice(deviceHandle, queueFamilies, swapchainSupport, vk));
            }

            return devices;
        }
    }

    public bool IsPresentSupported { get; private init; }

    ~VkInstance()
    {
        unsafe
        {
            vk.DestroyInstance(handle, null);
        }
    }
}