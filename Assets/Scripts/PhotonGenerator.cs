using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PhotonGenerator : MonoBehaviour
{
    [SerializeField] [RequireInterface(typeof(IPhoton))] private Object photonPrefab;
    [SerializeField] private OffsetObjectPooler offsetObjectPooler;
    [SerializeField] private TMP_InputField photonsCountInputField;
    [Space(12)]
    [SerializeField] private int photonsCount;
    [SerializeField] private float startEnergy;


    public static Vector3 radiatoinForce = Vector3.zero;


    private void Awake()
    {

    }
    private void Start()
    {
        photonsCountInputField.text = photonsCount.ToString();
        photonsCountInputField.onValueChanged.AddListener((text) =>
        {
            if (!int.TryParse(text, out photonsCount))
            {
                photonsCount = 0;
            }
        });
    }
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            Throw();
        }
    }


    public void Throw()
    {
        for (int i = 0; i < photonsCount; i++)
        {
            var pooledPhotonObject = offsetObjectPooler.GetObject();
            pooledPhotonObject.SetActive(true);
            IPhoton photon = pooledPhotonObject.GetComponent<IPhoton>();
            photon.Throw(pooledPhotonObject.transform.position, transform.forward, startEnergy);
        }
    }


    private IPhoton Photon => photonPrefab as IPhoton;
}
