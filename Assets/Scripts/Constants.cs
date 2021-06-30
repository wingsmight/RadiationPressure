public class Constants
{
    public const float APLHA = 1.0f;
    public const float KSI = 1.0f;
    public const float BETA = 1.0f;
    public const float P = 1.0f;
    public const float NU = 1.0f;
    public const float ZN = 1.0f;
    public const float E = 1.0f; // power of the light flow ot surface unit
    public const float C = 100000.0f; // light speed


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
