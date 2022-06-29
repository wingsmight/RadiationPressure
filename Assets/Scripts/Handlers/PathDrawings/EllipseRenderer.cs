using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EllipseRenderer : MonoBehaviour, IDrawable
{
    [SerializeField] private LineRenderer lineRenderer;
    [Space]
    [Range(3, 36)][SerializeField] private int segments = 36;
    [SerializeField] private EllipseOrbit ellipseOrbit;


    public void Draw()
    {
        Vector3[] points = new Vector3[segments + 1];
        for (int i = 0; i < segments; i++)
        {
            Vector2 position2D = ellipseOrbit.Path.Evaluate((float)i / (float)segments);
            points[i] = new Vector3(position2D.x, position2D.y, 0.0f);
        }
        points[segments] = points[0];
        lineRenderer.positionCount = segments + 1;
        lineRenderer.SetPositions(points);
    }
}
