using UnityEngine;

public struct VertexData
{
    public Vector3 position;
    public Vector3 normal;
    public Vector2 uv;
    public bool side;

    public VertexData(
        Vector3 position,
        Vector3 normal,
        Vector2 uv,
        bool side)
    {
        this.position = position;
        this.normal = normal;
        this.uv = uv;
        this.side = side;
    }
}
