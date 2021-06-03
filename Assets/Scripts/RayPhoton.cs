using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayPhoton : MonoBehaviour
{
    [SerializeField] private float distance;


    public void Move()
    {
        
    }

    private void Update()
    {
        Draw();
    }

    private void Draw()
    {
        // Vector3 forward = transform.TransformDirection(Vector3.forward) * distance;
        // Debug.DrawRay(transform.position, forward, Color.green);

        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Debug.DrawLine (ray.origin, Camera.main.transform.forward * distance, Color.red);
    }
}
