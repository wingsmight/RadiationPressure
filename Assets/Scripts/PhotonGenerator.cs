using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PhotonGenerator : MonoBehaviour
{
    [SerializeField] private Photon photonPrefab;
    [SerializeField] private OffsetObjectPooler offsetObjectPooler;
    [SerializeField] private TMP_InputField photonsCountInputField;


    private void Awake()
    {

    }
    private void Start()
    {
        photonsCountInputField.text = offsetObjectPooler.PoolAmount.ToString();
        photonsCountInputField.onValueChanged.AddListener((text) =>
        {
            offsetObjectPooler.PoolAmount = int.Parse(text);
        });
    }
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            Move();
        }
    }


    public void Move()
    {
        for (int i = 0; i < offsetObjectPooler.PoolAmount; i++)
        {
            Photon photon = offsetObjectPooler.GetObject().GetComponent<Photon>();
            photon.Show();
            photon.Move();
        }
    }
}
