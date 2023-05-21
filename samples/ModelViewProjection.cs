using Silk.NET.Maths;

namespace Vulkanoid.Sample;

public struct ModelViewProjection
{
    public Matrix4X4<float> Model;
    public Matrix4X4<float> View;
    public Matrix4X4<float> Projection;
}