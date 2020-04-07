using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HalfSidePair 
{
    private Planet parentPlanet;

    public int Index;

    // The two halfside indexes that make the pair.
    private int firstHalf;
    private int secondHalf;

    // The length around the sphere of this halfside pair.
    public float ArcLength { get; private set; }
    // The direct length between the points making up this halfside pair.
    public float CordLength { get; private set; }

    public HalfSideStatusType HalfSideStatus;

    public float ConnectionAge;

    public float ConnectionStrength { get {
            if (this.HalfSideStatus == HalfSideStatusType.Connected) {
                float maxAgePercent = this.ConnectionAge / this.parentPlanet.planetSettings.plateSettings.HalfSideConnectionMaxStrengthAge;

                return ((1 - maxAgePercent) * this.parentPlanet.planetSettings.plateSettings.HalfSideConnectionStartingStrength) + 
                    (maxAgePercent * this.parentPlanet.planetSettings.plateSettings.HalfSideConnectionEndStrength);
            }
            else {
                return 0;
            }
        } }

    public HalfSidePair (Planet _parent, int _index, int _side1, int _side2) {
        this.parentPlanet = _parent;
        this.Index = _index;

        this.firstHalf = _side1;
        this.secondHalf = _side2;

        // Generically all sides will start as "connecting" until we know more.
        this.HalfSideStatus = HalfSideStatusType.Connecting;
    }

    /// <summary>
    /// Calculates the direction and lengths of the halfsides that make up this pair.
    /// </summary>
    public void CalculateHalfSideProperties () {
        HalfSide side1 = this.parentPlanet.triangleSides[this.firstHalf];
        HalfSide side2 = this.parentPlanet.triangleSides[this.secondHalf];

        // Calculate the normal direciton of the first halfside.
        Vector2 direction = side1.Start.GetDirectionToPoint(side1.End.SpherePosition);

        // Set the directions of the halfsides.
        side1.SetDirection(direction);
        side2.SetDirection(-direction);

        // Calculate the lengths of the halfsides.
        //  Calculate the arc length (we can use the angle since this is a unit sphere).
        float dot = Vector3.Dot(side1.Start.SpherePosition, side1.End.SpherePosition);
        float arc = Mathf.Acos(dot);
        // Calculate the cord length.
        float cord = Vector3.Distance(side1.Start.SpherePosition, side1.End.SpherePosition);

        // Set the lengths of the halfsides.
        side1.SetLength(arc, cord);
        side2.SetLength(arc, cord);
    }

    /// <summary>
    /// Determines what sort of event is going on at the pair of halfsides. This finds
    /// if they're converging and whether or not one is subducting, if they're diverging, 
    /// if they're creating a transform fault, or if they're connecting or connected.
    /// </summary>
    public void CalculateHalfSideStatus () {
        // Get our halfsides and their parent triangles.
        HalfSide side1 = this.parentPlanet.triangleSides[this.firstHalf];
        HalfSide side2 = this.parentPlanet.triangleSides[this.secondHalf];

        TectonicTriangle triangle1 = side1.parentTriangle;
        TectonicTriangle triangle2 = side2.parentTriangle;

        // Get the amount the two triangle velocities are perpendicular to their opposing halfsides.
        float tri1VelocityInline = Vector2.Dot(triangle1.LateralVelocity.normalized, side2.Direction);
        float tri2VelocityInline = Vector2.Dot(triangle2.LateralVelocity.normalized, side1.Direction);

        // Get the difference in elevation and density between the two triangles non-shared points.
        float elevationDifference = Mathf.Abs(triangle1.GetOtherPoint(side1.Start, side1.End).GetElevation() -
            triangle2.GetOtherPoint(side1.Start, side1.End).GetElevation());
        float densityDifference = Mathf.Abs(triangle1.GetOtherPoint(side1.Start, side1.End).density -
            triangle2.GetOtherPoint(side1.Start, side1.End).density);

        // Go through all the possible interactions that could be happening at this
        //  halfside pair based on the current status.
        switch (this.HalfSideStatus) {
            case HalfSideStatusType.Connecting:

                break;
            case HalfSideStatusType.Connected:

                break;
            case HalfSideStatusType.Diverging:

                break;
            case HalfSideStatusType.Colliding:

                break;
            case HalfSideStatusType.Subducting:

                break;
            case HalfSideStatusType.Transform:

                break;
        }


        /*
        // Check for whether the status of the pair has already been determined.
        // If we're already subducting under our opposite, we can't stop subducting.
        if (this.IsSubducting) {
            return;
        }

        // If our opposite is subducting under us, we can't subduct.
        HalfSide oppositeSide = this.parentPlanet.triangleSides[this.Opposite];
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
        */
    }

    public void TestRender () {
        HalfSide side1 = this.parentPlanet.triangleSides[this.firstHalf];

        switch (this.HalfSideStatus) {
            case HalfSideStatusType.Connecting:
                Gizmos.color = Color.yellow;
                break;
            case HalfSideStatusType.Connected:
                Gizmos.color = Color.green;
                break;
            case HalfSideStatusType.Diverging:
                Gizmos.color = Color.blue;
                break;
            case HalfSideStatusType.Colliding:
                Gizmos.color = Color.red;
                break;
            case HalfSideStatusType.Subducting:
                Gizmos.color = new Color(1, 0.4f, 0);
                break;
            case HalfSideStatusType.Transform:
                Gizmos.color = Color.black;
                break;
        }

        Gizmos.DrawLine(side1.Start.SpherePosition, side1.End.SpherePosition);
    }
}
