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

    public static Vector3 MovePointAroundSphere (Vector3 _spherePoint, Vector2 _direction, float _amount, float _directionAdjust = 1f) {
        // First get the circle plane for where the displacement point will be.

        /* This uses a planet radius. Easier to use a unit sphere.
        float planeDistance = this.SpheretoSphereIntersectionPlane(this.parentPlanet.planetSettings.PlanetRadius, _amount);
        Vector3 planePosition = this.Position * (planeDistance / this.Position.magnitude);
        float circleRadius = Mathf.Sqrt(Mathf.Pow(this.parentPlanet.planetSettings.PlanetRadius, 2) - Mathf.Pow(planeDistance, 2));
        */

        float planeDistance = SpheretoSphereIntersectionPlane(1, _amount);
        Vector3 planePosition = _spherePoint * planeDistance;
        float circleRadius = Mathf.Sqrt(1 - Mathf.Pow(planeDistance, 2));

        // Calculate the displacement that will be moved.
        //Vector3 displacement = new Vector3(0, Mathf.Cos(_direction), Mathf.Sin(_direction));
        //displacement *= circleRadius;

        // Calculate the rotation axis' that will be used.
        Vector3 upAxis, rightAxis;
        if (_spherePoint.normalized == Vector3.up) {
            // If we're at the north pole, we need to use the south pole for our up axis.
            upAxis = -Vector3.Cross(_spherePoint, Vector3.down).normalized;
        }
        else {
            upAxis = Vector3.Cross(_spherePoint, Vector3.up).normalized;
        }
        rightAxis = Vector3.Cross(_spherePoint, upAxis).normalized;

        // Calculate the local displacement adjusted for rotation.
        Vector3 upDisplacement = upAxis * circleRadius * _direction.y;
        Vector3 rightDisplacement = rightAxis * circleRadius * _direction.x * _directionAdjust;

        // Get the new positon.
        Vector3 finalPosition = planePosition + rightDisplacement;

        // See if we crossed over a pole.
        if (Mathf.Sign(Vector3.Dot(_spherePoint, Vector3.right)) != Mathf.Sign(Vector3.Dot(finalPosition, Vector3.right)) &&
            Mathf.Sign(Vector3.Dot(_spherePoint, Vector3.forward)) != Mathf.Sign(Vector3.Dot(finalPosition, Vector3.forward))) {
            Debug.Log("Point went over a pole.");
            _directionAdjust *= -1;
        }
        // Add the final component to the position.
        finalPosition += upDisplacement;

        // Make sure we don't end up on a pole.
        if (finalPosition == Vector3.up || finalPosition == Vector3.down) {
            // If we do, nudge the point slightly.
            finalPosition += (rightDisplacement + upDisplacement) * 0.01f;
            finalPosition.Normalize();
        }

        return finalPosition;
    }

    public static float SpheretoSphereIntersectionPlane (float _originRadius, float _minorRadius) {
        return SpheretoSphereIntersectionPlane(_originRadius, _minorRadius, _originRadius);
    }

    public static float SpheretoSphereIntersectionPlane (float _originRadius, float _minorRadius, float _distanceFromOrigin) {
        float distance = (Mathf.Pow(_distanceFromOrigin, 2) + Mathf.Pow(_originRadius, 2) - Mathf.Pow(_minorRadius, 2)) / (2f * _distanceFromOrigin);
        return distance;
    }
}
