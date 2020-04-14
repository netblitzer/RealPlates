using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TectonicTriangle {

    private readonly Planet parentPlanet;

    public TectonicPoint[] Points { get; private set; }

    public int[] SideIndices { get; private set; }

    public TectonicPlate parentPlate { get; private set; }

    private TectonicTriangle[] edgeNeighbors;

    public float AverageDensity { get; private set; }

    public float AverageThickness { get; private set; }

    public float AverageAge { get; private set; }

    public float TriangleDirection { get; private set; }

    public float TriangleArea { get; private set; }

    public bool InternalTriangle { get; private set; }


    public Vector2 LateralForce;

    public Vector2 LateralVelocity;

    public float RotationalForce;

    public float RotationVelocity;

    public TectonicTriangle (Planet _parent, int _a, int _b, int _c) :
        this(_parent, _parent.TectonicPoints[_a], _parent.TectonicPoints[_b], _parent.TectonicPoints[_c]) { }

    public TectonicTriangle (Planet _parent, TectonicPoint _a, TectonicPoint _b, TectonicPoint _c) {
        this.parentPlanet = _parent;
        this.Points = new TectonicPoint[] { _a, _b, _c };

        this.edgeNeighbors = new TectonicTriangle[3];

        this.SetAsPointParent();

        this.SideIndices = new int[3];
        this.CreateHalfSides();

        this.InternalTriangle = false;
    }

    private void CreateHalfSides ()
    {
        // Go through the three sides of the triangle and create the half sides.
        for (int i = 0; i < 3; i++)
        {
            int j = (i + 1) % 3;

            HalfSide newSide = new HalfSide(this, this.parentPlanet, this.Points[i].Index, this.Points[j].Index);
            this.SideIndices[i] = newSide.Index;

            // Send the halfside to the planet's dictionary.
            this.parentPlanet.SetHalfSide(newSide.Index, newSide);
        }
    }

    private void SetAsPointParent () {
        foreach (TectonicPoint point in this.Points) {
            point.SetParentTriangle(this);
        }
    }

    public void SetParentPlate (TectonicPlate _parent) {
        this.parentPlate = _parent;
    }

    public void SetInitialVelocity (Vector2 _velocity, float _rotational) {
        this.LateralVelocity = _velocity;
        this.RotationVelocity = _rotational;
    }

    public TectonicTriangle[] GetNeighborTriangles () {
        return this.edgeNeighbors;
    }

    /// <summary>
    /// Returns the point indices that make up this triangle.
    /// </summary>
    /// <returns></returns>
    public TectonicPoint[] GetPoints () {
        return this.Points;
    }

    public int[] GetPointIndices ( ) {
        int[] indices = new int[3];
        for (int i = 0; i < 3; i++) {
            indices[i] = this.Points[i].Index;
        }
        return indices;
    }

    public int GetOtherPoint (int _a, int _b) {
        // Go through our points and see if we've found the opposite point.
        for (int i = 0; i < 3; i++) {
            if (this.Points[i].Index != _a && this.Points[i].Index != _b) {
                return this.Points[i].Index;
            }
        }

        // If somehow we don't find the other point, return an error state.
        return -1;
    }
    public TectonicPoint GetOtherPoint (TectonicPoint _a, TectonicPoint _b) {

        // Go through our points and see if we've found the opposite point.
        for (int i = 0; i < 3; i++) {
            if (this.Points[i].Index != _a.Index && this.Points[i].Index != _b.Index) {
                return this.Points[i];
            }
        }

        // If somehow we don't find the other point, return an error state.
        return null;
    }


    public void CalculateNeighbors () {
        // Grab the neighbor triangles (the opposite halfside to ours).
        for (int i = 0; i < 3; i++)
        {
            this.edgeNeighbors[i] = this.parentPlanet.triangleSides[this.parentPlanet.triangleSides[this.SideIndices[i]].Opposite].parentTriangle;
        }
    }

    public void CalculateInternalStatus ()
    {
        // Intialize as internal.
        this.InternalTriangle = true;

        // Go through each of our halfsides and see if they're external.
        for (int i = 0; i < 3; i++)
        {
            this.parentPlanet.triangleSides[this.SideIndices[i]].CalculateExternality();

            // Also grab the neighbor triangles (the opposite halfside to ours).
            this.edgeNeighbors[i] = this.parentPlanet.triangleSides[this.parentPlanet.triangleSides[this.SideIndices[i]].Opposite].parentTriangle;

            // If the neighbor isn't in our plate, set the triangle as external.
            if (this.edgeNeighbors[i].parentPlate.PlateIndex != this.parentPlate.PlateIndex)
            {
                this.InternalTriangle = false;
            }
        }
    }

    public float CalculateTriangleArea () {
        Vector3 ab = this.Points[1].SpherePosition - this.Points[0].SpherePosition;
        Vector3 bc = this.Points[2].SpherePosition - this.Points[1].SpherePosition;

        this.TriangleArea = Vector3.Magnitude(Vector3.Cross(ab, bc)) / 2f;
        return this.TriangleArea;
    }

    public void CalculateTriangleInformation () {
        this.CalculateTriangleArea();

        this.AverageDensity = 0f;
        this.AverageThickness = 0f;
        this.AverageAge = 0f;
        for (int i = 0; i < 3; i++) {
            this.AverageDensity += this.Points[i].density;
            this.AverageThickness += this.Points[i].thickness;
            this.AverageAge += this.Points[i].materialAverageAge;
        }
        this.AverageDensity /= 3f;
        this.AverageThickness /= 3f;
        this.AverageAge /= 3f;
    }

    public void CalculateTriangleForces () {

        // Reset the forces on the triangle.
        this.LateralForce = Vector2.zero;
        this.RotationalForce = 0f;

        // Go through each of our halfsides and calculate the forces being acted on the triangle.
        for (int i = 0; i < 3; i++) {
            HalfSide ourSide = this.parentPlanet.triangleSides[this.SideIndices[i]];
            HalfSide oppositeSide = this.parentPlanet.triangleSides[ourSide.Opposite];
            TectonicTriangle oppositeTriangle = oppositeSide.parentTriangle;

            // Calculate the amount the other triangle's velocity travels towards the triangle.
            float perpDot = Vector2.Dot(ourSide.Direction, oppositeTriangle.LateralVelocity);

            // Calculate modifiers.
            //  If our side is subducting or neither side is subducting, apply full force. If
            //  the other side is subducting, apply a small amount of force.
            float subductMod = 1; //ourSide.IsSubducting ? 1 : oppositeSide.IsSubducting ? 0.1f : 1;

            // Calculate the forces based on the triangle we're checking.
            this.LateralForce += oppositeTriangle.LateralVelocity * (1 - perpDot) * subductMod;
            this.RotationalForce += oppositeTriangle.LateralVelocity.magnitude * perpDot * subductMod;
        }
    }

    public void CalculateTriangleVelocity () {
        // Add forces to the velocity.
        this.LateralVelocity += this.LateralForce * Time.deltaTime;
        this.RotationVelocity += this.RotationalForce * Time.deltaTime;

        // Apply the triangle's velocity to all the tectonic points that make it up.
        for (int i = 0; i < 3; i++) {
            this.Points[i].AddParentTriangleVelocity(this.LateralVelocity, this.RotationVelocity);
        }

        // Reset the forces.
        this.LateralForce = Vector2.zero;
        this.RotationalForce = 0;
    }

    public void TestRender (float _distance) {
        // Get the center.
        Vector3 centerPoint = Vector3.zero;
        for (int i = 0; i < 3; i++) {
            centerPoint += this.Points[i].SpherePosition;
        }
        centerPoint.Normalize();

        // Get the point a distance from the center in the direction of movement.
        Vector3 directionPoint = TectonicFunctions.MovePointAroundSphere(centerPoint, this.LateralVelocity.normalized, _distance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(centerPoint, directionPoint);
    }
}

//  Triangle:
//      Variables:
//          Points (array of 3 points): the points that make up the triangle.
//          ParentPlate (plate): the plate that this triangle is contained in.
//          AverageDensity (float): average height of the points of the triangle.
//          AverageThickness (float): average thickness of the points.
//          TriangleDirection (float): the dot product of segments a-b and b-c. Used to see if the triangle ever flips.
//          TriangleArea (float): the total area taken up by the triangle.
//      Methods:
//          CalculateTriangleDirection () {float}: returns the current direction of the triangle.
//          CalculateTriangleArea () {float}: returns the total area of the triangle.