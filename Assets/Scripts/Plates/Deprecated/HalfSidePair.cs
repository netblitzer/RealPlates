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

    public float Inclination { get; private set; }

    public float Longitude { get; private set; }

    public Vector3 PlaneNormal { get; private set; }

    public HalfSideStatusType HalfSideStatus;

    public float ConnectionAge;

    public float Tension;

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

        // Calculate the plane that the halfsidepair passes through.
        this.PlaneNormal = Vector3.Cross(side1.Start.SpherePosition, side1.End.SpherePosition).normalized;

        // Get the inclination of the plane.
        this.Inclination = Mathf.Acos(Vector3.Dot(this.PlaneNormal, Vector3.up));

        // Calculate the intersection normal.
        Vector3 intersection = Vector3.Cross(this.PlaneNormal, Vector3.up).normalized;
        this.Longitude = Mathf.Acos(Vector3.Dot(intersection, Vector3.forward));
    }

    /// <summary>
    /// Determines what sort of event is going on at the pair of halfsides. This finds
    /// if they're converging and whether or not one is subducting, if they're diverging, 
    /// if they're creating a transform fault, or if they're connecting or connected.
    /// </summary>
    public void CalculateHalfSideStatus (float _ageStep) {
        // Get our halfsides and their parent triangles.
        HalfSide side1 = this.parentPlanet.triangleSides[this.firstHalf];
        HalfSide side2 = this.parentPlanet.triangleSides[this.secondHalf];

        TectonicTriangle triangle1 = side1.parentTriangle;
        TectonicTriangle triangle2 = side2.parentTriangle;

        // Get the amount the two triangle velocities are perpendicular to their opposing halfsides.
        float tri1VelocityPerp = Vector2.Dot(triangle1.LateralVelocity.normalized, side2.Direction);
        float tri2VelocityPerp = Vector2.Dot(triangle2.LateralVelocity.normalized, side1.Direction);
        float tri1VelocityInline = (1 - Mathf.Abs(tri1VelocityPerp)) * Mathf.Sign(tri1VelocityPerp);
        float tri2VelocityInline = (1 - Mathf.Abs(tri2VelocityPerp)) * Mathf.Sign(tri2VelocityPerp);

        // Get the combined perpendicular and inline velocities.
        float combinedPerp = tri1VelocityPerp + tri2VelocityPerp;
        float combinedInline = tri1VelocityInline + tri2VelocityInline;

        // Get the true combined velocities of the triangles.
        Vector2 combinedVelocity = triangle1.LateralVelocity - triangle2.LateralVelocity;

        // Get the sign of the velocities. If it's positive, they're moving towards or away from
        //  each other. Negative is moving the same direction.
        int combinedDirection = (int) (Mathf.Sign(tri1VelocityPerp) * Mathf.Sign(tri2VelocityPerp));

        // Get the difference in elevation and density between the two triangles non-shared points.
        float elevationDifference = triangle1.GetOtherPoint(side1.Start, side1.End).GetElevation() -
            triangle2.GetOtherPoint(side1.Start, side1.End).GetElevation();
        float densityDifference = triangle1.GetOtherPoint(side1.Start, side1.End).density -
            triangle2.GetOtherPoint(side1.Start, side1.End).density;


        // Go through all the possible interactions that could be happening at this
        //  halfside pair based on the current status.

        // See if the triangles are moving fast enough to cause any interactions to occur.
        if (combinedVelocity.magnitude > this.parentPlanet.planetSettings.plateSettings.TriangleVelocityDifferenceInteractionThreshold) {
            // See if the triangles are moving in opposite directions.
            if (combinedDirection == 1) {
                // If the triangles are moving in opposite directions, check to see if they're moving
                //  towards or away from each other.
                if (Mathf.Sign(tri1VelocityPerp) == 1 && Mathf.Sign(tri2VelocityPerp) == 1) {
                    // The triangles are both moving towards each other.
                    this.HalfSideStatus = HalfSideStatusType.Colliding;

                    // See if either triangle is low enough to subduct.
                    if (triangle1.GetOtherPoint(side1.Start, side1.End).GetElevation() < this.parentPlanet.planetSettings.plateSettings.SubductionThicknessLimit ||
                        triangle2.GetOtherPoint(side1.Start, side1.End).GetElevation() < this.parentPlanet.planetSettings.plateSettings.SubductionThicknessLimit) {
                        // See if the triangles have different enough elevations or densities to subduct.
                        if (Mathf.Abs(elevationDifference) >= this.parentPlanet.planetSettings.plateSettings.SubductionElevationDifferenceRequirement ||
                            Mathf.Abs(densityDifference) >= this.parentPlanet.planetSettings.plateSettings.SubductionDensityDifferenceRequirement) {
                            // If the elevation or density is different enough, find out which triangle can
                            //  subduct. Positive elevationDifference means triangle2 is lower.
                        }
                        else {
                            // If we're colliding and one triangle could subduct but the triangles are too
                            //  similar, build tension to reduce the difference requirements.
                            this.Tension += _ageStep;
                        }
                    }
                }
                else {
                    // The triangles are moving away from each other.
                    this.HalfSideStatus = HalfSideStatusType.Diverging;
                }
            }
            else {
                // If the triangles are moving in the same direction, see which direction they're going
                //  and whether one is pulling away or the other catching up.
                this.HalfSideStatus = HalfSideStatusType.Subducting;
            }
        }
        else {
            // If the triangles aren't moving fast enough to interact, they should fuse/connect.
            if (this.HalfSideStatus != HalfSideStatusType.Connected) {
                this.ConnectionAge += _ageStep;
                this.HalfSideStatus = HalfSideStatusType.Connecting;
            }

            // See if the age of the connection is enough to have connected/fused together.
            if (this.ConnectionAge > this.parentPlanet.planetSettings.plateSettings.HalfSideConnectionAgeThreshold) {
                this.HalfSideStatus = HalfSideStatusType.Connected;

                // Combine the plates of the triangles together if they don't share a plates (they shouldn't).
                if (triangle1.parentPlate.PlateIndex != triangle2.parentPlate.PlateIndex) {
                    int primaryPlate, secondaryPlate;

                    // Prefer to keep the larger plate.
                    if (triangle1.parentPlate.TriangleCount >= triangle2.parentPlate.TriangleCount) {
                        primaryPlate = triangle1.parentPlate.PlateIndex;
                        secondaryPlate = triangle2.parentPlate.PlateIndex;
                    }
                    else {
                        primaryPlate = triangle2.parentPlate.PlateIndex;
                        secondaryPlate = triangle1.parentPlate.PlateIndex;
                    }

                    // Combine the plates and remove the excess one.
                    this.parentPlanet.TectonicPlates[primaryPlate].CombinePlates(this.parentPlanet.TectonicPlates[secondaryPlate]);
                    this.parentPlanet.RemovePlate(secondaryPlate);
                }
            }
        }


        /*
        switch (this.HalfSideStatus) {
            case HalfSideStatusType.Connecting:
                // See if the triangles are moving fast enough to cause any interactions to occur.
                if (combinedVelocity.magnitude > this.parentPlanet.planetSettings.plateSettings.TriangleVelocityDifferenceInteractionThreshold) {
                    // See if the triangles are moving in opposite directions.
                    if (combinedDirection == 1) {
                        // If the triangles are moving in opposite directions, check to see if they're moving
                        //  towards or away from each other.
                        if (Mathf.Sign(tri1VelocityPerp) == 1 && Mathf.Sign(tri2VelocityPerp) == 1) {
                            // The triangles are both moving towards each other.
                            this.HalfSideStatus = HalfSideStatusType.Colliding;
                        }
                        else {
                            // The triangles are moving away from each other.
                            this.HalfSideStatus = HalfSideStatusType.Diverging;
                        }
                    }
                    else {
                        // If the triangles are moving in the same direction, see which direction they're going
                        //  and whether one is pulling away or the other catching up.
                        this.HalfSideStatus = HalfSideStatusType.Subducting;
                    }
                }
                else {
                    // If the triangles aren't moving fast enough to interact, they should fuse/connect.
                    if (this.HalfSideStatus != HalfSideStatusType.Connected) {
                        this.ConnectionAge += _ageStep;
                        this.HalfSideStatus = HalfSideStatusType.Connecting;
                    }

                    // See if the age of the connection is enough to have connected/fused together.
                    if (this.ConnectionAge > this.parentPlanet.planetSettings.plateSettings.HalfSideConnectionAgeThreshold) {
                        this.HalfSideStatus = HalfSideStatusType.Connected;

                        // Combine the plates of the triangles together if they don't share a plates (they shouldn't).
                        if (triangle1.parentPlate.PlateIndex != triangle2.parentPlate.PlateIndex) {
                            int primaryPlate, secondaryPlate;

                            // Prefer to keep the larger plate.
                            if (triangle1.parentPlate.TriangleCount >= triangle2.parentPlate.TriangleCount) {
                                primaryPlate = triangle1.parentPlate.PlateIndex;
                                secondaryPlate = triangle2.parentPlate.PlateIndex;
                            }
                            else {
                                primaryPlate = triangle2.parentPlate.PlateIndex;
                                secondaryPlate = triangle1.parentPlate.PlateIndex;
                            }

                            // Combine the plates and remove the excess one.
                            this.parentPlanet.TectonicPlates[primaryPlate].CombinePlates(this.parentPlanet.TectonicPlates[secondaryPlate]);
                            this.parentPlanet.RemovePlate(secondaryPlate);
                        }
                    }
                }
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
        */


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
