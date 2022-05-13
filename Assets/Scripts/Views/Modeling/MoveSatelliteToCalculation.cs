using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveSatelliteToCalculation : LoadSceneButton
{
    protected override void OnClick()
    {
        PrefabRuntime.CreatePrefab();

        base.OnClick();
    }
}
