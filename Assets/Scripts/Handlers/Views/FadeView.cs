using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeView : MonoBehaviour, IShowable, IHidable
{
    [SerializeField] protected FadeAnimation fadeAnimation;


    public virtual event Action OnHidden;
    public virtual event Action OnShown;


    protected virtual void Awake()
    {
        fadeAnimation.OnActiveChanged += OnFadeChangedState;
    }
    protected virtual void OnDestroy()
    {
        fadeAnimation.OnActiveChanged -= OnFadeChangedState;
    }


    public virtual void Show()
    {
        fadeAnimation.Appear();
    }
    public virtual void ShowImmediately()
    {
        fadeAnimation.SetVisible(true);
    }
    public virtual void Hide()
    {
        fadeAnimation.Disappear();
    }
    public virtual void HideImmediately()
    {
        fadeAnimation.SetVisible(false);
    }


    protected void OnFadeChangedState(bool state)
    {
        if (state)
        {
            OnShown?.Invoke();
        }
        else
        {
            OnHidden?.Invoke();
        }
    }


    public virtual bool IsShowing => fadeAnimation.IsShowing;
}
