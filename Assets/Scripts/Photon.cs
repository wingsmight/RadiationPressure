using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Photon : MonoBehaviour
{
    [SerializeField] private Vector3 direction;
    [SerializeField] private float speed;


    private Coroutine moveCoroutine;


    private void OnCollisionEnter(Collision other)
    {
        // Photon newPhoton = Instantiate(this, transform.position, transform.rotation, transform.parent);
        // var collider = newPhoton.gameObject.GetComponent<SphereCollider>();
        // Destroy(collider);
        // Destroy(newPhoton.GetComponent<Rigidbody>());
        // newPhoton.gameObject.AddComponent<Rigidbody>();
    }


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
        GetComponent<Rigidbody>().AddForce(direction * speed, ForceMode.Force);
        //if (moveCoroutine != null)
        //{
        //    StopCoroutine(moveCoroutine);
        //}
        //moveCoroutine = StartCoroutine(MoveRouitne());
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
