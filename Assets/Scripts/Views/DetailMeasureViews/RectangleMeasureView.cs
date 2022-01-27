using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RectangleMeasureView : SelectionSettingsView
{
    [SerializeField] private TMP_InputField widthInputField;
    [SerializeField] private TMP_InputField heightInputField;


    private void Awake()
    {
        fadeAnimation.OnActiveChanged += (isActive) =>
        {
            if (isActive)
            {
                widthInputField.text = raycastSelection.CurrentSelection.GetComponent<RectangleDetail>().Width.ToString();
                heightInputField.text = raycastSelection.CurrentSelection.GetComponent<RectangleDetail>().Height.ToString();
            }
        };
        widthInputField.onValueChanged.AddListener((newValue) =>
        {
            if (float.TryParse(newValue, out var width))
            {
                raycastSelection.CurrentSelection.GetComponent<RectangleDetail>().Width = width;
            }
        });
        heightInputField.onValueChanged.AddListener((newValue) =>
        {
            if (float.TryParse(newValue, out var height))
            {
                raycastSelection.CurrentSelection.GetComponent<RectangleDetail>().Height = height;
            }
        });
    }
    private void OnDestroy()
    {
        widthInputField.onValueChanged.RemoveAllListeners();
        heightInputField.onValueChanged.RemoveAllListeners();
    }


    protected override Type ShowOnType => typeof(RectangleDetail);
}
