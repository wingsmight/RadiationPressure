using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EulerAnglesExt
{
    public static float WrapAngleCoord(this float angle)
    {
        angle %= 360;
        if (angle > 180)
            return angle - 360;

        return angle;
    }
    public static Vector3 WrapAngle(this Vector3 angle)
    {
        return new Vector3(WrapAngleCoord(angle.x), WrapAngleCoord(angle.y), WrapAngleCoord(angle.z));
    }
}
