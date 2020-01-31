using UnityEngine;
using System.Collections;

public class HalfSide {

    private Planet parentPlanet;
    public TectonicTriangle parentTriangle { get; private set; }

    public int StartIndex;
    public int EndIndex;

    public float Direction;
    public float CordLength;
    public float Length;

    public int Opposite;

    public int Index;

    public bool IsExternal;
    public bool IsSubducting;

    public HalfSide (TectonicTriangle _parent, Planet _parentPlanet, int _start, int _end) {
        this.parentTriangle = _parent;
        this.parentPlanet = _parentPlanet;
        this.StartIndex = _start;
        this.EndIndex = _end;

        this.Index = (this.StartIndex << 16) + this.EndIndex;
        this.Opposite = (this.EndIndex << 16) + this.StartIndex;

        this.CalculateArcLength();
        this.CalculateCordLength();
        this.CalculateDirection();
    }

    public float CalculateArcLength ( ) {
        float dot = Vector3.Dot(this.parentPlanet.TectonicPoints[this.StartIndex].Position, this.parentPlanet.TectonicPoints[this.EndIndex].Position);
        float angle = Mathf.Asin(dot);

        this.Length = angle * this.parentPlanet.planetSettings.PlanetRadius;
        return this.Length;
    }

    public float CalculateCordLength ( ) {
        this.CordLength = Vector3.Distance(this.parentPlanet.TectonicPoints[this.StartIndex].Position, this.parentPlanet.TectonicPoints[this.EndIndex].Position);
        return this.CordLength;
    }

    public float CalculateDirection ( ) {
        this.Direction = this.parentPlanet.TectonicPoints[this.StartIndex].GetDirectionFromPoint(this.parentPlanet.TectonicPoints[this.EndIndex].Position);
        return this.Direction;
    }

    public bool CalculateExternality ( )
    {
        if (this.parentTriangle.parentPlate.PlateIndex != this.parentPlanet.triangleSides[this.Opposite].parentTriangle.parentPlate.PlateIndex)
        {
            this.IsExternal = true;
        }
        else
        {
            this.IsExternal = false;
        }

        return this.IsExternal;
    }
}
