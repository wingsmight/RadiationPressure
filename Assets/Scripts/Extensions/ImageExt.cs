using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class ImageExt
{
    public static void AdjustWidth(this Image image)
    {
        if (image.sprite == null)
        {
            return;
        }

        // (ow / oh) * nh = nw
        var spriteRect = image.sprite.rect;
        float width = image.rectTransform.rect.height * (spriteRect.width / spriteRect.height);
        image.rectTransform.sizeDelta = new Vector2(width, image.rectTransform.sizeDelta.y);
    }
}
