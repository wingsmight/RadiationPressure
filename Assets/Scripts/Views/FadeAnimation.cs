using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class FadeAnimation : MonoBehaviour
{
    [SerializeField] protected float speed = 1.0f;
    [SerializeField] protected bool isShowOnAwake = true;

    protected CanvasGroup canvasGroup;
    private bool isBlockRaycasts;
    private bool isInteractable;

    public delegate void ChangedEventHandler(bool state);
    public event ChangedEventHandler OnActiveChanged;


    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        isBlockRaycasts = canvasGroup.blocksRaycasts;
        isInteractable = canvasGroup.interactable;

        SetVisible(isShowOnAwake);
    }

    public virtual void Appear(float border = 1.0f)
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        StopAllCoroutines();
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(FadeIn(border));
        }
        else
        {
            Debug.Log("Coroutine FadeIn couldn't be started because the game object is inactive");
            canvasGroup.alpha = 1.0f;
        }
    }
    public virtual void Disappear()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        StopAllCoroutines();
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(FadeOut());
        }
        else
        {
            Debug.Log("Coroutine FadeOut couldn't be started because the game object is inactive");

            canvasGroup.alpha = 0.0f;
        }
    }
    public void SetVisible(bool state)
    {
        StopAllCoroutines();
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        canvasGroup.interactable = isInteractable && state;
        canvasGroup.blocksRaycasts = isBlockRaycasts && state;
        canvasGroup.alpha = state ? 1 : 0;
    }

    private IEnumerator FadeOut()
    {
        while (canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= Time.deltaTime * speed;
            yield return null;
        }

        SetVisible(false);

        OnActiveChanged?.Invoke(false);
    }
    private IEnumerator FadeIn(float border = 1.0f)
    {
        canvasGroup.blocksRaycasts = isBlockRaycasts;

        while (canvasGroup.alpha < border)
        {
            canvasGroup.alpha += Time.deltaTime * speed;
            yield return null;
        }

        SetVisible(true);

        OnActiveChanged?.Invoke(true);
    }

    public bool IsShowing => canvasGroup.alpha > 0;
}
