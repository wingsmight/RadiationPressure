using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

public class ThrownPhotonsCountInputView : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;


    private void Awake()
    {
        inputField.onValidateInput += ValidateInput;
    }


    private char ValidateInput(string text, int charIndex, char addedChar)
    {
        // var output = Regex.Replace(addedChar + "", @"[0-9]", "\0");
        // return output.ToCharArray()[0];

        return addedChar;
    }
}
