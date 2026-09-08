using System.Collections.Generic;
using UnityEngine;

public class MeshSlicer : MonoBehaviour
{
    private Mesh objectMesh;
    private MeshBuilder positiveMesh, negativeMesh;

    public Transform slicePlaneTransform;
    private Plane slicePlane;

    private void Start()
    {
        objectMesh = GetComponent<MeshFilter>().mesh;
    }
    
    private void GetSlicePlaneData()
    {
        // transforming plane to mesh local space
        Vector3 localPlanePosition = transform.InverseTransformPoint(slicePlaneTransform.position);
        Vector3 localPlaneNormal = transform.InverseTransformDirection(slicePlaneTransform.up).normalized;
        slicePlane = new Plane(localPlaneNormal, localPlanePosition);
    }


    public void SliceMesh()
    {
        GetSlicePlaneData();

        // vertices and triangles in a base mesh
        Vector3[] vertices = objectMesh.vertices;
        Vector3[] normals = objectMesh.normals;
        Vector2[] uvs = objectMesh.uv;
        int[] triangles = objectMesh.triangles;

        // 2 new meshes to divide triangles
        positiveMesh = new MeshBuilder();
        negativeMesh = new MeshBuilder();

        // iterating over triangles in a mesh - 3D array packed into 2D
        // [t1-A, t1-B, t1-C, t2-A, t2-B, t2-C, ...]
        for (int i = 0; i < triangles.Length; i += 3)
        {
            // getting positions of vertices in a single triangle
            int aIndex = triangles[i];
            int bIndex = triangles[i + 1];
            int cIndex = triangles[i + 2];

            // operating on entire data of a vertex - position, normal, uv
            VertexData aVert = new VertexData(vertices[aIndex], normals[aIndex], uvs[aIndex], slicePlane.GetSide(vertices[aIndex]));
            VertexData bVert = new VertexData(vertices[bIndex], normals[bIndex], uvs[bIndex], slicePlane.GetSide(vertices[bIndex]));
            VertexData cVert = new VertexData(vertices[cIndex], normals[cIndex], uvs[cIndex], slicePlane.GetSide(vertices[cIndex]));

            // defining state of the triangle
            if (aVert.side && bVert.side && cVert.side)             // entire triangle on side 1: +++
                positiveMesh.AddTriangle(aVert, bVert, cVert);
            else if (!aVert.side && !bVert.side && !cVert.side)     // entire triangle on side 0: ---
                negativeMesh.AddTriangle(aVert, bVert, cVert);
            else                                                    // triangle cut in half by a plane: ++/- or +/--
                SliceTriangle(aVert, bVert, cVert);
        }


        CreateMeshObject(positiveMesh.Build(), name + "SlicePositive", true, true);
        CreateMeshObject(negativeMesh.Build(), name + "SliceNegative", true, true);
        Destroy(gameObject);
    }

    private VertexData GetIntersection(VertexData lineStart, VertexData lineEnd)
    {
        Vector3 rayDirection = lineEnd.position - lineStart.position;
        Ray ray = new Ray(lineStart.position, rayDirection.normalized);

        // getting the intersection point of ray and plane
        slicePlane.Raycast(ray, out float distance);
        Vector3 position = ray.GetPoint(distance);

        // interpolating normal & uv for new vertex
        float t = distance / rayDirection.magnitude; // how close is the new vertex to line start and end
        Vector3 normal = Vector3.Lerp(lineStart.normal, lineEnd.normal, t).normalized;
        Vector2 uv = Vector2.Lerp(lineStart.uv, lineEnd.uv, t);

        return new VertexData(position, normal, uv, slicePlane.GetSide(position));
    }

    private void SliceTriangle(VertexData aVert, VertexData bVert, VertexData cVert)
    {
        // remembering what side the normal was facing in original triangle
        Vector3 referenceNormal = Vector3.Cross(bVert.position - aVert.position, cVert.position - aVert.position);

        // defining what side of a cut are verts in
        bool aVertSide = aVert.side;
        bool bVertSide = bVert.side;
        bool cVertSide = cVert.side;

        // Rearrange vertices so A is always the single vertex on one side ( A | BC )
        if (aVertSide == bVertSide)
            (aVert, cVert) = (cVert, aVert); // vertex C was on a separate side
        else if (aVertSide == cVertSide)
            (aVert, bVert) = (bVert, aVert); // vertex B was on a separate side

        // points of plane intersection with AB and AC
        VertexData abIntersection = GetIntersection(aVert, bVert);
        VertexData acIntersection = GetIntersection(aVert, cVert);

        // 2 separate cases - B&C are positive or negative
        if (aVert.side)
        {
            positiveMesh.AddTriangle(aVert, abIntersection, acIntersection, referenceNormal);
            negativeMesh.AddTriangle(bVert, cVert, acIntersection, referenceNormal);
            negativeMesh.AddTriangle(bVert, acIntersection, abIntersection, referenceNormal);
        }
        else
        {
            negativeMesh.AddTriangle(aVert, abIntersection, acIntersection, referenceNormal);
            positiveMesh.AddTriangle(bVert, cVert, acIntersection, referenceNormal);
            positiveMesh.AddTriangle(bVert, acIntersection, abIntersection, referenceNormal);
        }
    }

    private void CreateMeshObject(Mesh mesh, string objectName, bool applyPhysics, bool applyMeshSlicer)
    {
        // creating a new object, adjusting transform
        GameObject newObject = new GameObject(objectName);
        newObject.transform.SetPositionAndRotation(transform.position, transform.rotation);
        newObject.transform.localScale = transform.localScale;

        // adding new mesh to an object
        MeshFilter meshFilter = newObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = newObject.AddComponent<MeshRenderer>();
        meshFilter.mesh = mesh;
        meshRenderer.material = GetComponent<MeshRenderer>().material;

        if (applyPhysics)
        {
            // creating a collider
            MeshCollider meshCollider = newObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
            meshCollider.convex = true;

            // copying rigidbody from base object
            Rigidbody baseRB = GetComponent<Rigidbody>();
            Rigidbody createdRB = newObject.AddComponent<Rigidbody>();
            createdRB.mass = baseRB.mass;
            createdRB.linearDamping = baseRB.linearDamping;
            createdRB.angularDamping = baseRB.angularDamping;
            createdRB.useGravity = baseRB.useGravity;
            createdRB.isKinematic = baseRB.isKinematic;
            createdRB.interpolation = baseRB.interpolation;
            createdRB.collisionDetectionMode = baseRB.collisionDetectionMode;
            createdRB.constraints = baseRB.constraints;
        }

        if (applyMeshSlicer)
        {
            MeshSlicer newComponent = newObject.AddComponent<MeshSlicer>();
            newComponent.slicePlaneTransform = slicePlaneTransform;
            newObject.AddComponent<KeyboardSliceAction>();
        }
    }
}