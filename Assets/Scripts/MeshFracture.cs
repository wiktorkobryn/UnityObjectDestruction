using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class MeshFracture : MonoBehaviour
{
    private MeshCollider colliderVoronoiBounds;
    private List<Vector3> voronoiSeeds;

    private void Start()
    {
        colliderVoronoiBounds = GetComponent<MeshCollider>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
            FractureMeshVoronoi();
    }

    public void FractureMeshVoronoi()
    {
        // generating vornoi seeds
        voronoiSeeds = MeshOperations.GenerateVoronoiSeeds(colliderVoronoiBounds, 3);

    }

    /// <summary>
    /// visual debug - randomized seeds
    /// </summary>
    private void OnDrawGizmos()
    {
        if (voronoiSeeds == null)
            return;

        Gizmos.color = Color.red;

        foreach (Vector3 point in voronoiSeeds)
        {
            Gizmos.DrawSphere(point, 0.01f);
        }
    }
}
