using System.Reflection;

namespace Vulkanoid.Sample;

public static class Embedded
{
    public static byte[] GetShaderBytes(string filename)
    {
        var assembly = Assembly.GetExecutingAssembly();
        string name = assembly.GetName().Name ?? throw new Exception("could not get assambly name");

        using var stream = assembly.GetManifestResourceStream($"{name}.shaders.{filename}") ?? throw new Exception($"could not load: {filename}");
        using var reader = new BinaryReader(stream);

        return reader.ReadBytes((int)stream!.Length);
    }
}
