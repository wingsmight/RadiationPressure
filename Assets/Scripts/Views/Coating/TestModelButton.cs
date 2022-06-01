using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestModelButton : LoadSceneButton
{
    [SerializeField] private ModelSaving modelSave;
    [SerializeField] private RaycastSelection raycastSelection;


    protected override void OnClick()
    {
        raycastSelection.Reset();
        modelSave.Save();

        base.OnClick();
    }
}
