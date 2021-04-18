using UnityEngine;

public static class Tools
{
    public static Vector3 LerpVector(Vector3 current, Vector3 target, float speed = .1f)
    {
        Vector3 lerped = Vector3.Lerp(current, target, speed);

        return lerped;
    }

    public static float Remap(this float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
}
