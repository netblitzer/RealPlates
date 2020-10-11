using UnityEngine;
using System.Collections;
using UnityEngine.UIElements;

public class PTBoundaryCorner {

    public PTHalfSide TriangleSide;
    public PTPoint OppositePoint;


    public float BoundaryGap => this.gap;
    public float BoundaryStiffness => this.stiffness;
    public float DesiredBoundaryGap => this.desired;

    public bool CornerOverlapped = false;

    private Vector3 cornerCross;
    private Vector3 prevCornerCross;

    private float gap;
    private float previousGap;
    private float desired;
    private float gapDiff;
    public float stiffness = 1f;

    public int[] indices;

    public PTBoundaryCorner (PTHalfSide _side, PTPoint _opposite) {
        this.TriangleSide = _side;
        this.OppositePoint = _opposite;

        this.indices = new int[3];

        this.CalculateCorner();

        this.desired = this.gap;
    }

    public void SetIndices () {
        this.indices[0] = this.OppositePoint.RenderIndex;
        this.indices[1] = this.TriangleSide.End.RenderIndex;
        this.indices[2] = this.TriangleSide.Start.RenderIndex;
    }

    public void SetDesired () {
        this.desired = this.gap;
    }

    public void CalculateCorner () {
        // Set the previous parameters.
        this.previousGap = this.gap;
        this.prevCornerCross = this.cornerCross;

        // Get the vector offsets across the gap and for the triangle side.
        Vector3 gapOffset = (this.TriangleSide.Start.SphereLocation - this.OppositePoint.SphereLocation);
        Vector3 sideOffset = (this.TriangleSide.Start.SphereLocation - this.TriangleSide.End.SphereLocation);

        // Calculate the current gap going across the boundary (triangle to triangle).
        this.gap = Mathf.Acos(Vector3.Dot(this.OppositePoint.SphereLocation, this.TriangleSide.Start.SphereLocation));
        // Calculate the current angle between the triangle side and the gap across the boundary.
        this.cornerCross = Vector3.Cross(gapOffset, sideOffset);
            //Mathf.Acos(Vector3.Dot(gapOffset, sideOffset) / (gapOffset.magnitude * sideOffset.magnitude));

        if (Vector3.Dot(this.cornerCross, this.prevCornerCross) < 0) {
            this.CornerOverlapped = !this.CornerOverlapped;
        }

        // Check to ensure that the gap is always set as a proper value or 0 if it's in floating
        //  point error range.
        if (float.IsNaN(this.gap) || Mathf.Abs(this.gap) < 0.0001f) {
            this.gap = 0f;
        }

        // Calculate the stiffness of the boundary.
        this.stiffness = Mathf.Min(1000, Mathf.Max(1, this.stiffness + ((1 - (Mathf.Abs(this.gap - this.previousGap) * 50000f)) * 0.1f)));
    }

    public void CalculateCornerForce (float _timestep) {
        float forceDiff = (this.desired - this.gap) / 2f;
        forceDiff = this.CornerOverlapped ? forceDiff * -1 : forceDiff;
        Vector3 torqueVector = Vector3.Cross(this.OppositePoint.SphereLocation, this.TriangleSide.Start.SphereLocation).normalized * forceDiff * this.stiffness * _timestep;

        this.OppositePoint.AddTorque(-torqueVector);
        this.TriangleSide.Start.AddTorque(torqueVector);
    }
}
