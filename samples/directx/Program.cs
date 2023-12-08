using Silk.NET.Windowing;

using Vulkanoid.Sample.DirectX;

using var window = Window.Create(WindowOptions.Default);

var renderer = new DirectRenderer(window);

window.Load += renderer.OnLoad;
window.Closing += renderer.OnDestroy;
window.Render += renderer.OnRender;
window.Resize += renderer.OnWindowSizeChanged;

window.Run();
