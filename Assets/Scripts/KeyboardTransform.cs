using System;
using UnityEngine;

public class KeyboardTransform : MonoBehaviour
{
    private Vector3 resetPosition;
    private Quaternion resetRotation;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 90f;

    private void Start()
    {
        resetPosition = transform.position;
        resetRotation = transform.rotation;
    }

    void Update()
    {
        Move();
        Rotate();
        CheckNResetTransform();
    }

    /// <summary>
    /// applying movement to the object
    /// A/D - X axis (left/right)
    /// W/S - Z axis (forward/backward)
    /// Q/E - Y axis (up/down)
    /// </summary>
    private void Move()
    {
        Vector3 movement = Vector3.zero;

        // X AXIS
        if (Input.GetKey(KeyCode.A))
            movement.x -= 1f;
        else if (Input.GetKey(KeyCode.D))
            movement.x += 1f;

        // Z AXIS
        if (Input.GetKey(KeyCode.W))
            movement.z += 1f;
        else if (Input.GetKey(KeyCode.S))
            movement.z -= 1f;

        // Y AXIS
        if (Input.GetKey(KeyCode.Q))
            movement.y += 1f;
        else if (Input.GetKey(KeyCode.E))
            movement.y -= 1f;

        // normalization for fluid skew movement
        if (movement.magnitude > 1f)
            movement.Normalize();

        transform.position += movement * moveSpeed * Time.deltaTime;
    }

    /// <summary>
    /// applying rotation to the object
    /// 1/2 - X axis
    /// 3/4 - Z axis
    /// 5/6 - Y axis
    /// </summary>
    private void Rotate()
    {
        Vector3 rotation = Vector3.zero;

        // X ROT
        if (Input.GetKey(KeyCode.Alpha1))
            rotation.x -= 1f;
        else if (Input.GetKey(KeyCode.Alpha2))
            rotation.x += 1f;

        // Y ROT
        if (Input.GetKey(KeyCode.Alpha3))
            rotation.y -= 1f;
        else if (Input.GetKey(KeyCode.Alpha4))
            rotation.y += 1f;

        // Z ROT
        if (Input.GetKey(KeyCode.Alpha5))
            rotation.z -= 1f;
        else if (Input.GetKey(KeyCode.Alpha6))
            rotation.z += 1f;

        transform.Rotate(rotation * rotationSpeed * Time.deltaTime, Space.Self);
    }


    private void CheckNResetTransform()
    {
        // reseting position
        if (Input.GetKey(KeyCode.LeftBracket))
            transform.position = resetPosition;

        // resetting rotation
        if (Input.GetKey(KeyCode.RightBracket))
            transform.rotation = resetRotation;
    }
}