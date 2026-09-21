using System.Collections.Generic;
using UnityEngine;

public class KeyboardSliceAction : MonoBehaviour
{
    [SerializeField]
    private List<MeshSlicer> detectedMeshes = new List<MeshSlicer>();

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            foreach(MeshSlicer mesh in detectedMeshes)
            {
                if (mesh != null)
                    mesh.SliceMesh();
            }

            detectedMeshes.RemoveAll(mesh => mesh == null);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        MeshSlicer possiblySlicableObject = other.GetComponent<MeshSlicer>();

        if (possiblySlicableObject != null)
            detectedMeshes.Add(possiblySlicableObject);
    }

    private void OnTriggerExit(Collider other)
    {
        MeshSlicer possiblySlicableObject = other.GetComponent<MeshSlicer>();
        detectedMeshes.Remove(possiblySlicableObject);
    }
}
