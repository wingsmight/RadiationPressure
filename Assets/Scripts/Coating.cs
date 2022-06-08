using System;
using UnityEngine;

// Отображение в контекстном меню
[CreateAssetMenu(fileName = "Сoating0", menuName = "Сoatings/Сoating")]
public class Coating : ScriptableObject
{
    // Название
    [SerializeField] private new string name;
    // Визуальный материал
    [SerializeField] private Material material;
    // Оптические характеристики покрытия
    [SerializeField] private OpticalCoefficients coefficients;


    public string Name => name;
    public Material Material => material;
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
