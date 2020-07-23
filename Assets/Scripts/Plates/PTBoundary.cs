using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PTBoundary {

    private Planet parentPlanet;

    public PTHalfSide FirstSide { get; private set; }
    public PTHalfSide SecondSide { get; private set; }

    private Vector3 FirstReturnPosition;
    private Vector3 SecondReturnPosition;

    public PTBoundary (Planet _parentP, PTHalfSide _side1, PTHalfSide _side2) {
        this.parentPlanet = _parentP;

        this.FirstSide = _side1;
        this.SecondSide = _side2;

        this.FirstSide.SetParentBoundary(this);
        this.SecondSide.SetParentBoundary(this);

        this.FirstReturnPosition = Vector3.zero;
        this.SecondReturnPosition = Vector3.zero;
    }

    public List<int> GetBoundaryIndices () {
        List<int> indices = new List<int> {
            this.SecondSide.End.RenderIndex,
            this.FirstSide.End.RenderIndex,
            this.FirstSide.Start.RenderIndex,

            this.FirstSide.End.RenderIndex,
            this.SecondSide.End.RenderIndex,
            this.SecondSide.Start.RenderIndex
        };

        return indices;
    }

    public void CalculateReturnForce () {
        Vector3 firstDiff = this.FirstSide.Start.Location - this.SecondSide.End.Location;
        this.FirstSide.Start.AddForce(this.FirstReturnPosition - firstDiff);
        this.SecondSide.End.AddForce(firstDiff - this.FirstReturnPosition);

        Vector3 secondDiff = this.FirstSide.End.Location - this.SecondSide.Start.Location;
        this.FirstSide.End.AddForce(this.SecondReturnPosition - secondDiff);
        this.SecondSide.Start.AddForce(secondDiff - this.SecondReturnPosition);
    }

    public void CalculateReturnPosition (float _timestep) {
        this.FirstReturnPosition = (this.FirstReturnPosition * Mathf.Max(1 - _timestep, 0)) + ((this.FirstSide.Start.Location - this.SecondSide.End.Location) * Mathf.Min(_timestep, 1));
        this.SecondReturnPosition = (this.SecondReturnPosition * Mathf.Max(1 - _timestep, 0)) + ((this.FirstSide.End.Location - this.SecondSide.Start.Location) * Mathf.Min(_timestep, 1));

        Debug.Log(this.FirstReturnPosition);
    }
}
