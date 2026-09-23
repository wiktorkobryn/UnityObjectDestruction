using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class MeshFracture : MonoBehaviour
{
    private MeshCollider colliderVoronoiBounds;
    private List<Vector3> voronoiSeeds;
    public Material cutMaterial;

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
        voronoiSeeds = MeshOperations.GenerateVoronoiSeeds(colliderVoronoiBounds, 2);

        // test for the first cell
        Mesh singleCellMesh = SeparateCellVoronoi(voronoiSeeds[0]);

        if (singleCellMesh == null)
            return;

        CreateMeshObject(singleCellMesh, name + "_Cell0", true);
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
                return null;

            meshCutter.SetObjectMesh(cellMesh);
        }

        return cellMesh;
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
}
