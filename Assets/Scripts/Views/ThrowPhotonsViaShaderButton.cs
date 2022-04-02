using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ThrowPhotonsViaShaderButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [Space(12)]
    [SerializeField] private ShaderPhotonGenerator photonGenerator;
    [SerializeField] private PhotonDensityInputView densityInputView;
    [SerializeField] private float startEnergy;


    private void Awake()
    {
        button.onClick.AddListener(OnClick);
    }


    private void OnClick()
    {
        photonGenerator.ThrowViaShader(transform.position, transform.forward, startEnergy, densityInputView.Value);
    }
}
