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

    public Vector3 TriangleCenter { get; private set; }

    public bool InternalTriangle { get; private set; }


    public Vector2 LateralForce;

    public Vector2 LateralVelocity;

    public float RotationalForce;

    public float RotationVelocity;


    public Vector3 currentVelocityPlaneNormal;

    public Vector3 localXZPlaneForward;

    public float currentVelocity;


    public Vector3 nextVelocityPlaneNormal;

    private float nextVelocity;

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

    /*public void SetInitialVelocity (Vector2 _velocity, float _rotational) {
        this.LateralVelocity = _velocity;
        this.RotationVelocity = _rotational;
    }*/

    public void SetInitialVelocity (Vector2 _localDirection, float _amount) {
        // Get the positive X vector for the triangle, keeping the vector on the X/Z plane.
        this.localXZPlaneForward = Vector3.Cross(this.TriangleCenter, Vector3.up).normalized;
        // Get the positive Y vector.
        Vector3 uy = Vector3.Cross(this.localXZPlaneForward, this.TriangleCenter);

        // Calculate the velocity plane normal for the triangle.
        Vector3 offsetPoint = this.TriangleCenter + (this.localXZPlaneForward * _localDirection.x) + (uy * _localDirection.y);
        this.currentVelocityPlaneNormal = Vector3.Cross(this.TriangleCenter, offsetPoint);

        this.currentVelocity = _amount;
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

        // Set everything back to 0.
        this.TriangleCenter = Vector3.zero;
        this.AverageDensity = 0f;
        this.AverageThickness = 0f;
        this.AverageAge = 0f;

        // Go through each point adding its data.
        for (int i = 0; i < 3; i++) {
            this.AverageDensity += this.Points[i].density;
            this.AverageThickness += this.Points[i].thickness;
            this.AverageAge += this.Points[i].materialAverageAge;
            this.TriangleCenter += this.Points[i].SpherePosition;
        }

        // Normalize the data.
        this.AverageDensity /= 3f;
        this.AverageThickness /= 3f;
        this.AverageAge /= 3f;
        this.TriangleCenter /= 3f;
    }

    public void CalculateTriangleForces (float _timeStep) {

        // Reset the next velocity information.
        //this.nextVelocity = 0f;
        //this.nextVelocityPlaneNormal = Vector3.zero;

        // Initialize variables.
        float outOfPlanePercent, orientation;
        Vector3 outOfPlane, inPlane;
        Vector2 nextVelocityInPlane = Vector2.zero;

        // Get the right vector to our triangle.
        Vector3 right = Vector3.Cross(this.TriangleCenter, this.currentVelocityPlaneNormal);

        // Go through each edge triangle and see what the next velocity would be.
        for (int i = 0; i < 3; i++) {
            // Calculate the amount that the triangle neighbor is in plane with the current triangle.
            outOfPlanePercent = Vector3.Dot(this.TriangleCenter, this.edgeNeighbors[i].currentVelocityPlaneNormal);
            outOfPlane = this.TriangleCenter * outOfPlanePercent;

            // Get the normal of the triangle neighbor in our plane.
            inPlane = this.edgeNeighbors[i].currentVelocityPlaneNormal - outOfPlane;

            // Calculate the orientation of the triangle neighbor's velocity plane compared to ours.
            orientation = Mathf.Acos(Vector3.Dot(this.currentVelocityPlaneNormal, inPlane.normalized)) * Mathf.Sign(Vector3.Dot(right, inPlane));

            // Add to the next velocity.
            nextVelocityInPlane += new Vector2(Mathf.Cos(orientation), Mathf.Sin(orientation)) * this.edgeNeighbors[i].currentVelocity;
        }

        //Debug.Log("(" + (nextVelocityInPlane.x * 10f) + ", " + (nextVelocityInPlane.y * 10f) + ")");

        // Calculate the next velocity's direction around the current point.
        float pushFactor = 0.05f;
        Vector2 nextVelocity2 = (Vector2.right * (1 - pushFactor) * this.currentVelocity) + (nextVelocityInPlane * pushFactor);

        // Calculate the amount of friction being acted on the triangle based on its speed.
        float frictionCoef = 1.05f;//nextVelocity2.magnitude / this.currentVelocity;
        nextVelocity2 /= frictionCoef;

        //Debug.Log("(" + (nextVelocity2.x * 10f) + ", " + (nextVelocity2.y * 10f) + ")");

        // Calculate the next velocity plane normal for the triangle.
        Vector3 offsetPoint = this.TriangleCenter + (-right * nextVelocity2.x) + (this.currentVelocityPlaneNormal * nextVelocity2.y);
        this.nextVelocityPlaneNormal = Vector3.Cross(this.TriangleCenter, offsetPoint).normalized;

        float maxSpeed = 0.05f;

        // Get the next velocity amount.
        this.nextVelocity = Mathf.Min(nextVelocity2.magnitude, maxSpeed);
    }

    public void CalculateTriangleVelocity () {
        // Add forces to the velocity.
        //this.LateralVelocity += this.LateralForce * Time.deltaTime;
        //this.RotationVelocity += this.RotationalForce * Time.deltaTime;

        Vector3 nextPosition;
        float circleRadiusSize;

        // Apply the triangle's velocity to all the tectonic points that make it up.
        for (int i = 0; i < 3; i++) {
            // Calculate the decrease in circle size based on the distance from the plane the point is.
            circleRadiusSize = Mathf.Cos(Vector3.Dot(this.Points[i].SpherePosition, this.nextVelocityPlaneNormal) * Mathf.PI / 2f);

            nextPosition = PTFunctions.RotateVectorQuaternion(this.Points[i].SpherePosition, this.nextVelocityPlaneNormal, this.nextVelocity * circleRadiusSize);

            this.Points[i].AddParentTriangleVelocity(nextPosition);
        }

        // Now that we've used our velocities, move them to our "current" state.
        this.currentVelocity = this.nextVelocity;
        this.currentVelocityPlaneNormal = this.nextVelocityPlaneNormal;

        // Reset the forces.
        //this.LateralForce = Vector2.zero;
        //this.RotationalForce = 0;
    }

    public void TestRender (float _distance, Color _forwardColor, bool _showNext = false) {
        // Get the point a distance from the center in the direction of movement.
        //Vector3 directionPoint = PTFunctions.MovePointAroundSphere(this.TriangleCenter, this.LateralVelocity.normalized, _distance);

        Gizmos.color = Color.red;
        //Gizmos.DrawLine(this.TriangleCenter, directionPoint);
        //Gizmos.DrawLine(Vector3.zero, this.TriangleCenter);

        Gizmos.color = _forwardColor;
        Vector3 nextPoint = PTFunctions.RotateVectorQuaternion(this.TriangleCenter, this.currentVelocityPlaneNormal, this.currentVelocity);
        Gizmos.DrawLine(this.TriangleCenter, nextPoint);
        //Gizmos.DrawLine(Vector3.zero, this.localXZPlaneForward);
        //Gizmos.DrawLine(Vector3.zero, Vector3.Cross(this.TriangleCenter, this.localXZPlaneForward).normalized);


        if (_showNext) {
            //Gizmos.DrawLine(Vector3.zero, this.currentVelocityPlaneNormal);

            nextPoint = PTFunctions.RotateVectorQuaternion(this.TriangleCenter, this.nextVelocityPlaneNormal, this.nextVelocity);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(this.TriangleCenter, nextPoint);

            Vector3 planeOffset = Vector3.Cross(this.TriangleCenter, this.currentVelocityPlaneNormal).normalized;
            Vector3[] meshVerts = new Vector3[] {
            this.TriangleCenter + planeOffset,
            this.TriangleCenter - planeOffset,
            -this.TriangleCenter + planeOffset,
            -this.TriangleCenter - planeOffset };

            int[] meshIndices = new int[] { 1, 2, 0, 3, 2, 1 };

            Mesh planeMesh = new Mesh();
            planeMesh.SetVertices(meshVerts);
            planeMesh.SetIndices(meshIndices, MeshTopology.Triangles, 0);
            planeMesh.RecalculateNormals();

            _forwardColor.a = 0.2f;
            Gizmos.color = _forwardColor;
            //Gizmos.DrawMesh(planeMesh);

            Vector3 right = Vector3.Cross(this.TriangleCenter, this.currentVelocityPlaneNormal);

            for (int i = 0; i < 3; i++) {
                Color nextColor = new Color(0, 1, (i + 1) / 3f);
                this.edgeNeighbors[i].TestRender(_distance, nextColor);
                Gizmos.color = nextColor;

                Vector3 outOfPlane = Vector3.Dot(this.TriangleCenter, this.edgeNeighbors[i].currentVelocityPlaneNormal) * this.TriangleCenter;
                Vector3 inPlane = this.edgeNeighbors[i].currentVelocityPlaneNormal - outOfPlane;
                //Gizmos.DrawLine(Vector3.zero, inPlane);

                planeOffset = Vector3.Cross(this.TriangleCenter, inPlane).normalized;
                meshVerts = new Vector3[] {
                    this.TriangleCenter + planeOffset,
                    this.TriangleCenter - planeOffset,
                    -this.TriangleCenter + planeOffset,
                    -this.TriangleCenter - planeOffset };

                meshIndices = new int[] { 1, 2, 0, 3, 2, 1 };

                planeMesh = new Mesh();
                planeMesh.SetVertices(meshVerts);
                planeMesh.SetIndices(meshIndices, MeshTopology.Triangles, 0);
                planeMesh.RecalculateNormals();

                nextColor.a = 0.2f;
                Gizmos.color = nextColor;
                //Gizmos.DrawMesh(planeMesh);

                float orientation = Mathf.Acos(Vector3.Dot(this.currentVelocityPlaneNormal, inPlane.normalized)) * 180 / Mathf.PI * Mathf.Sign(Vector3.Dot(right, inPlane));
                //Debug.Log("Triangle " + (i + 1) + ": o1=" + orientation);

            }
        }
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