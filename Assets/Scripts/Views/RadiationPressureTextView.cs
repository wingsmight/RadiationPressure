using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RadiationPressureTextView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textView;
    [SerializeField] private OffsetObjectPooler photonPooler;


    private void Update()
    {
        textView.text = new Vector3(PhotonGenerator.radiatoinForce.x / photonPooler.BetweenOffset.x,
            PhotonGenerator.radiatoinForce.y / photonPooler.BetweenOffset.y,
            PhotonGenerator.radiatoinForce.z / photonPooler.BetweenOffset.z).ToString("F4");
    }
}
