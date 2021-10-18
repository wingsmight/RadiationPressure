using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaycastSelection : MonoBehaviour
{
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private RaycastUIDetection uiDetection;


    private Vector3 screenCenter;
    private Transform selectedTransform = null;
    private Material defaultMaterial = null;


    private void Awake()
    {
        screenCenter = new Vector3(Screen.width / 2.0f, Screen.height / 2.0f, 0.0f);
    }
    private void Update()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Input.GetMouseButtonDown(0) && !uiDetection.IsUIUnderMouse())
        {
            if (selectedTransform != null)
            {
                var selectionRenderer = selectedTransform.GetComponent<Renderer>();
                if (selectionRenderer != null)
                {
                    selectionRenderer.material = defaultMaterial;
                    selectedTransform = null;
                    defaultMaterial = null;
                }
            }


            if (Physics.Raycast(ray, out hit))
            {
                var newSelectedTransform = hit.transform;
                var selectionRenderer = newSelectedTransform.GetComponent<Renderer>();
                if (selectionRenderer != null)
                {
                    if (selectedTransform != null)
                    {
                        selectedTransform.GetComponent<Renderer>().material = defaultMaterial;
                    }

                    if (defaultMaterial == null)
                    {
                        defaultMaterial = selectionRenderer.material;
                    }

                    selectionRenderer.material = highlightMaterial;
                    selectedTransform = newSelectedTransform;
                }
            }
        }
    }


    public Transform CurrentSelection => selectedTransform;
}
