using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Photon : MonoBehaviour
{
    [SerializeField] private Vector3 direction;
    [SerializeField] private float speed;


    private Coroutine moveCoroutine;


    public void Show()
    {
        gameObject.SetActive(true);
    }
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    public void Move()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }
        moveCoroutine = StartCoroutine(MoveRouitne());
    }

    private IEnumerator MoveRouitne()
    {
        while (true)
        {
            transform.Translate(direction * speed * Time.timeScale, Space.Self);

            yield return new WaitForEndOfFrame();
        }
    }
}
