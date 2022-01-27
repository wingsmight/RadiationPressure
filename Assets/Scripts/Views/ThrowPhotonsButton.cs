using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ThrowPhotonsButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [Space(12)]
    [SerializeField] private PhotonGenerator photonGenerator;
    [SerializeField] private DensityPool cameraDensityPooler;
    [SerializeField] private PhotonDensityInputView densityInputView;


    private void Awake()
    {
        button.onClick.AddListener(OnClick);
    }


    private void OnClick()
    {
        cameraDensityPooler.Init(densityInputView.Value);
        photonGenerator.Throw();
    }
}
