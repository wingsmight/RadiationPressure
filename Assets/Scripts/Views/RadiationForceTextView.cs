using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RadiationForceTextView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textView;


    private void Update()
    {
        textView.text = PhotonGenerator.radiatoinForce.ToString("F10");
    }
}
