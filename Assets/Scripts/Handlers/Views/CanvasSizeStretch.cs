using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class CanvasSizeStretch : MonoBehaviour
{
    private RectTransform rectTransform;
    private Canvas topCanvas;


    private void Awake()
    {
        Canvas[] canvases = GetComponentsInParent<Canvas>();
        topCanvas = canvases[canvases.Length - 1];

        rectTransform = GetComponent<RectTransform>();

        rectTransform.MatchOther(topCanvas.GetComponent<RectTransform>(), true);
    }
}
