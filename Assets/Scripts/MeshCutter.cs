using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MeshCutter
{
    private Mesh objectMesh;
    private MeshBuilder positiveMesh, negativeMesh;

    private Plane slicePlane;
    private bool? sideToKeep;

    private List<VertexData> pointsAlongCut, sortedPointsAlongCut;

    public MeshCutter(Mesh mesh, Plane plane, Vector3? pointToKeep = null)
    {
        objectMesh = mesh;
        slicePlane = plane;

        if (pointToKeep.HasValue)
            sideToKeep = slicePlane.GetSide(pointToKeep.Value);
        else
            sideToKeep = null;
    }

    public void SetSlicePlane(Plane newPlane)
    {
        slicePlane = newPlane;
    }

    public void SetObjectMesh(Mesh newMesh)
    {
        objectMesh = newMesh;
    }

    public void SetPointToKeep(Vector3? pointToKeep)
    {
        if (pointToKeep.HasValue)
            sideToKeep = slicePlane.GetSide(pointToKeep.Value);
        else
            sideToKeep = null;
    }

    public (Mesh positiveMesh, Mesh negativeMesh) Cut()
    {
        // vertices and triangles in a base mesh
        Vector3[] vertices = objectMesh.vertices;
        Vector3[] normals = objectMesh.normals;
        Vector2[] uvs = objectMesh.uv;

        // list for collecting vertices on slice plane
        pointsAlongCut = new List<VertexData>();

        // 2 new meshes to divide triangles
        positiveMesh = sideToKeep == false ? null : new MeshBuilder();
        negativeMesh = sideToKeep == true ? null : new MeshBuilder();

        // iterating over all submeshes
        for (int subMesh = 0; subMesh < objectMesh.subMeshCount; subMesh++)
        {
            // triangles belonging to current material
            int[] triangles = objectMesh.GetTriangles(subMesh);

            // submesh 1 contains triangles created by a cut
            bool isCutMaterial = subMesh == 1;

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
                {
                    if (sideToKeep != false)
                        positiveMesh.AddTriangle(aVert, bVert, cVert, isCutMaterial ? 1 : 0);
                }
                else if (!aVert.side && !bVert.side && !cVert.side)     // entire triangle on side 0: ---
                {
                    if (sideToKeep != true)
                        negativeMesh.AddTriangle(aVert, bVert, cVert, isCutMaterial ? 1 : 0);
                }
                else                                                    // triangle cut in half by a plane: ++/- or +/--
                    SliceTriangle(aVert, bVert, cVert, isCutMaterial);
            }
        }

        if (positiveMesh != null)
            TriangulateCut(positiveMesh);

        if (negativeMesh != null)
            TriangulateCut(negativeMesh);

        return (positiveMesh?.Build(), negativeMesh?.Build());
    }

    /// <summary>
    /// method finding and interpolating uv/normal of an intersection of plane and section between 2 vertices
    /// </summary>
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

    private void SliceTriangle(VertexData aVert, VertexData bVert, VertexData cVert, bool isCutMaterial)
    {
        // remembering what side the normal was facing in original triangle
        Vector3 referenceNormal = Vector3.Cross(bVert.position - aVert.position, cVert.position - aVert.position);

        // defining what side of a cut are verts in
        bool aVertSide = aVert.side;
        bool bVertSide = bVert.side;
        bool cVertSide = cVert.side;

        // material index of triangles created from the original triangle
        int materialIndex = isCutMaterial ? 1 : 0;

        // Rearrange vertices so A is always the single vertex on one side ( A | BC )
        if (aVertSide == bVertSide)
            (aVert, cVert) = (cVert, aVert); // vertex C was on a separate side
        else if (aVertSide == cVertSide)
            (aVert, bVert) = (bVert, aVert); // vertex B was on a separate side

        // points of plane intersection with AB and AC
        VertexData abIntersection = GetIntersection(aVert, bVert);
        VertexData acIntersection = GetIntersection(aVert, cVert);

        // adding points to a collection for triangulation
        // side does not matter - 2 new meshes have the same cut hole
        pointsAlongCut.Add(abIntersection);
        pointsAlongCut.Add(acIntersection);

        // 2 separate cases
        if (aVert.side) // A positive, B&C negative
        {
            if (sideToKeep != false)
                positiveMesh.AddTriangle(aVert, abIntersection, acIntersection, referenceNormal, materialIndex);

            if (sideToKeep != true)
            {
                negativeMesh.AddTriangle(bVert, cVert, acIntersection, referenceNormal, materialIndex);
                negativeMesh.AddTriangle(bVert, acIntersection, abIntersection, referenceNormal, materialIndex);
            }
        }
        else // B&C positive, A negative
        {
            if (sideToKeep != true)
                negativeMesh.AddTriangle(aVert, abIntersection, acIntersection, referenceNormal, materialIndex);

            if (sideToKeep != false)
            {
                positiveMesh.AddTriangle(bVert, cVert, acIntersection, referenceNormal, materialIndex);
                positiveMesh.AddTriangle(bVert, acIntersection, abIntersection, referenceNormal, materialIndex);
            }
        }
    }

    /// <summary>
    /// Triangulation by ear clipping
    /// </summary>
    private void TriangulateCut(MeshBuilder mesh)
    {
        sortedPointsAlongCut = SortPointsAlongCut();
        bool isSortedClockwise = IsCutWindingClockwise();

        List<VertexData[]> ears = new List<VertexData[]>();

        // removing one ear at a time until only the final triangle remains
        while (sortedPointsAlongCut.Count > 3)
        {
            bool earFound = false;

            for (int i = 0; i < sortedPointsAlongCut.Count; i++)
            {
                // getting previous, current and next vertex of the candidate ear
                Vector2 previous = ProjectToSlicePlane(sortedPointsAlongCut[(i - 1 + sortedPointsAlongCut.Count) % sortedPointsAlongCut.Count].position);
                Vector2 current = ProjectToSlicePlane(sortedPointsAlongCut[i].position);
                Vector2 next = ProjectToSlicePlane(sortedPointsAlongCut[(i + 1) % sortedPointsAlongCut.Count].position);

                // checking the direction of the turn at the current vertex
                Vector2 vectA = current - previous;
                Vector2 vectB = next - current;
                float vectorCross = vectA.x * vectB.y - vectA.y * vectB.x;

                // middle point of the ear must be convex in relation to the polygon
                if ((isSortedClockwise && vectorCross < 0) || (!isSortedClockwise && vectorCross > 0))
                {
                    bool vertexInsideDetected = false;

                    // checking if any other polygon vertex is inside the candidate triangle
                    for (int j = 0; j < sortedPointsAlongCut.Count; j++)
                    {
                        if (j == i || j == (i - 1 + sortedPointsAlongCut.Count) % sortedPointsAlongCut.Count || j == (i + 1) % sortedPointsAlongCut.Count)
                            continue;

                        Vector2 point = ProjectToSlicePlane(sortedPointsAlongCut[j].position);

                        if (MeshOperations.IsPointInsideTriangle2D(previous, current, next, point))
                        {
                            vertexInsideDetected = true;
                            break;
                        }
                    }

                    // valid ear found - save it and remove its middle vertex
                    if (!vertexInsideDetected)
                    {
                        VertexData previousVertex = sortedPointsAlongCut[(i - 1 + sortedPointsAlongCut.Count) % sortedPointsAlongCut.Count];
                        VertexData currentVertex = sortedPointsAlongCut[i];
                        VertexData nextVertex = sortedPointsAlongCut[(i + 1) % sortedPointsAlongCut.Count];

                        ears.Add(new VertexData[] { previousVertex, currentVertex, nextVertex });

                        sortedPointsAlongCut.RemoveAt(i);
                        earFound = true;
                        break;
                    }
                }
            }

            if (!earFound)
                break;
        }

        // the last three vertices form the final triangle
        if (sortedPointsAlongCut.Count == 3)
        {
            ears.Add(new VertexData[]
            {
            sortedPointsAlongCut[0],
            sortedPointsAlongCut[1],
            sortedPointsAlongCut[2]
            });
        }

        Debug.Log("Ears: " + ears.Count);

        AddCutTriangles(ears);
    }

    /// <summary>
    /// Sorts points of a cut so a geometric figure is formed,
    /// collection pointsAlongCut contains pairs of vertices (segments)
    /// </summary>
    private List<VertexData> SortPointsAlongCut()
    {
        // sorted points along cut
        List<VertexData> sortedPoints = new List<VertexData>();

        // copy of pointsAlongCut collection
        List<VertexData> unsortedPoints = new List<VertexData>(pointsAlongCut);

        // starting with the first segment
        sortedPoints.Add(unsortedPoints[0]);
        sortedPoints.Add(unsortedPoints[1]);
        unsortedPoints.RemoveRange(0, 2);

        while (unsortedPoints.Count > 0)
        {
            bool found = false;

            // comparing last sorted point and current unsorted
            for (int i = 0; i < unsortedPoints.Count; i += 2)
            {
                // comparing by pairs
                int first = i;
                int second = i + 1;

                if (MeshOperations.IsSamePoint3D(sortedPoints.Last().position, unsortedPoints[first].position))
                    sortedPoints.Add(unsortedPoints[second]);
                else if (MeshOperations.IsSamePoint3D(sortedPoints.Last().position, unsortedPoints[second].position))
                    sortedPoints.Add(unsortedPoints[first]);
                else
                    continue;

                unsortedPoints.RemoveRange(first, 2);
                found = true;
                break;
            }

            if (!found)
                break;
        }

        // removing last duplicated point
        sortedPoints.Remove(sortedPoints.Last());

        Debug.Log("Points along cut: " + sortedPoints.Count);
        return sortedPoints;
    }

    /// <summary>
    /// calculating signed area of the cut polygon,
    /// negative = clockwise winding, positive = counter clockwise winding
    /// </summary>
    private bool IsCutWindingClockwise()
    {
        float winding = 0f;

        // iterating over all polygon edges
        for (int i = 0; i < sortedPointsAlongCut.Count; i++)
        {
            // converting points from 3D to 2D
            Vector2 current = ProjectToSlicePlane(sortedPointsAlongCut[i].position);
            Vector2 next = ProjectToSlicePlane(sortedPointsAlongCut[(i + 1) % sortedPointsAlongCut.Count].position);

            // adding the contribution of the current edge
            winding += current.x * next.y - next.x * current.y;
        }

        // negative signed area == clockwise winding
        return winding < 0f;
    }

    /// <summary>
    /// Transforms 3D point to 2D coordinates on the slice plane
    /// </summary>
    private Vector2 ProjectToSlicePlane(Vector3 point)
    {
        // finding an axis lying on the cut plane
        Vector3 right = Vector3.Cross(slicePlane.normal, Vector3.up);

        // using another axis if normal is parallel to up
        if (right.sqrMagnitude < 0.000001f)
            right = Vector3.Cross(slicePlane.normal, Vector3.right);

        // normalizing the first plane axis
        right.Normalize();

        // finding the second axis lying on the cut plane
        Vector3 up = Vector3.Cross(right, slicePlane.normal).normalized;

        // converting 3D point to 2D plane coordinates
        return new Vector2(Vector3.Dot(point, right), Vector3.Dot(point, up));
    }

    /// <summary>
    /// creates new uv and normal data for triangles on a cut - based on the relation with a slice plane
    /// </summary>
    private void AddCutTriangles(List<VertexData[]> ears)
    {
        Vector3 cutNormal = slicePlane.normal;

        foreach (VertexData[] ear in ears)
        {
            // Positive side of the cut.
            VertexData aPositive = CreateCutVertex(ear[0], -cutNormal);
            VertexData bPositive = CreateCutVertex(ear[1], -cutNormal);
            VertexData cPositive = CreateCutVertex(ear[2], -cutNormal);

            // Negative side of the cut.
            VertexData aNegative = CreateCutVertex(ear[0], cutNormal);
            VertexData bNegative = CreateCutVertex(ear[1], cutNormal);
            VertexData cNegative = CreateCutVertex(ear[2], cutNormal);

            // The reference normal determines the required triangle winding.
            // MeshBuilder automatically swaps B/C when necessary.
            if (sideToKeep != false)
                positiveMesh.AddCutTriangle(aPositive, bPositive, cPositive, -cutNormal);

            if (sideToKeep != true)
                negativeMesh.AddCutTriangle(aNegative, bNegative, cNegative, cutNormal);
        }
    }

    /// <summary>
    /// creating new vertexdata for trianle vertices on a cut
    /// </summary>
    private VertexData CreateCutVertex(VertexData vertex, Vector3 normal)
    {
        Vector2 uv = ProjectCutUV(vertex.position);
        return new VertexData(vertex.position, normal, uv, slicePlane.GetSide(vertex.position));
    }

    /// <summary>
    /// creating uv for a cut triangle vertices
    /// </summary>
    private Vector2 ProjectCutUV(Vector3 position)
    {
        Vector3 right = Vector3.Cross(slicePlane.normal, Vector3.up);

        if (right.sqrMagnitude < 0.000001f)
            right = Vector3.Cross(slicePlane.normal, Vector3.right);

        right.Normalize();

        Vector3 up = Vector3.Cross(right, slicePlane.normal).normalized;

        return new Vector2(Vector3.Dot(position, right), Vector3.Dot(position, up));
    }
}