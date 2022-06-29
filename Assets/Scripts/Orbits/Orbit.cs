using UnityEngine;

public abstract class Orbit : MonoBehaviour
{
    // Физический объект: спутник
    [SerializeField] protected GameObject satellite;
    // Круг, задающий орбиту
    [SerializeField][RequireInterface(typeof(IDrawable))] private new MonoBehaviour renderer;


    // Функция вызывается когда экземпляр скрипта будет загружен
    private void Awake()
    {
        PlaceSatellite(0);
    }
    // Функция, вызывающаяся каждый кадр
    private void Update()
    {
        Renderer.Draw();
    }


    // Функция помещения спутника относительно угла между спутником и центром Земли
    public abstract void PlaceSatellite(float satelliteEarthAngle);


    protected IDrawable Renderer => renderer as IDrawable;
}
