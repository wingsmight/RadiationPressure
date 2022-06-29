using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Linq;

public class SatelliteSlotView : UIButton
{
    protected const string MODELING_SCENE_NAME = "Modeling";
    protected const string CALCULATION_SCENE_NAME = "Calculation";


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
        ModelLoading.lastSatelliteName = labelTextView.text;
        SceneManager.LoadScene(CALCULATION_SCENE_NAME);
    }
}
