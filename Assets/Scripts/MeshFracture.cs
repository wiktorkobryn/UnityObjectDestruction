using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class MeshFracture : MonoBehaviour
{
    private MeshCollider colliderVoronoiBounds;
    private List<Vector3> voronoiSeeds;
    public Material cutMaterial;
    public int cellAmountMin = 2, cellAmountMax = 10;
    public bool randomizeSeedCount = true;

    private void Start()
    {
        colliderVoronoiBounds = GetComponent<MeshCollider>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
            FractureMeshVoronoi();
    }

    private int RandomizeSeedCount()
    {
        if (cellAmountMin <= cellAmountMax && cellAmountMin > 1 && cellAmountMax > 1)
            if (randomizeSeedCount)
                return Random.Range(cellAmountMin, cellAmountMax);
            else
                return cellAmountMin;
        else
            return -1;
    }

    public void FractureMeshVoronoi()
    {
        // generating vornoi seeds
        int seedCount = RandomizeSeedCount();
        
        if (seedCount < 1)
            return;

        voronoiSeeds = GenerateVoronoiSeeds(colliderVoronoiBounds, seedCount);
        List<Mesh> cellsMeshes = new List<Mesh>();

        // separating and creating cells
        for(int i = 0; i < voronoiSeeds.Count; i++)
        {
            Mesh singleCellMesh = SeparateCellVoronoi(voronoiSeeds[i]);

            if (singleCellMesh != null)
                cellsMeshes.Add(singleCellMesh);
            else
                Debug.Log("Empty voronoi cell");
        }

        if( cellsMeshes.Count > 0 )
        {
            // create gameobjects for voronoi cells
            for (int i = 0; i < cellsMeshes.Count; i++)
                CreateMeshObject(cellsMeshes[i], name + "_Cell" + i, true);

            Debug.Log("Created " + cellsMeshes.Count + " voronoi cells");

            // destroy fractured gameobject
            Destroy(gameObject);
        }
    }

    private Mesh SeparateCellVoronoi(Vector3 seed)
    {
        Mesh cellMesh = GetComponent<MeshFilter>().mesh;

        // converting seed from world space to mesh local space
        Vector3 localSeed = transform.InverseTransformPoint(seed);

        MeshCutter meshCutter = new MeshCutter(cellMesh, new Plane(), localSeed);

        // snipping 3d area of a cell - 'seed' can take only space closer to it than other seeds
        for (int i = 0; i < voronoiSeeds.Count; i++)
        {
            // cannot cut cell by self
            if ((voronoiSeeds[i] - seed).sqrMagnitude < 0.0001f)
                continue;

            // converting other seed from world space to mesh local space
            Vector3 localOtherSeed = transform.InverseTransformPoint(voronoiSeeds[i]);

            // dividing area between 2 cells / 2 seeds
            Plane slicePlane = MeshOperations.GetPlaneBetweenPoints(localSeed, localOtherSeed);
            meshCutter.SetSlicePlane(slicePlane);

            (Mesh positiveMesh, Mesh negativeMesh) = meshCutter.Cut();

            cellMesh = positiveMesh != null ? positiveMesh : negativeMesh;

            if (cellMesh == null)
               continue;

            meshCutter.SetObjectMesh(cellMesh);
        }

        return cellMesh;
    }

    /// <summary>
    /// generating voronoi seeds inside given mesh bound by mesh collider
    /// </summary>
    private List<Vector3> GenerateVoronoiSeeds(MeshCollider collider, int amountOfPoints)
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

            if (MeshOperations.IsPointInsideMesh(collider, pointCandidate))
            {
                seeds.Add(pointCandidate);
                Debug.Log("point added");
            }
        }

        return seeds;
    }

    /// <summary>
    /// creates gameobject for a single fractured part
    /// </summary>
    private void CreateMeshObject(Mesh mesh, string objectName, bool applyPhysics)
    {
        // creating a new object, adjusting transform
        GameObject newObject = new GameObject(objectName);
        newObject.transform.SetPositionAndRotation(transform.position, transform.rotation);
        newObject.transform.localScale = transform.localScale;

        // adding new mesh to an object
        MeshFilter meshFilter = newObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = newObject.AddComponent<MeshRenderer>();
        meshFilter.mesh = mesh;

        Material originalMaterial = GetComponent<MeshRenderer>().sharedMaterials[0];

        meshRenderer.materials = new Material[]
        {
            originalMaterial,
            cutMaterial
        };

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
