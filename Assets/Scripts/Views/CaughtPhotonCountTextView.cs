using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CaughtPhotonCountTextView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textView;


    private void Update()
    {
        textView.text = RaycastReflectionPhoton.caughtPhtotonCount.ToString();
    }
}
