using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MaterialExchangeView : SelectionSettingsView
{
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private List<Material> materials;


    private void Awake()
    {
        fadeAnimation.OnActiveChanged += (isActive) =>
        {
            if (isActive)
            {
                var currentMaterial = raycastSelection.CurrentSelection.GetComponent<MeshRenderer>().material;
                int currentMaterialIndex = materials.FindIndex(x => x.name.Contains(currentMaterial.name) || currentMaterial.name.Contains(x.name));
                dropdown.SetValueWithoutNotify(currentMaterialIndex);
            }
        };
        dropdown.onValueChanged.AddListener((newMaterialIndex) =>
        {
            raycastSelection.CurrentSelection.GetComponent<MeshRenderer>().material = materials[newMaterialIndex];
        });
    }
    private void OnDestroy()
    {
        dropdown.onValueChanged.RemoveAllListeners();
    }


    protected override Type ShowOnType => typeof(MeshRenderer);
}
