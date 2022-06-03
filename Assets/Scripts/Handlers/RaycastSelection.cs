using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaycastSelection : MonoBehaviour
{
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private RaycastUIDetection uiDetection;


    private Vector3 screenCenter;
    private Transform selectedTransform = null;


    private void Awake()
    {
        screenCenter = new Vector3(Screen.width / 2.0f, Screen.height / 2.0f, 0.0f);
    }
    private void Update()
    {
        Ray ray;
        RaycastHit hit;

        if (Input.GetMouseButtonDown(0) && !uiDetection.IsUIUnderMouse())
        {
            if (selectedTransform != null)
            {
                Reset();
            }

            ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
            {
                var newSelectedTransform = hit.transform;
                var newSelectionRenderer = newSelectedTransform.GetComponent<Renderer>();
                if (newSelectionRenderer != null)
                {
                    if (selectedTransform != null)
                    {
                        var selectedRenderer = selectedTransform.GetComponent<Renderer>();
                        selectedRenderer.materials = new Material[1] { selectedRenderer.material };
                    }

                    newSelectionRenderer.materials = new Material[2] { newSelectionRenderer.material, highlightMaterial };
                    selectedTransform = newSelectedTransform;
                }
            }
        }
    }


    public void Reset()
    {
        if (selectedTransform == null)
            return;
            
        var selectionRenderer = selectedTransform.GetComponent<Renderer>();
        if (selectionRenderer != null)
        {
            selectionRenderer.materials = new Material[1] { selectionRenderer.material };
            selectedTransform = null;
        }
    }


    public Transform CurrentSelection => selectedTransform;
}
