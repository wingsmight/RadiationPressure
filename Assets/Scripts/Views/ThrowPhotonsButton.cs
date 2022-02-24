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
    [SerializeField] private StepPressure stepPressure;
    [SerializeField] private RangeInput anglesInput;
    [SerializeField] private FloatInput stepInput;


    private void Awake()
    {
        button.onClick.AddListener(OnClick);
    }


    private void OnClick()
    {
        cameraDensityPooler.Init(densityInputView.Value);

        stepPressure.Calculate(anglesInput.StartValue, anglesInput.FinishValue, stepInput.Value);
    }
}
