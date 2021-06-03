using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RaycastReflection : MonoBehaviour
{
    public int reflectionsCount;
    public float maxLength;

    private LineRenderer lineRenderer;
    private Ray ray;
    private RaycastHit raycastHit;
    private Vector3 direction;


    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }
    private void Update()
    {
        ray = new Ray(transform.position, transform.forward);

        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, transform.position);

        float remainingLength = maxLength;

        for (int i = 0; i < reflectionsCount; i++)
        {
            if (Physics.Raycast(ray.origin, ray.direction, out var hit, remainingLength))
            {
                lineRenderer.positionCount++;
                lineRenderer.SetPosition(lineRenderer.positionCount - 1, hit.point);
                remainingLength -= Vector3.Distance(ray.origin, hit.point);
                ray = new Ray(hit.point, Vector3.Reflect(ray.direction, hit.normal));

                if (hit.collider.tag == "Mirror")
                {
                    break;
                }
            }
            else
            {
                lineRenderer.positionCount++;
                lineRenderer.SetPosition(lineRenderer.positionCount - 1, ray.origin + ray.direction * remainingLength);
            }
        }

        Rotate();
        RotateByKeyboard();
    }

 
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float bound;

    private int rotateDirection = 1;
    private int upDownRotateDirection = 0;

    private void Rotate()
    {
        transform.Rotate(upDownRotateDirection * rotationSpeed * Time.deltaTime, rotateDirection * rotationSpeed * Time.deltaTime, 0);

        float rotationAngleY = transform.localRotation.eulerAngles.y < 180 
            ? transform.localRotation.eulerAngles.y 
            : transform.localRotation.eulerAngles.y - 360;

        if (rotationAngleY > bound)
        {
            rotateDirection = -1;
        }
        else if (rotationAngleY < -bound)
        {
            rotateDirection = 1;
        }
    }
    private void RotateByKeyboard()
    {
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            rotateDirection = -1;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            rotateDirection = 1;
        }
        if (Input.GetKey(KeyCode.UpArrow))
        {
            upDownRotateDirection = -1;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            upDownRotateDirection = 1;
        }
    }
}
