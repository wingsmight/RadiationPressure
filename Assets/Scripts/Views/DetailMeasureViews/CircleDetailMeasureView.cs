using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CircleDetailMeasureView : DetailMeasureView
{
    [SerializeField] private TMP_InputField radiusInputField;


    private void Awake()
    {
        radiusInputField.onValueChanged.AddListener((newValue) =>
        {
            raycastSelection.CurrentSelection.GetComponent<CircleDetail>().Radius = float.Parse(newValue);
        });
    }
    protected override Type DetailType => typeof(CircleDetail);
}
