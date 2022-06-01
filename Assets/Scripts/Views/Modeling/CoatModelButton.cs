using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoatModelButton : LoadSceneButton
{
    [SerializeField] private ModelSaving modelSave;


    protected override void OnClick()
    {
        modelSave.Save();

        base.OnClick();
    }
}
