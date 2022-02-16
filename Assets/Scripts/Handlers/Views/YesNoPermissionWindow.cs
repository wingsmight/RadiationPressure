using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class YesNoPermissionWindow : FadeView, IHidable
{
    [SerializeField] private Button yesButton;
    [SerializeField] private List<Button> noButtons;


    private Action actionAfterYes;
    private Action actionAfterNo;


    public void Show(Action actionAfterYes, Action actionAfterNo)
    {
        base.Show();

        this.actionAfterYes = actionAfterYes;
        this.actionAfterNo = actionAfterNo;

        yesButton.onClick.AddListener(OnButtonYesClicked);
        noButtons.ForEach(x => x.onClick.AddListener(OnButtonNoClicked));
    }

    private void OnButtonYesClicked()
    {
        Hide();

        actionAfterYes?.Invoke();

        yesButton.onClick.RemoveListener(OnButtonYesClicked);
    }
    private void OnButtonNoClicked()
    {
        Hide();

        actionAfterNo?.Invoke();

        noButtons.ForEach(x => x.onClick.RemoveListener(OnButtonNoClicked));
    }
}
