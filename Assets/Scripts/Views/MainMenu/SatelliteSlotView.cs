using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class SatelliteSlotView : UIButton
{
    private const string MODELING_SCENE_NAME = "Modeling";
    [SerializeField] private TextMeshProUGUI labelTextView;


    public void Show(Satellite satellite)
    {
        labelTextView.text = satellite.Name;
    }
    public void Hide()
    {

    }


    protected override void OnClick()
    {
        SceneManager.LoadScene(MODELING_SCENE_NAME);
    }
}
