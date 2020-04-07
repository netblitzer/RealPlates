using UnityEngine;
using System.Collections;

public static class TectonicFunctions 
{

    public static Vector3 MapProjectedPointOntoSphere (Vector2 _projected) {
        Vector3 spherePoint = new Vector3(Mathf.Sin(_projected.x) * Mathf.Cos(_projected.y),
            Mathf.Cos(_projected.x) * Mathf.Cos(_projected.y),
            Mathf.Sin(_projected.y));

        return spherePoint;
    }

    public static Vector2 MapSpherePointOntoProjected (Vector3 _sphere) {
        Vector2 projectedPoint = new Vector2(Mathf.Atan2(_sphere.z, _sphere.x),
            Mathf.Asin(_sphere.y));

        return projectedPoint;
    }
}
