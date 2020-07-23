using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class Icosahedron : ISphereShape
{
    private List<Vector3> _points;
    public List<Vector3> Points => this._points;

    private List<int> _indices;
    public List<int> Indices => this._indices;

    private List<Triangle> _triangles;
    public List<Triangle> Triangles => this._triangles;

    public int VerticesCount => this._points.Count;

    public int TrianglesCount => this._triangles.Count;

    private int _subdivisions;

    private bool _recursiveSubdivide;

    private List<Vector3> _initialPoints;

    private List<Triangle> _initialTriangles;

    public Icosahedron() {
        // Initialize reference lists.
        this._points = new List<Vector3>();
        this._indices = new List<int>();
        this._triangles = new List<Triangle>();

        this._initialPoints = new List<Vector3>();
        this._initialTriangles = new List<Triangle>();
    }

    /// <summary>
    /// Initializes the parameters needed to generate an Icosahedron.
    /// </summary>
    /// <param name="subdivisions">The number of subdivisions each triangle should undergo.</param>
    /// <param name="recursiveSubdivide">Whether each triangle should have each side be subdivided in the
    ///     total subdivision amount (false), or if each triangle should be subdivided then go back through
    ///     the list and repeate until the total number of subdivisions has been reached (true).</param>
    public void Initialize (int subdivisions, bool recursiveSubdivide) {
        // Initialize generation parameters.
        this._subdivisions = subdivisions;
        this._recursiveSubdivide = recursiveSubdivide;
    }

    public void ResetSphere () {
        // Reset the lists on the icosahedron.
        this._points = new List<Vector3>();
        this._indices = new List<int>();
        this._triangles = new List<Triangle>();

        this._initialPoints = new List<Vector3>();
        this._initialTriangles = new List<Triangle>();
    }

    public void GenerateSphere () {
        Profiler.BeginSample("Generating Icosahedron");
        // Get the golden ratio.
        float g = 1 + (Mathf.Sqrt(5) / 2.0f);

        // Generate initial points.
        // Icosahedron is three rectangles in each plane with a length ratio equal to the golden ratio.
        for (int i = 0; i < 3; i++) {
            for (int j = 0; j < 4; j++) {
                Vector3 tempNextVector = Vector3.zero;
                tempNextVector[i] = 1 * (-1 + ((j % 2) * 2));
                tempNextVector[(i + 1) % 3] = g * (j > 1 ? -1 : 1);
                tempNextVector[(i + 2) % 3] = 0;

                this._initialPoints.Add(tempNextVector.normalized);
            }
        }

        // Generate intial triangles and indices. (Too lazy to come up with an algorithm for this right now).
        this._initialTriangles.Add(new Triangle(0, 11, 5));
        this._initialTriangles.Add(new Triangle(0, 5, 1));
        this._initialTriangles.Add(new Triangle(0, 1, 7));
        this._initialTriangles.Add(new Triangle(0, 7, 10));
        this._initialTriangles.Add(new Triangle(0, 10, 11));
        this._initialTriangles.Add(new Triangle(3, 9, 4));
        this._initialTriangles.Add(new Triangle(3, 4, 2));
        this._initialTriangles.Add(new Triangle(3, 2, 6));
        this._initialTriangles.Add(new Triangle(3, 6, 8));
        this._initialTriangles.Add(new Triangle(3, 8, 9));
        this._initialTriangles.Add(new Triangle(1, 5, 9));
        this._initialTriangles.Add(new Triangle(2, 4, 11));
        this._initialTriangles.Add(new Triangle(9, 5, 4));
        this._initialTriangles.Add(new Triangle(5, 11, 4));
        this._initialTriangles.Add(new Triangle(6, 2, 10));
        this._initialTriangles.Add(new Triangle(1, 8, 7));
        this._initialTriangles.Add(new Triangle(6, 7, 8));
        this._initialTriangles.Add(new Triangle(1, 9, 8));
        this._initialTriangles.Add(new Triangle(6, 10, 7));
        this._initialTriangles.Add(new Triangle(2, 11, 10));

        this._points = this._initialPoints;

        // If we're not planning to subdivide, set our indices now.
        if (this._subdivisions == 0) {
            this._triangles.AddRange(this._initialTriangles);
        }
        else {
            this.Subdivide(this._recursiveSubdivide);
        }

        for (int i = 0; i < this._triangles.Count; i++) {
            this._indices.AddRange(this._triangles[i].Indices);
        }

        //Debug.Log("Vert count: " + this._points.Count + " | Triangle count: " + this._triangles.Count);
        /*
        // Go through all the points to ensure we're reusing points properly.
        int close = 0;
        float dist = Vector3.Distance(this._initialPoints[2], this._initialPoints[11]) / 2f / (this._subdivisions + 1);

        for (int i = 0; i < this._points.Count; i++) {
            for (int j = i + 1; j < this._points.Count; j++) {
                float d = Vector3.Distance(this._points[i], this._points[j]);

                if (d < dist) {
                    close++;
                }
            }
        }

        Debug.Log(close + " points near each other.");
        */

        Profiler.EndSample();
    }

    private void Subdivide (bool recursiveDivision) {
        if (recursiveDivision) {
            Dictionary<int, Dictionary<short, int>> midPointCache = new Dictionary<int, Dictionary<short, int>>();

            // Add the intial triangles to our list.
            this._triangles.AddRange(this._initialTriangles);

            for (int i = 0; i < this._subdivisions; i++) {
                List<Triangle> tempTriangles = new List<Triangle>();
                foreach (Triangle tri in this._triangles) {
                    // Get the indices of the triangle.
                    int a = tri.Indices[0];
                    int b = tri.Indices[1];
                    int c = tri.Indices[2];

                    // Get the midpoints of the triangles, creating them if needed.
                    int ab = this.GetPointIndex(midPointCache, a, b);
                    int bc = this.GetPointIndex(midPointCache, b, c);
                    int ca = this.GetPointIndex(midPointCache, c, a);

                    // Create new triangles from the original triangle and the midpoints.               
                    tempTriangles.Add(new Triangle(a, ab, ca));
                    tempTriangles.Add(new Triangle(b, bc, ab));
                    tempTriangles.Add(new Triangle(c, ca, bc));
                    tempTriangles.Add(new Triangle(ab, bc, ca));
                }

                this._triangles = tempTriangles;
            }
        }
        else {
            Dictionary<int, Dictionary<short, int>> pointCache = new Dictionary<int, Dictionary<short, int>>();

            // Go through each triangle in the intial set.
            foreach (Triangle tri in this._initialTriangles) {
                // Partition the triangle.
                this._triangles.AddRange(this.PartitionTriangle(pointCache, tri));
            }

            // Go through each point and move it to its normalized position.
            for (int i = 0; i < this._points.Count; i++) {
                this._points[i] = this._points[i].normalized;
            }
        }
    }

    private int GetPointIndex (Dictionary<int, Dictionary<short, int>> cache, int indexA, int indexB, float percentAlongLine = 0.5f, bool normalize = true) {
        // We create a key out of the two original indices
        // by storing the smaller index in the upper two bytes
        // of an integer, and the larger index in the lower two
        // bytes. By sorting them according to whichever is smaller
        // we ensure that this function returns the same result
        // whether you call
        // GetMidPointIndex(cache, 5, 9)
        // or...
        // GetMidPointIndex(cache, 9, 5)
        // This assumes that the indexes never get above 2^16.
        int smallerIndex = Mathf.Min(indexA, indexB);
        int greaterIndex = Mathf.Max(indexA, indexB);
        int key = (smallerIndex << 16) + greaterIndex;
        int index;

        // Make sure we're consistently using the same percentage.
        if (indexA > indexB) {
            percentAlongLine = 1 - percentAlongLine;
        }

        // Remove floating point errors by rounding and capping the percentage.
        percentAlongLine = Mathf.Max(0, Mathf.Min(100, Mathf.RoundToInt(percentAlongLine * 100)));

        if (cache.ContainsKey(key)) {
            // If a midpoint is already defined, return it.        
            if (cache[key].TryGetValue((short) percentAlongLine, out index)) {
                return index;
            }

        }
        else {
            cache[key] = new Dictionary<short, int>();
        }

        // If no midpoint already exists, create a new one.
        Vector3 p1 = this._points[smallerIndex];
        Vector3 p2 = this._points[greaterIndex];
        Vector3 middle = Vector3.Lerp(p1, p2, percentAlongLine / 100f);

        if (normalize) {
            middle.Normalize();
        }

        // Add the new point to our location map and to the cache.
        index = this._points.Count;
        this._points.Add(middle);

        cache[key].Add((short) percentAlongLine, index);
        return index;
    }

    private List<Triangle> PartitionTriangle (Dictionary<int, Dictionary<short, int>> cache, Triangle baseTriangle) {
        List<Triangle> partitions = new List<Triangle>();

        // Get the indices of the triangle.
        int a = baseTriangle.Indices[0];
        int b = baseTriangle.Indices[1];
        int c = baseTriangle.Indices[2];

        // Find the step size along the line that we'll take.
        float stepSize = 1 / (float) (this._subdivisions + 1);

        // Break up the triangle edges to ensure that all the points along the edges have already been created.
        //  This prevents accidentally indexing an internal point or vice versa.
        for (int i = 0; i < this._subdivisions; i++) {
            this.GetPointIndex(cache, a, b, stepSize * (i + 1), false);
            this.GetPointIndex(cache, b, c, stepSize * (i + 1), false);
            this.GetPointIndex(cache, c, a, stepSize * (i + 1), false);
        }

        // Generate the first two points. These will be used to generate the rest of the triangles.
        int sIndex = this.GetPointIndex(cache, a, b, stepSize, false);
        int tIndex = this.GetPointIndex(cache, a, c, stepSize, false);

        // Get the offset from a for s and t to create internal points.
        Vector3 sOffset = this._points[sIndex] - this._points[a];
        Vector3 tOffset = this._points[tIndex] - this._points[a];

        // Add the first triangle, the corner of a, s, and t.
        //partitions.Add(new Triangle(a, sIndex, tIndex));

        int pointStartingOffset = this._points.Count;

        // Generate the internal points.
        for (int i = 1; i <= this._subdivisions; i++) {
            for (int j = 1; j <= this._subdivisions - i; j++) {
                this._points.Add(this._points[a] + (sOffset * i) + (tOffset * j));
            }
        }

        int trailingPoint = a;
        int midPoint = 0;
        int leadingPoint;
        int pointOffset = 0;

        // Generate the triangles partitioning this base triangle.
        for (int i = 0; i < this._subdivisions; i++) {

            // If we're not on the bottom row and just starting, set the trailingPoint to be along the a-b line.
            if (i != 0) {
                trailingPoint = this.GetPointIndex(cache, a, b, stepSize * i, false);
            }

            for (int j = 0; j < (this._subdivisions - i) * 2; j++) {
                bool invert = false;
                // If we're on the left-most triangle, the midPoint will always start on the a-b line.
                if (j == 0) {
                    midPoint = this.GetPointIndex(cache, a, b, stepSize * (i + 1), false);
                }

                // Get the current leadingPoint.
                //  See if we're on a triangle pointing up ( /\ ) or a triangle pointing down ( \/ ).
                if (j % 2 == 0) {

                    //  Triangle pointing up.  /\

                    //  If we're on the bottom row, the leadingPoint will be along the c-a line.
                    if (i == 0) {
                        leadingPoint = this.GetPointIndex(cache, c, a, stepSize * (this._subdivisions - Mathf.FloorToInt(j / 2)), false);
                    }
                    else {
                        // Otherwise we need to find the point internally on the previous row.
                        leadingPoint = pointStartingOffset + pointOffset - (this._subdivisions - i);
                    }

                }
                else {

                    //  Triangle pointing down.  \/

                    // If this is the last triangle in this row, the leadingPoint will now be on the b-c line.
                    invert = true;
                    if (j == ((this._subdivisions - i) * 2) - 1) {
                        leadingPoint = this.GetPointIndex(cache, b, c, stepSize * (this._subdivisions - i), false);
                        //Debug.Log("tp: " + trailingPoint + " | md: " + midPoint + " | lp: " + leadingPoint);
                    }
                    else {
                        // Otherwise add to the pointOffset and get the internal point.
                        leadingPoint = pointStartingOffset + pointOffset;
                        pointOffset++;
                    }

                }

                //Debug.Log("tp: " + trailingPoint + " | md: " + midPoint + " | lp: " + leadingPoint);
                //Debug.Log("tpP: " + this._points[trailingPoint] + " | mdP: " + this._points[midPoint] + " | lpP: " + this._points[leadingPoint]);

                if (invert) {
                    partitions.Add(new Triangle(trailingPoint, leadingPoint, midPoint));
                }
                else {
                    partitions.Add(new Triangle(trailingPoint, midPoint, leadingPoint));
                }
                trailingPoint = midPoint;
                midPoint = leadingPoint;

                // If we just finished creating this row, create the upward triangle at the end.
                if (j == ((this._subdivisions - i) * 2) - 1) {
                    // If this is the bottom row, the last triangle will use c.
                    if (i == 0) {
                        partitions.Add(new Triangle(trailingPoint, midPoint, c));
                    }
                    else {
                        // Otherwise the point will be found along c.
                        leadingPoint = this.GetPointIndex(cache, b, c, stepSize * (this._subdivisions - i + 1), false);
                        //Debug.Log("tp: " + trailingPoint + " | md: " + midPoint + " | lp: " + leadingPoint);
                        //Debug.Log("tpP: " + this._points[trailingPoint] + " | mdP: " + this._points[midPoint] + " | lpP: " + this._points[leadingPoint]);
                        partitions.Add(new Triangle(trailingPoint, midPoint, leadingPoint));
                    }
                }
            }
        }

        // When we have created all the triangles that pass internally, finish with the triangle that uses b.
        // trailingPoint will be on the a-b line.
        trailingPoint = this.GetPointIndex(cache, a, b, stepSize * this._subdivisions, false);
        // leadingPoint will be on the b-c line.
        leadingPoint = this.GetPointIndex(cache, b, c, stepSize, false);

        partitions.Add(new Triangle(trailingPoint, b, leadingPoint));
        
        return partitions;
    }

    public void RenderSphere (Mesh _meshToRender) {

        // Set the mesh values.
        _meshToRender.SetVertices(this._points);
        _meshToRender.SetIndices(this._indices, MeshTopology.Triangles, 0);

        // Recaculate mesh properties.
        _meshToRender.RecalculateBounds();
        _meshToRender.RecalculateNormals();
        _meshToRender.RecalculateTangents();
    }
}
