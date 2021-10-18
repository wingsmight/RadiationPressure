using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class DetailMeasureView : MonoBehaviour
{
    [SerializeField] protected FadeAnimation fadeAnimation;
    [SerializeField] protected RaycastSelection raycastSelection;


    protected virtual void Update()
    {
        if (raycastSelection.CurrentSelection != null && raycastSelection.CurrentSelection.TryGetComponent(DetailType, out var component))
        {
            fadeAnimation.Appear();
        }
        else
        {
            fadeAnimation.Disappear();
        }
    }


    protected abstract Type DetailType { get; }
}
