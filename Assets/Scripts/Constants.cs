public class Constants
{
    public const float APLHA = 1.0f;
    public const float HI = 1.0f;
    public const float BETA = 1.0f;
    public const float RHO = 1.0f;
    public const float ETA = 1.0f;
    public const float ZN = 1.0f;
    public const float E = 1.0f; // power of the light flow ot surface unit
    public const float C = 299792458; // speed of light


    private static double h = double.NaN;


    public static double H
    {
        get
        {
            if (double.IsNaN(h))
            {
                h = Constants.E / Constants.C;
            }
            return h;
        }
    }
}
