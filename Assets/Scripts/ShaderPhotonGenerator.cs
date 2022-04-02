using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShaderPhotonGenerator : MonoBehaviour
{
    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private GameObject pooledPhoton;


    private Photon[] photons;


    public void ThrowViaShader(Vector3 startPosition, Vector3 direction, float energy, float density)
    {
        GeneratePhotons(energy, density);

        int vector3Size = sizeof(float) * 3;
        int float1Size = sizeof(float);
        int photonSize = vector3Size + float1Size;

        ComputeBuffer computeBuffer = new ComputeBuffer(photons.Length, photonSize);
        computeBuffer.SetData(photons);

        computeShader.SetBuffer(0, "photons", computeBuffer);
        computeShader.SetVector("direction", direction);
        computeShader.Dispatch(0, photons.Length / 16, photons.Length / 16, 1);
    }


    private void GeneratePhotons(float energy, float density)
    {
        var betweenOffset = new Vector3(1, 1, 0);
        var cubeWidth = 150;
        var dimension = 2;
        density = density / 10.0f;

        betweenOffset /= density;
        Vector3 realCubeWidth = Vector3.one;

        var capacity = 1;
        if (dimension >= 1)
        {
            capacity *= (int)((cubeWidth / betweenOffset.x));
            realCubeWidth = new Vector3(capacity, realCubeWidth.y, realCubeWidth.z);
        }
        if (dimension >= 2)
        {
            capacity *= (int)((cubeWidth / betweenOffset.y));
            realCubeWidth = new Vector3(realCubeWidth.x, realCubeWidth.x, realCubeWidth.z);
        }
        else if (dimension >= 3)
        {
            capacity *= (int)((cubeWidth / betweenOffset.z));
            realCubeWidth = new Vector3(realCubeWidth.x, realCubeWidth.x, realCubeWidth.x);
        }

        photons = new Photon[capacity];
        Vector3 objectSize = pooledPhoton.transform.localScale;
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
                        photons[objectIndex++] = new Photon(new Vector3(x * objectSize.x, y * objectSize.y, z * objectSize.z) + minPoint, energy);
                    }
                }
            }
        }
    }


    private struct Photon
    {
        public Vector3 position;
        public float energy;


        public Photon(Vector3 position, float energy)
        {
            this.position = position;
            this.energy = energy;
        }
    }
}
