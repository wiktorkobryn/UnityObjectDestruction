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
        voronoiSeeds = MeshBoundsPointGenerator.GenerateVoronoiSeeds(colliderVoronoiBounds, 100);
    }

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
