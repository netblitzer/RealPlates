using UnityEngine;
using System.Collections;

public class HalfSide {

    private Planet parentPlanet;
    public TectonicTriangle parentTriangle { get; private set; }

    public TectonicPoint Start;
    public TectonicPoint End;
    public int StartIndex { get { return this.Start.Index; } }
    public int EndIndex { get { return this.End.Index; } }

    public float CordLength { get; private set; }
    public float Length { get; private set; }
    public float ArcLength { get { return this.Length; } }
    public Vector2 Direction { get; private set; }

    public int Opposite { get; private set; }

    public int Index { get; private set; }

    public bool IsExternal { get; private set; }
    public bool IsSubducting { get; private set; }

    public HalfSide (TectonicTriangle _parent, Planet _parentPlanet, TectonicPoint _start, TectonicPoint _end) {

        this.parentTriangle = _parent;
        this.parentPlanet = _parentPlanet;
        this.Start = _start;
        this.End = _end;

        this.Index = (this.StartIndex << 16) + this.EndIndex;
        this.Opposite = (this.EndIndex << 16) + this.StartIndex;

        this.IsSubducting = false;
        this.IsExternal = false;
    }

    public HalfSide (TectonicTriangle _parent, Planet _parentPlanet, int _start, int _end) :
        this(_parent, _parentPlanet, _parentPlanet.dep__TectonicPoints[_start], _parentPlanet.dep__TectonicPoints[_end]) { }

    public void SetDirection (Vector2 _direction) {
        this.Direction = _direction;
    }

    public void SetLength(float _arcLength, float _cordLength) {
        this.Length = _arcLength;
        this.CordLength = _cordLength;
    }

    public float CalculateArcLength ( ) {
        float dot = Vector3.Dot(this.Start.SpherePosition, this.End.SpherePosition);
        float angle = Mathf.Asin(dot);

        this.Length = angle;
        return this.Length;
    }

    public float CalculateCordLength ( ) {
        this.CordLength = Vector3.Distance(this.Start.SpherePosition, this.End.SpherePosition);
        return this.CordLength;
    }

    public Vector2 CalculateDirection ( ) {
        // Calculate the normal direciton of the halfside. Make sure we use the same
        //  start and end for both halfsides in order to clear out problems.
        this.Direction = this.parentPlanet.dep__TectonicPoints[Mathf.Max(this.StartIndex, this.EndIndex)].GetDirectionToPoint(this.parentPlanet.dep__TectonicPoints[Mathf.Min(this.StartIndex, this.EndIndex)].SpherePosition);
        // If the end is larger than the start, invert the normal so both sides have opposite directions.
        this.Direction = (this.StartIndex > this.EndIndex) ? this.Direction : this.Direction * -1f;
        return this.Direction;
    }

    public bool CalculateExternality ( )
    {
        if (this.parentTriangle.parentPlate.PlateIndex != this.parentPlanet.dep__triangleSides[this.Opposite].parentTriangle.parentPlate.PlateIndex)
        {
            this.IsExternal = true;
        }
        else
        {
            this.IsExternal = false;
        }

        return this.IsExternal;
    }

    public void TestRender () {
        Gizmos.color = new Color(Mathf.Abs(this.Direction.x), Mathf.Abs(this.Direction.y), 0);
        Gizmos.DrawLine(this.Start.SpherePosition, this.End.SpherePosition);
    }

    public void CalculateSubduction ( ) {

        // If we're already subducting under our opposite, we can't stop subducting.
        if (this.IsSubducting) {
            return;
        }

        // If our opposite is subducting under us, we can't subduct.
        HalfSide oppositeSide = this.parentPlanet.dep__triangleSides[this.Opposite];
        if (oppositeSide.IsSubducting) {
            return;
        }

        // If we're not an external side, we can't subduct under our own plate.
        if (!this.IsExternal) {
            return;
        }

        // If our triangle is on average thicker than the max, we cannot subduct.
        if (this.parentTriangle.AverageThickness > this.parentPlanet.planetSettings.plateSettings.SubductionThicknessLimit) {
            return;
        }

        // See if our triangle is heading towards the opposite triangle enough to be colliding.
        Vector2 inverseNormal = new Vector2(-this.Direction.y, this.Direction.x);
        float velocityInline = Vector2.Dot(inverseNormal, this.parentTriangle.LateralVelocity);

        if (velocityInline > this.parentPlanet.planetSettings.plateSettings.SubductionDirectionRequirement) {
            // Calculate other triangles velocity towards us.
            float oppositeVelocityInline = Vector2.Dot(inverseNormal, oppositeSide.parentTriangle.LateralVelocity);

            // If our triangle is moving towards the other triangle faster than it moves away 
            //  and the density difference is above the threshold, we're subducting.
            if (velocityInline > oppositeVelocityInline 
                && this.parentTriangle.AverageDensity - oppositeSide.parentTriangle.AverageDensity > this.parentPlanet.planetSettings.plateSettings.SubductionDensityDifferenceRequirement) {
                this.IsSubducting = true;
                return;
            }
        }
    }
}
