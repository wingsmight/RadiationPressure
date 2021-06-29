using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Photon : MonoBehaviour, IPhoton
{
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
    public void Throw(Vector3 startPosition, Vector3 direction, float energy)
    {
        transform.position = startPosition;
        GetComponent<Rigidbody>().AddForce(direction * speed, ForceMode.Force);
    }

    private IEnumerator MoveRouitne(Vector3 direction)
    {
        while (true)
        {
            transform.Translate(direction * speed * Time.timeScale, Space.Self);

            yield return new WaitForEndOfFrame();
        }
    }
}
