using UnityEngine;

public class KeyboardSliceAction : MonoBehaviour
{
    private MeshSlicer meshSlicer;

    private void Start()
    {
        meshSlicer = GetComponent<MeshSlicer>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
            meshSlicer.SliceMesh();
    }
}
