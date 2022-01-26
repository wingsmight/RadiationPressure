using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CoatingExchangeView : SelectionSettingsView
{
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private List<Сoating> coatings;


    private void Awake()
    {
        fadeAnimation.OnActiveChanged += (isActive) =>
        {
            if (isActive)
            {
                var currentMaterial = raycastSelection.CurrentSelection.GetComponent<MeshRenderer>().material;
                int currentMaterialIndex = coatings.FindIndex(x => x.Material.name.Contains(currentMaterial.name) || currentMaterial.name.Contains(x.Material.name));
                dropdown.SetValueWithoutNotify(currentMaterialIndex);
            }
        };
        dropdown.onValueChanged.AddListener((newMaterialIndex) =>
        {
            raycastSelection.CurrentSelection.GetComponent<MeshRenderer>().material = coatings[newMaterialIndex].Material;
        });
    }
    private void OnDestroy()
    {
        dropdown.onValueChanged.RemoveAllListeners();
    }
    protected override Type ShowOnType => typeof(MeshRenderer);
}
