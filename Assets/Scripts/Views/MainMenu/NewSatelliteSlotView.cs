using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NewSatelliteSlotView : SatelliteSlotView
{
    protected override void OnClick()
    {
        SceneManager.LoadScene(MODELING_SCENE_NAME);
    }
}
