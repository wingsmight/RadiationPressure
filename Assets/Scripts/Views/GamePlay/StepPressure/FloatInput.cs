using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FloatInput : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;


    private float value;


    private void Awake()
    {
        inputField.onValueChanged.AddListener(SetValue);
    }
    private void Start()
    {
        SetValue(inputField.text);
    }
    private void OnDestroy()
    {
        inputField.onValueChanged.RemoveListener(SetValue);
    }


    public void SetValue(string stringValue)
    {
        value = float.Parse(stringValue);
    }


    public float Value => value;
}
