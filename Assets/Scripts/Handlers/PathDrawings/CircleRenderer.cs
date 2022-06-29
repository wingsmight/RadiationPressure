using UnityEngine;

public class CircleRenderer : MonoBehaviour, IDrawable
{
    // Объект отрисовки
    [SerializeField] private LineRenderer circleRenderer;
    [SerializeField] private CircleOrbit circleOrbit;


    /// <summary>
    /// Draw circle at the center of transform.
    /// </summary>
    /// <param name="radius">the radius of the circle.</param>
    /// <param name="stepCount">the count of lines drawing the circle. More counts mean more smoothly result.</param>
    public void Draw()
    {
        var circle = circleOrbit.Path;

        float radius = circle.Radius;
        int stepCount = circle.StepCount;

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
