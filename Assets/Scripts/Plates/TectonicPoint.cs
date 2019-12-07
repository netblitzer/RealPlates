using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TectonicPoint {

    private Planet parentPlanet;

    public List<TectonicTriangle> parentTriangles;

    public int Index;

    public Vector3 Position { get; private set; }
    public float Longitude { get; private set; }
    public float Latitude { get; private set; }
    private int directionAdjust;

    public TectonicPoint (Planet _parent, Vector3 _startPos, int _index) {
        this.parentPlanet = _parent;
        this.Index = _index;
        this.parentTriangles = new List<TectonicTriangle>();

        this.SetPosition(_startPos * this.parentPlanet.PlanetRadius);
        this.directionAdjust = 1;
    }

    public void SetPosition (Vector3 _pos) {
        this.Position = _pos;
        /*
        this.Latitude = Mathf.Asin(_pos.y / _radius);
        this.Longitude = Mathf.Atan2(_pos.z, _pos.x);
        if (float.IsNaN(this.Latitude) || float.IsNaN(this.Longitude)) {
            Debug.LogError("Something went wrong!");
        }
        */
    }

    /*
    private void UpdatePosition (float _radius) {
        this.Position = new Vector3((_radius * Mathf.Sin(this.Latitude) * Mathf.Cos(this.Longitude)),
            (_radius * Mathf.Sin(this.Latitude) * Mathf.Sin(this.Longitude)),
            (_radius * Mathf.Cos(this.Latitude)));
    }
    */

    public void MovePoint (float _direction, float _amount) {
        // First get the circle plane for where the displacement point will be.
        float planeDistance = this.SpheretoSphereIntersectionPlane(this.parentPlanet.PlanetRadius, _amount);
        Vector3 planePosition = this.Position * (planeDistance / this.Position.magnitude);
        float circleRadius = Mathf.Sqrt(Mathf.Pow(this.parentPlanet.PlanetRadius, 2) - Mathf.Pow(planeDistance, 2));

        // Calculate the displacement that will be moved.
        Vector3 displacement = new Vector3(0, Mathf.Cos(_direction), Mathf.Sin(_direction));
        displacement *= circleRadius;

        // Calcualte the rotation axis' that will be used.
        Vector3 upAxis, rightAxis;
        if (this.Position == Vector3.up) {
            // If we're at the north pole, we need to use the south pole for our up axis.
            upAxis = -Vector3.Cross(this.Position, Vector3.down).normalized;
        }
        else {
            upAxis = Vector3.Cross(this.Position, Vector3.up).normalized;
        }
        rightAxis = Vector3.Cross(this.Position, upAxis).normalized;

        // Calculate the local displacement adjusted for rotation.
        Vector3 upDisplacement = upAxis * circleRadius * Mathf.Sin(_direction);
        Vector3 rightDisplacement = rightAxis * circleRadius * Mathf.Cos(_direction) * this.directionAdjust;

        // Get the new positon.
        Vector3 finalPosition = planePosition + rightDisplacement;

        // See if we crossed over a pole.
        if (Mathf.Sign(Vector3.Dot(this.Position, Vector3.right)) != Mathf.Sign(Vector3.Dot(finalPosition, Vector3.right)) &&
            Mathf.Sign(Vector3.Dot(this.Position, Vector3.forward)) != Mathf.Sign(Vector3.Dot(finalPosition, Vector3.forward))) {
            Debug.Log("Point went over a pole.");
            this.directionAdjust *= -1;
        }
        // Add the final component to the position.
        finalPosition += upDisplacement;

        this.SetPosition(finalPosition);
    }
    
    private Vector3 RotateVector ( Vector3 _original, Vector3 _axis, float _angle ) {
        // Find the cross and dot products from the original vector and the rotation axis.is.
        Vector3 cross = Vector3.Cross(_axis, _original);
        float dot = Vector3.Dot(_axis, _original);

        // Rotate based on Rodrigues' Rotation Formula.
        Vector3 rotatedVector = (_original * Mathf.Cos(_angle))
            + (cross * Mathf.Sin(_angle))
            + (_axis * dot * (1 - Mathf.Cos(_angle)));
        return rotatedVector;
    }

    private float SpheretoSphereIntersectionPlane ( float _originRadius, float _minorRadius ) {
        return this.SpheretoSphereIntersectionPlane(_originRadius, _minorRadius, _originRadius);
    }

    private float SpheretoSphereIntersectionPlane ( float _originRadius, float _minorRadius, float _distanceFromOrigin ) {
        float distance = (Mathf.Pow(_distanceFromOrigin, 2) + Mathf.Pow(_originRadius, 2) - Mathf.Pow(_minorRadius, 2)) / (2f * _distanceFromOrigin);
        return distance;
    }


    public void SetParentTriangle (TectonicTriangle _triangle) {
        if (this.parentTriangles.Contains(_triangle)) {
            this.parentTriangles.Remove(_triangle);
        }
        else {
            this.parentTriangles.Add(_triangle);
        }
    }
}

// Point:
//      Variables:
//          ParentTriangles (List of triangles): the triangles that all contain this point.
//          Position (Vector3): the position on the sphere of the planet that this point is at.
//          Velocity (Vector3): the velocity that the point is currently traveling at.
//          Forces (Vector3): the forces acting on this point this frame.
//          Density (float): the density of this specific point.
//          Thickness (float): the thickness this point is, built up over time from subducting other points.
//          IsSubducting (bool): whether the point is currently being pulled under another plate. (might be unneeded)
//          Age (float): how old the point is.