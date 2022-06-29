using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RadiationPressureTextView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textView;


    private float overallArea;


    private void FixedUpdate()
    {
        var pressure = PhotonGenerator.radiatoinForce / RaycastReflectionPhoton.caughtPhotonCount;

        textView.text = pressure.ToString("E");
    }
}
