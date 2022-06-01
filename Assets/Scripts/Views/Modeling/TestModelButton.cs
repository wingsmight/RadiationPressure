using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestModelButton : LoadSceneButton
{
    [SerializeField] private ModelSaving modelSave;


    protected override void OnClick()
    {
        base.OnClick();

        modelSave.Save();
    }
}
