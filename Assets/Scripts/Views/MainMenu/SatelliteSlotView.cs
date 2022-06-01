using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Linq;

public class SatelliteSlotView : UIButton
{
    private const string MODELING_SCENE_NAME = "Modeling";
    private const string CALCULATION_SCENE_NAME = "Calculation";


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
        if (labelTextView.text.Any(char.IsDigit))
        {
            ModelLoading.lastSatelliteName = labelTextView.text;
            SceneManager.LoadScene(CALCULATION_SCENE_NAME);
        }
        else
        {
            SceneManager.LoadScene(MODELING_SCENE_NAME);
        }
    }
}
