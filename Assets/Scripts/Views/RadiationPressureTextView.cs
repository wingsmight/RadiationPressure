using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RadiationPressureTextView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textView;
    [SerializeField] private TMP_InputField cubeWidthInputField;
    [SerializeField] private TMP_InputField cubeHieghtInputField;


    private void Update()
    {
        var pressure = PhotonGenerator.radiatoinForce / ((int.Parse(cubeWidthInputField.text) * int.Parse(cubeHieghtInputField.text)) * RaycastReflectionPhoton.caughtPhtotonCount);

        textView.text = pressure.ToString("E");
    }
}
