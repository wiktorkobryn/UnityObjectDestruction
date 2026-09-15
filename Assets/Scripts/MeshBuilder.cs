using System.Collections.Generic;
using UnityEngine;

public class MeshBuilder
{
    private List<Vector3> vertices = new List<Vector3>();
    private List<Vector3> normals = new List<Vector3>();
    private List<Vector2> uvs = new List<Vector2>();
    private List<int> triangles = new List<int>();

    public void AddTriangle(
        VertexData a,
        VertexData b,
        VertexData c)
    {
        int index = vertices.Count;

        vertices.Add(a.position);
        vertices.Add(b.position);
        vertices.Add(c.position);

        normals.Add(a.normal);
        normals.Add(b.normal);
        normals.Add(c.normal);

        uvs.Add(a.uv);
        uvs.Add(b.uv);
        uvs.Add(c.uv);

        triangles.Add(index);
        triangles.Add(index + 1);
        triangles.Add(index + 2);
    }

    public void AddTriangle(VertexData a, VertexData b, VertexData c, Vector3 referenceNormal)
    {
        // recreating normal of a base triangle
        Vector3 normal = Vector3.Cross(b.position - a.position, c.position - a.position);

        if (Vector3.Dot(normal, referenceNormal) < 0f)
            (b, c) = (c, b);

        AddTriangle(a, b, c);
    }

    public Mesh Build()
    {
        Mesh mesh = new Mesh();

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);

        return mesh;
    }
}