using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangeRotation : MonoBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private float range;


    private int rotateDirection = 1;
    private int upDownRotateDirection = 0;
    private Coroutine rotateCoroutine;


    public void StartRotation()
    {
        if (rotateCoroutine == null)
        {
            rotateCoroutine = StartCoroutine(RotateRoutine());
        }
    }
    public void StopRotation()
    {
        if (rotateCoroutine != null)
        {
            StopCoroutine(rotateCoroutine);
            rotateCoroutine = null;
        }
    }

    private void Rotate()
    {
        transform.Rotate(upDownRotateDirection * speed * Time.deltaTime, rotateDirection * speed * Time.deltaTime, 0);

        float rotationAngleY = transform.localRotation.eulerAngles.y < 180
            ? transform.localRotation.eulerAngles.y
            : transform.localRotation.eulerAngles.y - 360;

        if (rotationAngleY > range)
        {
            rotateDirection = -1;
        }
        else if (rotationAngleY < -range)
        {
            rotateDirection = 1;
        }
    }
    private void PollChangeDirection()
    {
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            rotateDirection = -1;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            rotateDirection = 1;
        }
        if (Input.GetKey(KeyCode.UpArrow))
        {
            upDownRotateDirection = -1;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            upDownRotateDirection = 1;
        }
    }
    private IEnumerator RotateRoutine()
    {
        while (true)
        {
            Rotate();
            PollChangeDirection();

            yield return new WaitForEndOfFrame();
        }
    }
}
