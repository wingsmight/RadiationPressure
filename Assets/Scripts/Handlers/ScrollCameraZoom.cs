using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ScrollCameraZoom : MonoBehaviour
{
    [SerializeField] private float speed = 10.0f;


    private new Camera camera;


    private void Awake()
    {
        camera = GetComponent<Camera>();
    }
    private void Update()
    {
        if (camera.orthographic)
        {
            camera.orthographicSize -= Input.GetAxis("Mouse ScrollWheel") * speed;
        }
        else
        {
            camera.fieldOfView -= Input.GetAxis("Mouse ScrollWheel") * speed;
        }
    }
}
