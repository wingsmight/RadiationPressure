using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RectangleDetailMeasureView : SelectionSettingsView
{
    [SerializeField] private TMP_InputField widthInputField;
    [SerializeField] private TMP_InputField heightInputField;


    private void Awake()
    {
        widthInputField.onValueChanged.AddListener((newValue) =>
        {
            raycastSelection.CurrentSelection.GetComponent<RectangleDetail>().Width = float.Parse(newValue);
        });
        heightInputField.onValueChanged.AddListener((newValue) =>
        {
            raycastSelection.CurrentSelection.GetComponent<RectangleDetail>().Height = float.Parse(newValue);
        });
    }
    private void OnDestroy()
    {
        widthInputField.onValueChanged.RemoveAllListeners();
        heightInputField.onValueChanged.RemoveAllListeners();
    }
    protected override Type ShowOnType => typeof(RectangleDetail);
}
