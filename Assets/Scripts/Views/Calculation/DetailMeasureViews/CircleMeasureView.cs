using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CircleMeasureView : SelectionSettingsView
{
    [SerializeField] private TMP_InputField diameterInputField;


    private void Awake()
    {
        fadeAnimation.OnActiveChanged += (isActive) =>
        {
            if (isActive)
            {
                diameterInputField.text = raycastSelection.CurrentSelection.GetComponent<CircleDetail>().Diameter.ToString();
            }
        };
        diameterInputField.onValueChanged.AddListener((newValue) =>
        {
            if (float.TryParse(newValue, out var diameter))
            {
                raycastSelection.CurrentSelection.GetComponent<CircleDetail>().Diameter = diameter;
            }
        });
    }
    private void OnDestroy()
    {
        diameterInputField.onValueChanged.RemoveAllListeners();
    }


    protected override Type ShowOnType => typeof(CircleDetail);
}
