using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Сoating0", menuName = "Сoatings/Сoating")]
public class Coating : ScriptableObject
{
    [SerializeField] private Material material;
    [SerializeField] private OpticalCoefficients coefficients;


    //public Material Material => material;
    public Material Material { get => material; set => material = value; } // TEST
    public OpticalCoefficients Coefficients => coefficients;
}

[Serializable]
public class OpticalCoefficients
{
    public float alpha = 0.1f; // reflection coefficient
    public float hi = 0.1f; // transmittance
    public float beta = 0.1f; // optical return loss (ORL) coefficient
    public float rho = 0.1f; // mirror reflectivity
    public float eta = 0.1f; // absorption coefficient
    public float zn = 0.1f; // IR
}
