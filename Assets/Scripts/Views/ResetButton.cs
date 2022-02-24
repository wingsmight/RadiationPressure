using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResetButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [Space(12)]
    [SerializeField] private PhotonDensityInputView photonDensityInputView;
    [SerializeField] private Button throwPhotonButton;


    private void Awake()
    {
        button.onClick.AddListener(OnClick);
    }


    private void OnClick()
    {
        photonDensityInputView.ResetValue();
        throwPhotonButton.interactable = true;
    }
}
