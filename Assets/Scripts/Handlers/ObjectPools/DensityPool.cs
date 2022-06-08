using UnityEngine;

public class DensityPool : ObjectPooler
{
    // Измерение (1D, 2D или 3D)
    [SerializeField] private int dimension = 3;
    // Размер стороны куба, образующий пул
    [SerializeField] private float cubeWidth;
    // Расстояния между объектами
    [SerializeField] private Vector3 betweenOffset;


    // Размер объекта
    private Vector3 objectSize;
    // Плотность
    private float density = 0.1f;


    protected override void Awake()
    {
        var betweenOffset = this.betweenOffset;
        betweenOffset /= density;
        Vector3 realCubeWidth = Vector3.one;

        this.capacity = 1;
        if (dimension >= 1)
        {
            this.capacity *= (int)((cubeWidth / betweenOffset.x));
            realCubeWidth = new Vector3(capacity, realCubeWidth.y, realCubeWidth.z);
        }
        if (dimension >= 2)
        {
            this.capacity *= (int)((cubeWidth / betweenOffset.y));
            realCubeWidth = new Vector3(realCubeWidth.x, realCubeWidth.x, realCubeWidth.z);
        }
        else if (dimension >= 3)
        {
            this.capacity *= (int)((cubeWidth / betweenOffset.z));
            realCubeWidth = new Vector3(realCubeWidth.x, realCubeWidth.x, realCubeWidth.x);
        }

        base.Awake();

        Vector3 objectSize = pooledObject.transform.localScale;
        objectSize += betweenOffset / 2.0f;
        Vector3 minPoint = (realCubeWidth.x / dimension) * new Vector3(objectSize.x, objectSize.y, objectSize.z) * -1;
        int objectIndex = 0;
        for (int z = 0; z < realCubeWidth.z; z++)
        {
            for (int y = 0; y < realCubeWidth.y; y++)
            {
                for (int x = 0; x < realCubeWidth.x; x++)
                {
                    if (objectIndex < capacity)
                    {
                        // Установка позиции объекта по все площади/объему квадрата/куба
                        pooledObjects[objectIndex++].GameObject.transform.localPosition = new Vector3(x * objectSize.x, y * objectSize.y, z * objectSize.z) + minPoint;
                    }
                }
            }
        }
    }


    public void Init(float density)
    {
        this.density = density / 10.0f;

        Awake();
    }


    public Vector3 BetweenOffset => betweenOffset;
    public float Density => density;
}
