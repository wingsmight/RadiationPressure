using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SelectionSettingsView : MonoBehaviour
{
    [SerializeField] protected FadeAnimation fadeAnimation;
    [SerializeField] protected RaycastSelection raycastSelection;


    protected virtual void Update()
    {
        if (raycastSelection.CurrentSelection != null && raycastSelection.CurrentSelection.TryGetComponent(ShowOnType, out var component))
        {
            fadeAnimation.Appear();
        }
        else
        {
            fadeAnimation.Disappear();
        }
    }


    protected abstract Type ShowOnType { get; }
}
