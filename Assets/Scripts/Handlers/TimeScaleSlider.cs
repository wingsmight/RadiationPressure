using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimeScaleSlider : MonoBehaviour
{
    private const float MIN_VALUE = 0.0f;
    private const float MAX_VALUE = 1.0f;


    [SerializeField] private Slider slider;


    private void Start()
    {
        slider.maxValue = MIN_VALUE;
        slider.maxValue = MAX_VALUE;

        slider.value = Time.timeScale;
    }
    private void Update()
    {
        Time.timeScale = slider.value;
    }
}
