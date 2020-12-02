using UnityEngine;
using System.Collections;

public class PTHalfSide {

    private Planet parentPlanet;
    
    public PTTriangle ParentTriangle { get; private set; }
    public PTBoundary ParentBoundary { get; private set; }


    public PTPoint Start;
    public PTPoint End;


    public float CurrentLength => this.length;
    public float DesiredLength => this.desired;

    private float length;
    private float previousLength = 0f;
    private float desired;
    private float stiffness = 10f;

    public Vector3 Cross;

    public PTHalfSide (Planet _parentP, PTPoint _start, PTPoint _end) {
        this.parentPlanet = _parentP;
        this.Start = _start;
        this.End = _end;

        this.CalculateLength();
        this.desired = this.length;
    }

    public void SetDesired(float _desired) {
        this.desired = _desired;
    }

    public void CalculateLength(float _lengthChangeMax = 0.001f) {
        // Set the previous length of the side.
        this.previousLength = this.length;

        // Calculate the current length of the side.
        this.length = Mathf.Acos(Vector3.Dot(this.Start.SphereLocation, this.End.SphereLocation));

        if (Mathf.Abs(this.length) < 0.0001f || float.IsNaN(this.length)) {
            this.length = 0f;
        }

        // Calculate the stiffness of the side.
        //  If the gap stays relatively the same, stiffness will increase. If the gap is rapidly changing, stiffness will drop.
        float lengthChange = Mathf.Abs(this.length - this.previousLength);
        float stiffnessChange = Mathf.Min(_lengthChangeMax, Mathf.Max(-_lengthChangeMax, _lengthChangeMax - lengthChange)) * 20f;
        this.stiffness = Mathf.Min(100f, Mathf.Max(5f, this.stiffness + stiffnessChange));

        this.Cross = Vector3.Cross(this.Start.SphereLocation, this.End.SphereLocation).normalized;
    }

    public void SetParentTriangle (PTTriangle _parentT) {
        this.ParentTriangle = _parentT;
    }

    public void SetParentBoundary (PTBoundary _parentB) {
        this.ParentBoundary = _parentB;
    }

    public void CalculateEdgeForce (float _timestep) {
        float forceDiff = (this.desired - this.length) / 2f;
        Vector3 torqueVector = this.Cross * forceDiff * this.stiffness * _timestep;

        this.Start.AddTorque(-torqueVector, 2f);
        this.End.AddTorque(torqueVector, 2f);

    }
}
