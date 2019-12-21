using UnityEngine;
using System.Collections;

public class HalfSide {

    private Planet parentPlanet;

    public int StartIndex;
    public int EndIndex;

    public float Direction;
    public float Length;

    public HalfSide Opposite;

    public HalfSide (Planet _parent, int _start, int _end) {

    }

    public float CaclulateLength ( ) {
        float dot = Vector3.Dot(this.parentPlanet.TectonicPoints[this.StartIndex].Position, this.parentPlanet.TectonicPoints[this.EndIndex].Position);
        float angle = Mathf.Asin(dot);

        this.Length = angle * this.parentPlanet.PlanetRadius;
        return this.Length;
    }

    public float CaculateDirection ( ) {
        this.Direction = this.parentPlanet.TectonicPoints[this.StartIndex].GetDirectionFromPoint(this.parentPlanet.TectonicPoints[this.EndIndex].Position);
        return this.Direction;
    }
}
