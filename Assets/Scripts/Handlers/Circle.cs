using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Circle : MonoBehaviour
{
    [SerializeField] private LineRenderer circleRenderer;


    public void Draw(float radius, int stepCount)
    {
        circleRenderer.positionCount = stepCount + 1;
        for (int currentStep = 0; currentStep < stepCount; currentStep++)
        {
            float circumferenceProgress = (float)currentStep / stepCount;
            float currentRadian = circumferenceProgress * 2 * Mathf.PI;
            float xScaled = Mathf.Cos(currentRadian);
            float yScaled = Mathf.Sin(currentRadian);
            float x = xScaled * radius; float y = yScaled * radius;
            Vector3 currentPosition = new Vector3(x, y, 0);
            circleRenderer.SetPosition(currentStep, currentPosition);
        }
        circleRenderer.SetPosition(stepCount, circleRenderer.GetPosition(0));
    }
}
