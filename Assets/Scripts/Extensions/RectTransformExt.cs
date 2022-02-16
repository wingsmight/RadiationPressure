using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RectTransformExt
{
    /// <summary>
    /// places rect transform to have the same dimensions as 'other', even if they don't have same parent.
    /// Relatively non-expensive.
    /// NOTICE - also modifies scale of your rectTransf to match the scale of other
    /// </summary>
    public static void MatchOther(this RectTransform rt, RectTransform other, bool isIgnoreScale = false)
    {
        Vector2 myPrevPivot = other.pivot;
        rt.position = other.position;

        if (!isIgnoreScale)
        {
            rt.localScale = other.localScale;
        }

        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, other.rect.width);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, other.rect.height);
        //rectTransf.ForceUpdateRectTransforms(); - needed before we adjust pivot a second time?
        rt.pivot = myPrevPivot;
    }
}
