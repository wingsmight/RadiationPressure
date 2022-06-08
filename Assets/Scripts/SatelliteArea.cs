using UnityEngine;

public class SatelliteArea : MonoBehaviour
{
    private Detail[] details = new Detail[0];


    private void Awake()
    {
        SetDetails();
    }


    private void SetDetails()
    {
        details = UnityEngine.Object.FindObjectsOfType<Detail>();
    }


    public float OverallArea
    {
        get
        {
            var overallArea = 0.0f;

            foreach (var detail in details)
            {
                overallArea += detail.Area;
            }

            return overallArea;
        }
    }
}
