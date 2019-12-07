using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TectonicTriangle {

    private Planet parentPlanet;

    private int[] pointIndices;

    public TectonicPlate parentPlate { get; private set; }

    private TectonicTriangle[] edgeNeighbors;

    private List<TectonicTriangle> pointNeighbors;

    public float AverageDensity { get; private set; }

    public float AverageThickness { get; private set; }

    public float TriangleDirection { get; private set; }

    public float TriangleArea { get; private set; }

    public TectonicTriangle (Planet _parent, int _a, int _b, int _c) {
        this.parentPlanet = _parent;
        this.pointIndices = new int[] {_a, _b, _c};

        this.pointNeighbors = new List<TectonicTriangle>();

        this.SetAsPointParent();
    }

    private void SetAsPointParent ( ) {
        foreach (int point in this.pointIndices) {
            this.parentPlanet.TectonicPoints[point].SetParentTriangle(this);
        }
    }

    public void SetParentPlate (TectonicPlate _parent) {
        this.parentPlate = _parent;
    }

    public void CalculateNeighbors () {
        List<TectonicTriangle> edgeNeighbors = new List<TectonicTriangle>();

        // Go through all our points and get the triangles they're a part of.
        foreach (int point in this.pointIndices) {
            List<TectonicTriangle> triangles = new List<TectonicTriangle>(this.parentPlanet.TectonicPoints[point].parentTriangles);

            // Remove ourselves from the list.
            triangles.Remove(this);

            // Go through each and add it to our check lists.
            foreach (TectonicTriangle triangle in triangles) {
                // If the triangle was already found amongst all triangles sharing a point, it means it shares two points and is along an edge.
                if (this.pointNeighbors.Contains(triangle)) {
                    edgeNeighbors.Add(triangle);
                }

                this.pointNeighbors.Add(triangle);
            }
        }

        // Set our lists/arrays of neighbors.
        this.edgeNeighbors = edgeNeighbors.ToArray();
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
        this.pointIndices.CopyTo(array, 0);
        return array;
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