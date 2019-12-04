using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TectonicTriangle {

    public TectonicPoint[] points;

    public TectonicPlate parentPlate;

    private TectonicTriangle[] neighbors;

    public float AverageDensity { get; private set; }

    public float AverageThickness { get; private set; }

    public float TriangleDirection { get; private set; }

    public float TriangleArea { get; private set; }

    public TectonicTriangle (TectonicPoint _a, TectonicPoint _b, TectonicPoint _c) {
        this.points = new TectonicPoint[] {_a, _b, _c};

        this.SetAsPointParent();
    }

    private void SetAsPointParent ( ) {
        foreach (TectonicPoint point in this.points) {
            point.SetParentTriangle(this);
        }
    }

    public void SetParentPlate (TectonicPlate _parent) {
        this.parentPlate = _parent;
    }

    public void CalculateNeighbors () {
        List<TectonicTriangle> pointNeighbors = new List<TectonicTriangle>();
        List<TectonicTriangle> edgeNeighbors = new List<TectonicTriangle>();

        // Go through all our points and get the triangles they're a part of.
        foreach (TectonicPoint point in this.points) {
            List<TectonicTriangle> triangles = new List<TectonicTriangle>(point.parentTriangles);

            // Remove ourselves from the list.
            triangles.Remove(this);

            // Go through each and add it to our check lists.
            foreach (TectonicTriangle triangle in triangles) {
                // If the triangle was already found amongst all triangles sharing a point, it means it shares two points and is along an edge.
                if (pointNeighbors.Contains(triangle)) {
                    edgeNeighbors.Add(triangle);
                }

                pointNeighbors.Add(triangle);
            }
        }

        // Return the neighbors that share an edge.
        this.neighbors = edgeNeighbors.ToArray();
    }

    public TectonicTriangle[] GetNeighborTriangles () {
        return this.neighbors;
    }

    /// <summary>
    /// Returns the points that make up this triangle.
    /// </summary>
    /// <returns></returns>
    public TectonicPoint[] GetPoints () {
        TectonicPoint[] array = new TectonicPoint[3];
        this.points.CopyTo(array, 0);
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