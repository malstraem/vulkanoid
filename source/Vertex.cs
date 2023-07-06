using System.Runtime.InteropServices;

using Silk.NET.Maths;

namespace Vulkanoid;

public readonly record struct Vertex
{
    public readonly Vector3D<float> Position;
    public readonly Vector3D<float> Normal;
    public readonly Vector2D<float> Texcoord;

    public Vertex(Vector3D<float> position, Vector3D<float> normal, Vector2D<float> texcoord)
    {
        Position = position;
        Normal = normal;
        Texcoord = texcoord;
    }

    public static VertexInputBindingDescription BindingDescription
    {
        get
        {
            unsafe
            {
                return new(0, (uint)sizeof(Vertex), VertexInputRate.Vertex);
            }
        }
    }

    public static VertexInputAttributeDescription[] AttributeDescriptions => new VertexInputAttributeDescription[]
    {
        new(0, 0, Format.R32G32B32Sfloat, (uint)Marshal.OffsetOf<Vertex>(nameof(Position))),
        new(1, 0, Format.R32G32B32Sfloat, (uint)Marshal.OffsetOf<Vertex>(nameof(Normal))),
        new(2, 0, Format.R32G32Sfloat, (uint)Marshal.OffsetOf<Vertex>(nameof(Texcoord)))
    };
}
