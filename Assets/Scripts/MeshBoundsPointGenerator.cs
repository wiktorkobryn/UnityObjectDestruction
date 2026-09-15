using System.Collections.Generic;
using UnityEngine;

public static class MeshBoundsPointGenerator
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
    private static bool IsPointInsideMesh(MeshCollider collider, Vector3 point)
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
}
