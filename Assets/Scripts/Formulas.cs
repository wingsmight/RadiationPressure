using UnityEngine;

public static class Formulas
{
    public static Vector3 RadiationForce(Vector3 n, Vector3 t, OpticalCoefficients opticalCoeff)
    {
        double cosnt = Mathf.Cos(Vector3.Angle(n, t));

        return FallenFlow(cosnt, t, opticalCoeff) +
                InverseReflectedFlow(cosnt, t, opticalCoeff) +
                DiffuseReflectedFlow(cosnt, n, opticalCoeff) +
                AsymmetricalEmitedFlow(cosnt, n, opticalCoeff) +
                SpecularReflectedFlow(cosnt, n, opticalCoeff);
    }
    // Сила, обусловленная падающим потоком,
    public static Vector3 FallenFlow(double cosnt, Vector3 t, OpticalCoefficients opticalCoeff)
    {
        return (float)(Constants.H * cosnt * (1 - opticalCoeff.alpha * opticalCoeff.rho - opticalCoeff.hi)) * t;
    }
    // Сила обратно отражённого потока
    public static Vector3 InverseReflectedFlow(double cosnt, Vector3 t, OpticalCoefficients opticalCoeff)
    {
        return (float)(Constants.H * opticalCoeff.alpha * opticalCoeff.beta * cosnt) * t;
    }
    // Сила диффузно отраженного потока
    public static Vector3 DiffuseReflectedFlow(double cosnt, Vector3 n, OpticalCoefficients opticalCoeff)
    {
        return (float)(2.0d / 3.0d * Constants.H * opticalCoeff.alpha * (1 - opticalCoeff.rho - opticalCoeff.beta) * cosnt) * n;
    }
    // Сила несимметрично переизлученного потока в ИК диапазоне
    public static Vector3 AsymmetricalEmitedFlow(double cosnt, Vector3 n, OpticalCoefficients opticalCoeff)
    {
        return (float)(opticalCoeff.zn * 2.0d / 3.0d * Constants.H * (1 - opticalCoeff.alpha - opticalCoeff.hi) * (1 - opticalCoeff.eta) * cosnt) * n;
    }
    // Сила зеркально отраженного потока
    public static Vector3 SpecularReflectedFlow(double cosnt, Vector3 n, OpticalCoefficients opticalCoeff)
    {
        return (float)(2.0d * Constants.H * opticalCoeff.alpha * opticalCoeff.rho * (cosnt * cosnt)) * n;
    }
}
