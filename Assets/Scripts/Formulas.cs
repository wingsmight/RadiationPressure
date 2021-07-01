using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Formulas
{
    public static Vector3 RadiationForce(Vector3 n, Vector3 t)
    {
        double cosnt = Vector3.Angle(n, t);

        return FallenFlow(cosnt, t) +
                InverseReflectedFlow(cosnt, t) +
                DiffuseReflectedFlow(cosnt, n) +
                AsymmetricalEmitedFlow(cosnt, n) +
                SpecularReflectedFlow(cosnt, n);
    }
    public static Vector3 FallenFlow(double cosnt, Vector3 t)
    {
        return (float)(Constants.H * cosnt * (1 - Constants.APLHA * Constants.RHO - Constants.HI)) * t;
    }
    public static Vector3 InverseReflectedFlow(double cosnt, Vector3 t)
    {
        return (float)(Constants.H * Constants.APLHA * Constants.BETA * cosnt) * t;
    }
    public static Vector3 DiffuseReflectedFlow(double cosnt, Vector3 n)
    {
        return (float)(2.0d / 3.0d * Constants.H * Constants.APLHA * (1 - Constants.RHO - Constants.BETA) * cosnt) * n;
    }
    public static Vector3 AsymmetricalEmitedFlow(double cosnt, Vector3 n)
    {
        return (float)(Constants.ZN * 2.0d / 3.0d * Constants.H * (1 - Constants.APLHA - Constants.HI) * (1 - Constants.ETA) * cosnt) * n;
    }
    public static Vector3 SpecularReflectedFlow(double cosnt, Vector3 n)
    {
        return (float)(2.0d * Constants.H * Constants.APLHA * Constants.RHO * (cosnt * cosnt)) * n;
    }
}
