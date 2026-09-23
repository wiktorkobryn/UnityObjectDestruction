using System.Collections.Generic;
using UnityEngine;

public static class MeshOperations
{
    /// <summary>
    /// Method for generating voronoi seeds inside given mesh bound by mesh collider
    /// </summary>
    public static List<Vector3> GenerateVoronoiSeeds(MeshCollider collider, int amountOfPoints)
    {
        List<Vector3> seeds = new List<Vector3>();
        Bounds meshColliderBounds = collider.bounds;

        while (seeds.Count < amountOfPoints)
        {
            Vector3 pointCandidate = new Vector3(
                Random.Range(meshColliderBounds.min.x, meshColliderBounds.max.x),
                Random.Range(meshColliderBounds.min.y, meshColliderBounds.max.y),
                Random.Range(meshColliderBounds.min.z, meshColliderBounds.max.z)
                );

            if (IsPointInsideMesh(collider, pointCandidate))
            {
                seeds.Add(pointCandidate);
                Debug.Log("point added");
            }
        }

        return seeds;
    }

    /// <summary>
    /// Checks whether seed point candidate lies in given mesh bounds 
    /// </summary>
    public static bool IsPointInsideMesh(MeshCollider collider, Vector3 point)
    {
        // raycast from the outside of the mesh collider to the point
        // because raycast does not detect a collider if it's origin is inside it
        float margin = collider.bounds.size.x;
        Ray ray = new Ray(new Vector3(collider.bounds.min.x - margin, point.y, point.z), Vector3.right);
        RaycastHit[] hits = Physics.RaycastAll(ray, point.x - (collider.bounds.min.x - margin));

        int intersections = 0;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == collider)
                intersections++;
        }

        // odd intersections == point is inside mesh
        if (intersections % 2 == 1)
            return true;
        else
            return false;
    }

    public static bool IsSamePoint3D(Vector3 a, Vector3 b)
    {
        return Vector3.SqrMagnitude(a - b) < 0.00001f;
    }

    /// <summary>
    /// checking if point lies in triangle by comparing fragmentary areas
    /// </summary>
    public static bool IsPointInsideTriangle2D(Vector2 triA, Vector2 triB, Vector2 triC, Vector2 point)
    {
        // comparison error
        float epsilonErr = 0.0001f;

        float fullArea = CalculateTriangleArea(triA, triB, triC);

        float areaABP = CalculateTriangleArea(triA, triB, point);
        float areaACP = CalculateTriangleArea(triA, triC, point);
        float areaBCP = CalculateTriangleArea(triB, triC, point);
        float fragmentaryAreasSum = areaABP + areaACP + areaBCP;

        return Mathf.Abs(fullArea - fragmentaryAreasSum) < epsilonErr;
    }

    /// <summary>
    /// calculating triangle area with given 3 points,
    /// formula: A = | Ax(By - Cy) + Bx(Cy - Ay) + Cx(Ay-By) | / 2
    /// </summary>
    public static float CalculateTriangleArea(Vector2 triA, Vector2 triB, Vector2 triC)
    {
        return Mathf.Abs(triA.x * (triB.y - triC.y) + triB.x * (triC.y - triA.y) + triC.x * (triA.y - triB.y)) / 2.0f;
    }

    public static Plane GetPlaneBetweenPoints(Vector3 pointA, Vector3 pointB)
    {
        // middle point and normal vector is needed to create a plane between 2 points
        // xn = (xa + xb) / 2; yn = (ya + yb) / 2
        Vector3 middlePoint = (pointB + pointA) / 2.0f;
        Vector3 normal = (pointB - pointA).normalized; // direction from B to A - says which side of the plane is positive
        return new Plane(normal, middlePoint);
    }
}
