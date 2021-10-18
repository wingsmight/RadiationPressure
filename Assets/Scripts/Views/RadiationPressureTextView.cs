using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RadiationPressureTextView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textView;
    [SerializeField] private TMP_InputField cubeWidthInputField;
    [SerializeField] private TMP_InputField cubeHieghtInputField;
    [SerializeField] private SatelliteArea satelliteArea;


    private float overallArea;


    private void FixedUpdate()
    {
        var pressure = PhotonGenerator.radiatoinForce / (satelliteArea.OverallArea * RaycastReflectionPhoton.caughtPhtotonCount);

        textView.text = pressure.ToString("E");
    }
}
