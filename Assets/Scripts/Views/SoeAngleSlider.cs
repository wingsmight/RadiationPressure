using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SoeAngleSlider : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private SunObjectEarthSystem sunObjectEarthSystem;
    [SerializeField] private TextMeshProUGUI valueTextView;


    private void Awake()
    {
        slider.onValueChanged.AddListener(SetAngle);
    }
    private void Start()
    {
        SetAngle(slider.value);
    }
    private void OnDestroy()
    {
        slider.onValueChanged.RemoveListener(SetAngle);
    }


    private void SetAngle(float angle)
    {
        sunObjectEarthSystem.PlaceSatellite(angle);
        valueTextView.text = (Mathf.Rad2Deg * angle).ToString() + '°';
    }
}
