using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class TectonicPoint {

    private readonly Planet parentPlanet;

    public List<TectonicTriangle> parentTriangles;

    private List<int> neighborPointIndices;

    public int Index;

    // Whether the point is shared by multiple plates or not.
    public bool IsSharedPoint;

    public Vector3 SpherePosition;

    public Vector2 ProjectedPosition;

    public float Longitude { get { return this.ProjectedPosition.x; } }
    public float Latitude { get { return this.ProjectedPosition.y; } }
    public int DirectionAdjust { get; private set; }

    public float thickness;
    public float density;
    public float materialAverageAge;

    private Vector2 parentTrianglesTotalVelocity;
    private int parentTrianglesActing;

    public TectonicPoint (Planet _parent, Vector3 _startPos, int _index) {
        this.parentPlanet = _parent;
        this.Index = _index;
        this.parentTriangles = new List<TectonicTriangle>();

        this.SetSpherePosition(_startPos);// * this.parentPlanet.planetSettings.PlanetRadius);
        this.DirectionAdjust = 1;

        this.parentTrianglesActing = 0;
        this.parentTrianglesTotalVelocity = Vector2.zero;
    }

    public void SetParentTriangle (TectonicTriangle _triangle) {
        if (this.parentTriangles.Contains(_triangle)) {
            this.parentTriangles.Remove(_triangle);
        }
        else {
            this.parentTriangles.Add(_triangle);
        }
    }

    public void SetSpherePosition (Vector3 _pos) {
        this.SpherePosition = _pos;

        this.ProjectedPosition = TectonicFunctions.MapSpherePointOntoProjected(_pos);

        if (float.IsNaN(this.Latitude) || float.IsNaN(this.Longitude)) {
            Debug.LogError("Something went wrong!");
        }
    }

    public void SetProjectedPosition (Vector2 _projected) {
        this.ProjectedPosition.x = _projected.x % Mathf.PI;
        this.ProjectedPosition.y = _projected.y % (Mathf.PI / 2f);

        this.SpherePosition = TectonicFunctions.MapProjectedPointOntoSphere(_projected);
    }

    public void SetDensity (float _density)
    {
        this.density = _density;
    }

    public void SetThickness (float _thickness)
    {
        this.thickness = _thickness;
    }

    public float GetElevation () {
        return (12.5f * this.thickness * (1 - (21 * this.density / 50f)) / 70f) - (2.5f * (1 + (0.1f * this.density)));
    }

    public Vector2 GetDirectionToPoint (Vector3 _otherSpherePoint, float _distanceAlongPath = 0.025f) {
        // Find the vector to travel along to get direction.
        Vector3 diff = _otherSpherePoint - this.SpherePosition;
        // Use the midpoint between the two points and get the point along the path to then check for direction.
        diff = ((_otherSpherePoint + this.SpherePosition) / 2f) + (diff * _distanceAlongPath);

        // Calculate the new projected point.
        Vector2 projectedPoint = TectonicFunctions.MapSpherePointOntoProjected(diff);

        // Get the direction around the sphere.
        Vector2 direction = (projectedPoint - this.ProjectedPosition);

        // Ensure that crossing the -180 to 180 boundary doesn't break the direction.
        direction.x = (direction.x < -Mathf.PI) ? direction.x + (Mathf.PI * 2f) : (direction.x > Mathf.PI) ? direction.x - (Mathf.PI * 2f) : direction.x;
        // Return the normalized direction.
        return direction.normalized;
    }

    public void MovePoint2 (Vector2 _direction, float _amount) {

        Vector3 finalPosition;
        Vector3 sideAxis;
        if (this.SpherePosition != Vector3.up) {
            sideAxis = Vector3.Cross(this.SpherePosition, Vector3.up).normalized;
            finalPosition = this.RotateVector(this.SpherePosition, Vector3.up, _direction.y * _amount * Mathf.PI);
            finalPosition = this.RotateVector(finalPosition, sideAxis, _direction.x * _amount * Mathf.PI * this.DirectionAdjust);
        }
        else {
            sideAxis = Vector3.Cross(this.SpherePosition, Vector3.down).normalized;
            finalPosition = this.RotateVector(this.SpherePosition, Vector3.down, _direction.y * _amount * Mathf.PI);
            finalPosition = this.RotateVector(finalPosition, sideAxis, _direction.x * _amount * Mathf.PI * this.DirectionAdjust);
        }

        // See if we crossed over a pole.
        if (Mathf.Sign(Vector3.Dot(this.SpherePosition, Vector3.right)) != Mathf.Sign(Vector3.Dot(finalPosition, Vector3.right)) &&
            Mathf.Sign(Vector3.Dot(this.SpherePosition, Vector3.forward)) != Mathf.Sign(Vector3.Dot(finalPosition, Vector3.forward))) {
            Debug.Log("Point went over a pole.");
            this.DirectionAdjust *= -1;
        }

        this.SetSpherePosition(finalPosition);
    }

    public void MovePoint (Vector2 _direction, float _amount) {
        // First get the circle plane for where the displacement point will be.

        /* This uses a planet radius. Easier to use a unit sphere.
        float planeDistance = this.SpheretoSphereIntersectionPlane(this.parentPlanet.planetSettings.PlanetRadius, _amount);
        Vector3 planePosition = this.Position * (planeDistance / this.Position.magnitude);
        float circleRadius = Mathf.Sqrt(Mathf.Pow(this.parentPlanet.planetSettings.PlanetRadius, 2) - Mathf.Pow(planeDistance, 2));
        */

        float planeDistance = this.SpheretoSphereIntersectionPlane(1, _amount);
        Vector3 planePosition = this.SpherePosition * planeDistance;
        float circleRadius = Mathf.Sqrt(1 - Mathf.Pow(planeDistance, 2));

        // Calculate the displacement that will be moved.
        //Vector3 displacement = new Vector3(0, Mathf.Cos(_direction), Mathf.Sin(_direction));
        //displacement *= circleRadius;

        // Calculate the rotation axis' that will be used.
        Vector3 upAxis, rightAxis;
        if (this.SpherePosition.normalized == Vector3.up) {
            // If we're at the north pole, we need to use the south pole for our up axis.
            upAxis = -Vector3.Cross(this.SpherePosition, Vector3.down).normalized;
        }
        else {
            upAxis = Vector3.Cross(this.SpherePosition, Vector3.up).normalized;
        }
        rightAxis = Vector3.Cross(this.SpherePosition, upAxis).normalized;

        // Calculate the local displacement adjusted for rotation.
        Vector3 upDisplacement = upAxis * circleRadius * _direction.y;
        Vector3 rightDisplacement = rightAxis * circleRadius * _direction.x * this.DirectionAdjust;

        // Get the new positon.
        Vector3 finalPosition = planePosition + rightDisplacement;

        // See if we crossed over a pole.
        if (Mathf.Sign(Vector3.Dot(this.SpherePosition, Vector3.right)) != Mathf.Sign(Vector3.Dot(finalPosition, Vector3.right)) &&
            Mathf.Sign(Vector3.Dot(this.SpherePosition, Vector3.forward)) != Mathf.Sign(Vector3.Dot(finalPosition, Vector3.forward))) {
            Debug.Log("Point went over a pole.");
            this.DirectionAdjust *= -1;
        }
        // Add the final component to the position.
        finalPosition += upDisplacement;

        // Make sure we don't end up on a pole.
        if (finalPosition == Vector3.up || finalPosition == Vector3.down) {
            // If we do, nudge the point slightly.
            finalPosition += (rightDisplacement + upDisplacement) * 0.01f;
            finalPosition.Normalize();
        }

        this.SetSpherePosition(finalPosition);
    }
    
    private Vector3 RotateVector ( Vector3 _original, Vector3 _axis, float _angle ) {
        // Find the cross and dot products from the original vector and the rotation axis.
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

    public void CaculatePointNeighbors ( ) {
        List<int> neighbors = new List<int>();
        int firstPlate = -1;
        bool sharedPoint = false;

        foreach (TectonicTriangle parent in this.parentTriangles) {
            for (int i = 0; i < 3; i++) {
                if (this.Index != parent.Points[i].Index && !neighbors.Contains(parent.Points[i].Index)) {
                    neighbors.Add(parent.Points[i].Index);
                }
            }

            if (firstPlate == -1) {
                firstPlate = parent.parentPlate.PlateIndex;
            }
            else if (firstPlate != parent.parentPlate.PlateIndex) {
                sharedPoint = true;
            }
        }



        this.neighborPointIndices = neighbors;
    }

    /*
    public void CalculatePointForce ( ) {
        foreach (int pointIndex in this.neighborPointIndices) {
            float distance = Vector3.Distance(this.SpherePosition, this.parentPlanet.TectonicPoints[pointIndex].SpherePosition);
            Vector3 force = Vector3.Normalize(this.SpherePosition - this.parentPlanet.TectonicPoints[pointIndex].SpherePosition);

            force *= this.parentPlanet.averageSideLength - distance;
            this.force += force;
        }
    }

    public void ApplyPointForce ( ) {
        this.velocity += this.force * Time.deltaTime;
        this.SetSpherePosition(this.SpherePosition + (this.velocity * Time.deltaTime));

        Vector3 normal = Vector3.Normalize(this.SpherePosition - this.parentPlanet.transform.position);
        this.SetSpherePosition(normal * this.parentPlanet.planetSettings.PlanetRadius);

        this.force = Vector3.zero;
    }
    */

    internal void AddParentTriangleVelocity (Vector2 _lateralVelocity, float _rotationVelocity) {
        this.parentTrianglesTotalVelocity += _lateralVelocity;
        this.parentTrianglesActing++;
    }

    internal void CalculatePointMovement (float _ageTimestep, float _movementTimestep) {
        // Add to the points age.
        this.materialAverageAge += _ageTimestep;

        // Get the direction of the velocity movement.
        Vector2 direction = this.parentTrianglesTotalVelocity.normalized;

        // Move the point around the sphere based on the magnitude of the velocity. We divide
        //  by the number of triangles to make sure points get the same average movement no
        //  matter the number of parent triangles.
        this.MovePoint(direction, this.parentTrianglesTotalVelocity.magnitude * _movementTimestep / (float)this.parentTrianglesActing);

        // Reset the velocity and acting triangles count.
        this.parentTrianglesTotalVelocity = Vector2.zero;
        this.parentTrianglesActing = 0;
    }
}

// Point:
//      Variables:
//          ParentTriangles (List of triangles): the triangles that all contain this point.
//          Position (Vector3): the position on the sphere of the planet that this point is at.
//          Velocity (Vector2): the velocity that the point is currently traveling at.
//          Forces (Vector2): the forces acting on this point this frame.
//          Density (float): the density of this specific point.
//          Thickness (float): the thickness this point is, built up over time from subducting other points.
//          Age (float): how old the point is.