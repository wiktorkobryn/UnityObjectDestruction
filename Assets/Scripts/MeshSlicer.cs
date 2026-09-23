using UnityEngine;

public class MeshSlicer : MonoBehaviour
{
    private Mesh objectMesh;

    public Transform slicePlaneTransform;
    public Material cutMaterial;

    private void Start()
    {
        objectMesh = GetComponent<MeshFilter>().mesh;
    }

    private void GetSlicePlaneData(out Vector3 localPlanePosition, out Vector3 localPlaneNormal)
    {
        // transforming plane to mesh local space
        localPlanePosition = transform.InverseTransformPoint(slicePlaneTransform.position);
        localPlaneNormal = transform.InverseTransformDirection(slicePlaneTransform.up).normalized;
    }

    /// <summary>
    /// slices a mesh in half, produces 2 independent submeshes with cutMaterial on a newly created trianglesz
    /// autodestroys the game object
    /// </summary>
    public void SliceMesh()
    {
        GetSlicePlaneData(out Vector3 localPlanePosition, out Vector3 localPlaneNormal);

        Plane slicePlane = new Plane(localPlaneNormal, localPlanePosition);

        Vector3? pointToKeep = null;

        MeshCutter meshCutter = new MeshCutter(objectMesh, slicePlane, pointToKeep);
        (Mesh positiveMesh, Mesh negativeMesh) = meshCutter.Cut();

        CreateMeshObject(positiveMesh, name + "SlicePositive", true, true);
        CreateMeshObject(negativeMesh, name + "SliceNegative", true, true);

        Destroy(gameObject);
    }

    /// <summary>
    /// creates gameobject for sliced parts, adds components
    /// </summary>
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

        if (applyMeshSlicer)
        {
            MeshSlicer newComponent = newObject.AddComponent<MeshSlicer>();
            newComponent.slicePlaneTransform = slicePlaneTransform;
            newComponent.cutMaterial = cutMaterial;
            newObject.AddComponent<KeyboardSliceAction>();
        }
    }
}