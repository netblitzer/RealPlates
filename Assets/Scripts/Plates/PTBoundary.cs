using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class PTBoundary {

    private Planet parentPlanet;

    public PTHalfSide FirstSide { get; private set; }
    public PTHalfSide SecondSide { get; private set; }

    private int[] indices;

    public PTBoundaryCorner FirstCorner { get; private set; }
    public PTBoundaryCorner SecondCorner { get; private set; }


    public PTBoundary (Planet _parentP, PTHalfSide _side1, PTHalfSide _side2) {
        this.parentPlanet = _parentP;
        this.indices = new int[6];

        this.FirstSide = _side1;
        this.SecondSide = _side2;

        this.FirstSide.SetParentBoundary(this);
        this.SecondSide.SetParentBoundary(this);

        this.FirstCorner = new PTBoundaryCorner(this.FirstSide, this.SecondSide.End);
        this.SecondCorner = new PTBoundaryCorner(this.SecondSide, this.FirstSide.End);
    }

    public int[] GetBoundaryIndices () {
        this.FirstCorner.SetIndices();
        this.SecondCorner.SetIndices();

        int i = 0;
        for (i = 0; i < 3; i++) {
            this.indices[i] = this.FirstCorner.indices[i];
        }
        i = 0;
        for (i = 0; i < 3; i++) {
            this.indices[i + 3] = this.SecondCorner.indices[i];
        }

        return this.indices;
    }

    public void CalculateBoundaryInformation () {
        this.FirstCorner.CalculateCorner();
        this.SecondCorner.CalculateCorner();

        this.FirstSide.CalculateLength();
        this.SecondSide.CalculateLength();
    }

    public void CalculateBoundaryForces (float _timestep) {
        this.FirstCorner.CalculateCornerForce(_timestep);
        this.SecondCorner.CalculateCornerForce(_timestep);

        this.FirstSide.CalculateEdgeForce(_timestep);
        this.SecondSide.CalculateEdgeForce(_timestep);
    }

    public void SetCornerDesired () {
        this.FirstCorner.SetDesired();
        this.SecondCorner.SetDesired();
    }
/*
    public void CalculateReturnForce () {
        Vector3 firstDiff = (this.FirstSide.Start.Location - this.SecondSide.End.Location) / 2f;
        this.FirstSide.Start.AddForce(this.FirstReturnPosition - firstDiff);
        this.SecondSide.End.AddForce(firstDiff - this.FirstReturnPosition);

        Vector3 secondDiff = (this.FirstSide.End.Location - this.SecondSide.Start.Location) / 2f;
        this.FirstSide.End.AddForce(this.SecondReturnPosition - secondDiff);
        this.SecondSide.Start.AddForce(secondDiff - this.SecondReturnPosition);
    }

    public void CalculateReturnPosition (float _timestep) {
        this.FirstReturnPosition = (this.FirstReturnPosition * Mathf.Max(1 - _timestep, 0)) + ((this.FirstSide.Start.Location - this.SecondSide.End.Location) * Mathf.Min(_timestep, 1));
        this.SecondReturnPosition = (this.SecondReturnPosition * Mathf.Max(1 - _timestep, 0)) + ((this.FirstSide.End.Location - this.SecondSide.Start.Location) * Mathf.Min(_timestep, 1));
    }

    public void CalculateBoundaryForce (float _timestep) {
        // The plane between the firstSide.Start and secondSide.End.
        //  The point these two will want to merge is along the circle on this plane.
        Vector3 firstPlane = Vector3.Cross(this.FirstSide.Start.SphereLocation, this.SecondSide.End.SphereLocation);
        // the plane between the secondSide.Start and firstSide.End.
        Vector3 secondPlane = Vector3.Cross(this.SecondSide.Start.SphereLocation, this.FirstSide.End.SphereLocation);

        // Get the distance in radians that the two boundary edges are from each other.
        float firstArcDistance = Mathf.Asin(Vector3.Dot(this.FirstSide.Start.SphereLocation, this.SecondSide.End.SphereLocation));
        float secondArcDistance = Mathf.Asin(Vector3.Dot(this.SecondSide.Start.SphereLocation, this.FirstSide.End.SphereLocation));

        this.FirstSide.Start.AddTorque(firstPlane   * firstArcDistance   * _timestep);
        this.SecondSide.Start.AddTorque(secondPlane * secondArcDistance  * _timestep);
        this.FirstSide.End.AddTorque(secondPlane    * -secondArcDistance * _timestep);
        this.SecondSide.End.AddTorque(firstPlane    * -firstArcDistance  * _timestep);
    }*/

}
