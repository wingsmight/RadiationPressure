using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CircleDetailMeasureView : SelectionSettingsView
{
    [SerializeField] private TMP_InputField diameterInputField;


    private void Awake()
    {
        diameterInputField.onValueChanged.AddListener((newValue) =>
        {
            raycastSelection.CurrentSelection.GetComponent<CircleDetail>().Diameter = float.Parse(newValue);
        });
    }
    private void OnDestroy()
    {
        diameterInputField.onValueChanged.RemoveAllListeners();
    }
    protected override Type ShowOnType => typeof(CircleDetail);
}
