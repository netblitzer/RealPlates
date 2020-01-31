using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TectonicTriangle {

    private Planet parentPlanet;

    public int[] PointIndices { get; private set; }

    public int[] SideIndices { get; private set; }

    public TectonicPlate parentPlate { get; private set; }

    private TectonicTriangle[] edgeNeighbors;

    private List<TectonicTriangle> pointNeighbors;

    public float AverageDensity { get; private set; }

    public float AverageThickness { get; private set; }

    public float TriangleDirection { get; private set; }

    public float TriangleArea { get; private set; }

    public bool InternalTriangle { get; private set; }

    public TectonicTriangle (Planet _parent, int _a, int _b, int _c) {
        this.parentPlanet = _parent;
        this.PointIndices = new int[] {_a, _b, _c};

        this.pointNeighbors = new List<TectonicTriangle>();
        this.edgeNeighbors = new TectonicTriangle[3];

        this.SetAsPointParent();

        this.SideIndices = new int[3];
        this.CreateHalfSides();
    }

    private void CreateHalfSides ( )
    {
        // Go through the three sides of the triangle and create the half sides.
        for (int i = 0; i < 3; i++)
        {
            int j = (i + 1) % 3;

            HalfSide newSide = new HalfSide(this, this.parentPlanet, this.PointIndices[i], this.PointIndices[j]);
            this.SideIndices[i] = newSide.Index;
            this.parentPlanet.SetHalfSide(newSide);
        }
    }

    private void SetAsPointParent ( ) {
        foreach (int point in this.PointIndices) {
            this.parentPlanet.TectonicPoints[point].SetParentTriangle(this);
        }
    }

    public void SetParentPlate (TectonicPlate _parent) {
        this.parentPlate = _parent;
    }

    public void CalculateNeighbors () {
        // Grab the neighbor triangles (the opposite halfside to ours).
        for (int i = 0; i < 3; i++)
        {
            this.edgeNeighbors[i] = this.parentPlanet.triangleSides[this.parentPlanet.triangleSides[this.SideIndices[i]].Opposite].parentTriangle;
        }
    }

    public void CalculateInternalStatus ( )
    {
        // Intialize as internal.
        this.InternalTriangle = true;

        // Go through each of our halfsides and see if they're external.
        for (int i = 0; i < 3; i++)
        {
            this.parentPlanet.triangleSides[this.SideIndices[i]].CalculateExternality();

            // Also grab the neighbor triangles (the opposite halfside to ours).
            this.edgeNeighbors[i] = this.parentPlanet.triangleSides[this.parentPlanet.triangleSides[this.SideIndices[i]].Opposite].parentTriangle;

            // If the neighbor isn't in our plate, set the triangle as external.
            if (this.edgeNeighbors[i].parentPlate.PlateIndex != this.parentPlate.PlateIndex)
            {
                this.InternalTriangle = false;
            }
        }
    }

    public TectonicTriangle[] GetNeighborTriangles () {
        return this.edgeNeighbors;
    }

    /// <summary>
    /// Returns the point indices that make up this triangle.
    /// </summary>
    /// <returns></returns>
    public int[] GetPoints () {
        int[] array = new int[3];
        this.PointIndices.CopyTo(array, 0);
        return array;
    }

    public float CalculateTriangleArea () {
        Vector3 ab = this.parentPlanet.TectonicPoints[this.PointIndices[1]].Position - this.parentPlanet.TectonicPoints[this.PointIndices[0]].Position;
        Vector3 bc = this.parentPlanet.TectonicPoints[this.PointIndices[2]].Position - this.parentPlanet.TectonicPoints[this.PointIndices[1]].Position;

        this.TriangleArea = Vector3.Magnitude(Vector3.Cross(ab, bc)) / 2f;
        return this.TriangleArea;
    }
}

//  Triangle:
//      Variables:
//          Points (array of 3 points): the points that make up the triangle.
//          ParentPlate (plate): the plate that this triangle is contained in.
//          AverageDensity (float): average height of the points of the triangle.
//          AverageThickness (float): average thickness of the points.
//          TriangleDirection (float): the dot product of segments a-b and b-c. Used to see if the triangle ever flips.
//          TriangleArea (float): the total area taken up by the triangle.
//      Methods:
//          CalculateTriangleDirection () {float}: returns the current direction of the triangle.
//          CalculateTriangleArea () {float}: returns the total area of the triangle.