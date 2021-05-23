using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhotonGenerator : MonoBehaviour
{
    [SerializeField] private Photon photonPrefab;
    [SerializeField] private OffsetObjectPooler offsetObjectPooler;


    private void Awake()
    {

    }
    private void Start()
    {

    }
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            Move();
        }
    }


    private void Move()
    {
        for (int i = 0; i < offsetObjectPooler.PoolAmount; i++)
        {
            Photon photon = offsetObjectPooler.GetObject().GetComponent<Photon>();
            photon.Show();
            photon.Move();
        }
    }
}
