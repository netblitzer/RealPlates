using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointOctree
{
    // The current point that's contained by the octree.
    Vector3 currentPoint;
    // The immediate subdivisions of this octree.
    PointOctree[] subDivisions;
    // The boundary that this octTree is defined by.
    public CubeBoundary Boundary { get; private set; }

    public PointOctree ( float _side ) : this(Vector3.zero, _side) { }
    
    public PointOctree ( Vector3 _center, float _side ) {
        this.Boundary = new CubeBoundary(_center, _side);
        this.currentPoint = new Vector3(float.NaN, float.NaN);
    }

    public PointOctree ( CubeBoundary _boundary ) {
        this.Boundary = _boundary;
        this.currentPoint = new Vector3(float.NaN, float.NaN);
    }

    /// <summary>
    /// Attempts to insert a point into the octree. If it has to, it will subdivide and insert the point
    /// into a lower subdivision.
    /// </summary>
    /// <param name="_point">The point to insert into the octree.</param>
    /// <returns>True if the point is inserted into the octree successfully.</returns>
    public bool InsertPoint ( Vector3 _point ) {
        // Make sure the point exists.
        if (_point == null) {
            return false;
        }

        // Make sure the point is contained within this octTree.
        if (!this.Boundary.ContainsPoint(_point)) {
            return false;
        }

        // If we have no subdivisions and the current point is empty, set this to our point.
        if (this.subDivisions == null && float.IsNaN(this.currentPoint.x)) {
            this.currentPoint = _point;
            return true;
        }

        // If we already have a point but don't have subdivisions yet, subdivide this octTree.
        if (this.subDivisions == null) {
            this.Subdivide();
        }

        // Once we know that we need to place it into a subdivision, try inserting it into all of them.
        for (int i = 0; i < 8; i++) {
            if (this.subDivisions[i].InsertPoint(_point)) {
                return true;
            }
        }

        // Fall through return.
        return false;
    }

    private void Subdivide ( ) {
        // Create the array of subdivisions.
        this.subDivisions = new PointOctree[8];

        // Easy access values.
        Vector3 ourCenter = this.Boundary.Center;
        float ourSide = this.Boundary.SideLength;
        float newSide = ourSide / 2f;

        // Create new octrees at each corner of the cube.
        for (int i = 0; i < 2; i++) {
            for (int j = 0; j < 2; j++) {
                for (int k = 0; k < 2; k++) {
                    Vector3 octCenter = new Vector3(ourCenter.x - (newSide / 2f) + (newSide * i),
                        ourCenter.y - (newSide / 2f) + (newSide * j),
                        ourCenter.z - (newSide / 2f) + (newSide * k));
                    CubeBoundary newBoundary = new CubeBoundary(octCenter, newSide);
                    this.subDivisions[(i * 4) + (j * 2) + k] = new PointOctree(newBoundary);
                }
            }
        }

        // Insert the point that was originally contained by the parent octree into one of the subdivisions.
        for (int i = 0; i < 8; i++) {
            if (this.subDivisions[i].InsertPoint(this.currentPoint)) {
                this.currentPoint = new Vector3(float.NaN, float.NaN);
                return;
            }
        }
    }

    public bool SphereIntersectsContainedPoints ( Vector3 _center, float _radius ) {
        if (this.Boundary.IntersectsSphere(_center, _radius)) {
            if (!float.IsNaN(this.currentPoint.x)) {
                if (Vector3.Distance(_center, this.currentPoint) < _radius) {
                    return true;
                }
            }

            if (this.subDivisions != null) {
                bool testResults = false;
                for (int i = 0; i < 8; i++) {
                    bool test = this.subDivisions[i].SphereIntersectsContainedPoints(_center, _radius);
                    if (test) {
                        testResults = test;
                    }
                }
                return testResults;
            }
        }

        return false;
    }
}