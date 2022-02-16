using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class AutoRectScroll : MonoBehaviour
{
    [SerializeField] private Vector2 speed;
    [SerializeField] private bool isPlaying = true;


    private RectTransform rectTransform;


    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }
    private void Update()
    {
        if (isPlaying)
        {
            rectTransform.anchorMax += Time.deltaTime * speed;
        }
    }


    public void Play()
    {
        isPlaying = true;
    }
    public void Pause()
    {
        isPlaying = false;
    }
}
