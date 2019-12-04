using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FibonacciSphere {
    
    public static List<Vector3> GenerateFibonacci (int _numberOfPoints, float _adjust, float _jitter) {
        List<Vector3> points = new List<Vector3>();
        float offset = 2f / _numberOfPoints;
        float increment = Mathf.PI * (3f - Mathf.Sqrt(5f));

        for (int i = 0; i < _numberOfPoints; i++) {
            float y = (i * offset) - 1 + (offset / 2f);
            float r = Mathf.Sqrt(1 - Mathf.Pow(y, 2));

            float phi = i * increment;

            float x = Mathf.Cos(phi) * r;
            float z = Mathf.Sin(phi) * r;

            Vector3 createdPoint = new Vector3(x, y, z);
            points.Add(createdPoint);
        }

        return points;
    }

    public static List<Vector3> GenerateFibonacciSmoother ( int _numberOfPoints, float _adjust, float _jitter ) {
        List<Vector3> points = new List<Vector3>();
        float offset = 2f / _numberOfPoints;
        float increment = Mathf.PI * (3f - Mathf.Sqrt(5f));

        float totalIncrement = 0f;
        float z = 1 - (offset / 2f);
        for (int i = 0; i < _numberOfPoints; i++) {
            float r = Mathf.Sqrt(1 - Mathf.Pow(z, 2));
            float x = Mathf.Cos(totalIncrement) * r;
            float y = Mathf.Sin(totalIncrement) * r;

            Vector3 createdPoint = new Vector3(x, y, z);
            points.Add(createdPoint);

            z -= offset;
            totalIncrement += increment;
        }

        return points;
    }

    private Vector3 MapPointToSphere (Vector2 _unitPoint) {
        Vector2 sphereCoords = new Vector2(Mathf.Acos((2f * _unitPoint.x) - 1) - (Mathf.PI / 2f), (2 * (Mathf.PI * _unitPoint.y)));
        return (new Vector3(Mathf.Cos(sphereCoords.x) * Mathf.Cos(sphereCoords.y), Mathf.Cos(sphereCoords.x) * Mathf.Sin(sphereCoords.y), Mathf.Sin(sphereCoords.y)));
    }
}
