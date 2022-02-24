using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

public class PhotonDensityInputView : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [Space(12)]
    [SerializeField] private float defaultValue;


    private void Awake()
    {
        ResetValue();

        inputField.onValidateInput += ValidateInput;
    }


    public void ResetValue()
    {
        inputField.text = defaultValue.ToString();
        inputField.interactable = true;
    }


    private char ValidateInput(string text, int charIndex, char addedChar)
    {
        // var output = Regex.Replace(addedChar + "", @"[0-9]", "\0");
        // return output.ToCharArray()[0];

        return addedChar;
    }


    public float Value => float.Parse(inputField.text);
    public bool Interactable { get => inputField.interactable; set => inputField.interactable = value; }
}
