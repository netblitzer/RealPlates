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
    private float previousLength;
    private float desired;
    private float stiffness = 1f;

    public PTHalfSide (Planet _parentP, PTPoint _start, PTPoint _end) {
        this.parentPlanet = _parentP;
        this.Start = _start;
        this.End = _end;

        this.CalculateLength();
        this.desired = this.length;
    }

    public void CalculateLength() {
        // Set the previous length of the side.
        this.previousLength = this.length;

        // Calculate the current length of the side.
        this.length = Mathf.Asin(Vector3.Dot(this.Start.SphereLocation, this.End.SphereLocation));

        // Calculate the stiffness of the side.
        this.stiffness = Mathf.Min(1000, Mathf.Max(1, this.stiffness + ((1 - (Mathf.Abs(this.length - this.previousLength) * 50000f)) * 0.1f)));
    }

    public void SetParentTriangle (PTTriangle _parentT) {
        this.ParentTriangle = _parentT;
    }

    public void SetParentBoundary (PTBoundary _parentB) {
        this.ParentBoundary = _parentB;
    }

    public void CalculateEdgeForce (float _timestep) {
        float forceDiff = (this.desired - this.length) / 2f;
        Vector3 torqueVector = Vector3.Cross(this.Start.SphereLocation, this.End.SphereLocation).normalized * forceDiff * this.stiffness * _timestep;

        this.Start.AddTorque(torqueVector);
        this.End.AddTorque(-torqueVector);
    }
}
