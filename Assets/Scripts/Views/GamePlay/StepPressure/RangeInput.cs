using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RangeInput : MonoBehaviour
{
    [SerializeField] private FloatInput startInput;
    [SerializeField] private FloatInput finishInput;


    public float StartValue => startInput.Value;
    public float FinishValue => finishInput.Value;
}
