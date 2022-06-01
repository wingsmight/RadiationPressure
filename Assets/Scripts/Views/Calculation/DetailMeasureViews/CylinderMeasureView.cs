using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CylinderMeasureView : SelectionSettingsView
{
    [SerializeField] private TMP_InputField diameterInputField;
    [SerializeField] private TMP_InputField heightInputField;


    private void Awake()
    {
        fadeAnimation.OnActiveChanged += (isActive) =>
        {
            if (isActive)
            {
                diameterInputField.text = raycastSelection.CurrentSelection.GetComponent<CylinderDetail>().Diameter.ToString();
                heightInputField.text = raycastSelection.CurrentSelection.GetComponent<CylinderDetail>().Height.ToString();
            }
        };
        diameterInputField.onValueChanged.AddListener((newValue) =>
        {
            if (float.TryParse(newValue, out var diameter))
            {
                raycastSelection.CurrentSelection.GetComponent<CylinderDetail>().Diameter = diameter;
            }
        });
        heightInputField.onValueChanged.AddListener((newValue) =>
        {
            if (float.TryParse(newValue, out var height))
            {
                raycastSelection.CurrentSelection.GetComponent<CylinderDetail>().Height = height;
            }
        });
    }
    private void OnDestroy()
    {
        diameterInputField.onValueChanged.RemoveAllListeners();
        heightInputField.onValueChanged.RemoveAllListeners();
    }


    protected override Type ShowOnType => typeof(CylinderDetail);
}
