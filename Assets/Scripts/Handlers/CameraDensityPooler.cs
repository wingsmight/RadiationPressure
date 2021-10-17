using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraDensityPooler : ObjectPooler
{
    private const float CAMERA_BOUND_SCALER = 1.0f;


    [SerializeField] private new Camera camera;
    

    private float density = 5.0f;


    protected override void Awake()
    {
        float cameraHeight = camera.orthographicSize * 2 * CAMERA_BOUND_SCALER;
        float cameraWidth = cameraHeight * camera.aspect * CAMERA_BOUND_SCALER;

        float rectangleWidth = cameraWidth;
        float rectangleHeight = cameraHeight;
        float horizontalObjectDistance = cameraWidth / density;
        float verticalObjectDistance = cameraHeight / density;
        int horizontalObjectCount = Mathf.RoundToInt(density);
        int verticalObjectCount = horizontalObjectCount;
        base.capacity = horizontalObjectCount * verticalObjectCount;

        Clean();
        base.Awake();

        Vector3 startPoint = new Vector3(-rectangleWidth / 2, -rectangleHeight / 2, 0);
        int objectIndex = 0;
        for (int horizontalObjectIndex = 0; horizontalObjectIndex < horizontalObjectCount; horizontalObjectIndex++)
        {
            for (int verticalObjectIndex = 0; verticalObjectIndex < verticalObjectCount; verticalObjectIndex++)
            {
                pooledObjects[objectIndex].GameObject.transform.localPosition = startPoint + new Vector3(horizontalObjectDistance * horizontalObjectIndex, verticalObjectDistance * verticalObjectIndex, 0);

                objectIndex++;
            }
        }
    }


    public void Init(float density)
    {
        this.density = density;

        Awake();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            Clean();
        }
    }
}
