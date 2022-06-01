using UnityEngine;

public class RotatableObject : MonoBehaviour
{
    [SerializeField] private Vector2 rotationVelocity = new Vector2(50.0f, 50.0f);


    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            var mouseMovement = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            var rotation = Vector3.Scale(mouseMovement, rotationVelocity * Mathf.Deg2Rad);

            transform.Rotate(Vector3.up, -rotation.x, Space.World);
            transform.Rotate(Vector3.right, rotation.y, Space.World);
        }
    }
}