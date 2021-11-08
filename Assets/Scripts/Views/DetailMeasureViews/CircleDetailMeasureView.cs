using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CircleDetailMeasureView : SelectionSettingsView
{
    [SerializeField] private TMP_InputField radiusInputField;


    private void Awake()
    {
        radiusInputField.onValueChanged.AddListener((newValue) =>
        {
            raycastSelection.CurrentSelection.GetComponent<CircleDetail>().Radius = float.Parse(newValue);
        });
    }
    private void OnDestroy()
    {
        radiusInputField.onValueChanged.RemoveAllListeners();
    }
    protected override Type ShowOnType => typeof(CircleDetail);
}
