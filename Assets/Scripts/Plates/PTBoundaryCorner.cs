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
    private float previousGap = 0f;
    private float desired;
    private float gapDiff;
    public float stiffness = 10f;

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

    public void SetDesired (float _desired) {
        if (_desired < 0f) {
            this.desired = this.gap;
        }
        else {
            this.desired = _desired;
        }
    }

    public void CalculateCorner (float _gapChangeMax = 0.001f) {
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
        //  If the gap stays relatively the same, stiffness will increase. If the gap is rapidly changing, stiffness will drop.
        float gapChange = Mathf.Abs(this.gap - this.previousGap);
        this.stiffness = Mathf.Min(100f, Mathf.Max(5f, this.stiffness + (Mathf.Min(_gapChangeMax, Mathf.Max(-_gapChangeMax, _gapChangeMax - gapChange)) * 20f)));
    }

    public void CalculateCornerForce (float _timestep) {
        float forceDiff = (this.desired - this.gap) / 2f;
        Vector3 torqueVector = Vector3.Cross(this.OppositePoint.SphereLocation, this.TriangleSide.Start.SphereLocation).normalized * forceDiff * this.stiffness * _timestep;

        this.OppositePoint.AddTorque(-torqueVector, 2f);
        this.TriangleSide.Start.AddTorque(torqueVector, 2f);
    }
}
