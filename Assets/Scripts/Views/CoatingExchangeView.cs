using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CoatingExchangeView : SelectionSettingsView
{
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private List<Coating> coatings;


    private void Awake()
    {
        dropdown.options = coatings.Select(x => new TMP_Dropdown.OptionData(x.Name)).ToList();

        fadeAnimation.OnActiveChanged += (isActive) =>
        {
            if (isActive)
            {
                var currentCoating = raycastSelection.CurrentSelection.GetComponent<Detail>().Coating;
                int currentCoatingIndex = 0;
                if (currentCoating != null)
                {
                    currentCoatingIndex = coatings.FindIndex(x => x.Name == currentCoating.Name);
                }

                dropdown.SetValueWithoutNotify(currentCoatingIndex);
            }
        };
        dropdown.onValueChanged.AddListener((newMaterialIndex) =>
        {
            var newCoating = coatings[newMaterialIndex];
            var selectedDetail = raycastSelection.CurrentSelection.GetComponent<Detail>();

            if (selectedDetail != null)
            {
                selectedDetail.Coating = newCoating;
            }
        });
    }
    private void OnDestroy()
    {
        dropdown.onValueChanged.RemoveAllListeners();
    }


    protected override Type ShowOnType => typeof(Detail);
}
