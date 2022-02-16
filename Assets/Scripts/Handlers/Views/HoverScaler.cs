using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HoverScaler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private const float START_INTERPOLATION_VALUE = 0.0f;
    private const float FINISH_INTERPOLATION_VALUE = 1.0f;
    private const float START_FACTOR = 0.0f;


    [SerializeField] private Image image;
    [SerializeField] private float factor;
    [SerializeField] private float speed;


    private RectTransform imageRect;
    private Coroutine scaleLocationImageCoroutine;
    private float startSizeDeltaX;

    private delegate float InterpolationFunction(float t);
    private InterpolationFunction interpolationFunction = MathfExt.Smoothstep3; // choose whenever you want


    private void Awake()
    {
        imageRect = image.gameObject.GetComponent<RectTransform>();

        startSizeDeltaX = imageRect.sizeDelta.x;
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        if (scaleLocationImageCoroutine != null)
        {
            StopCoroutine(scaleLocationImageCoroutine);
            StopAllCoroutines();
        }
        scaleLocationImageCoroutine = StartCoroutine(ScaleLocationImageRoutine(factor));
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        if (scaleLocationImageCoroutine != null)
        {
            StopCoroutine(scaleLocationImageCoroutine);
            StopAllCoroutines();
        }
        scaleLocationImageCoroutine = StartCoroutine(ScaleLocationImageRoutine(START_FACTOR));
    }

    private IEnumerator ScaleLocationImageRoutine(float scaleFactor)
    {
        float finishSizeDeltaX = startSizeDeltaX + scaleFactor;
        float currentStartSizeDeltaX = imageRect.sizeDelta.x;
        float t = START_INTERPOLATION_VALUE;

        while (t <= FINISH_INTERPOLATION_VALUE)
        {
            t += speed * Time.deltaTime;

            float sizeDeltaX = Mathf.Lerp(currentStartSizeDeltaX, finishSizeDeltaX, interpolationFunction(t));
            imageRect.sizeDelta = new Vector2(sizeDeltaX, sizeDeltaX);

            yield return new WaitForEndOfFrame();
        }

        imageRect.sizeDelta = new Vector2(finishSizeDeltaX, finishSizeDeltaX);
    }
}
