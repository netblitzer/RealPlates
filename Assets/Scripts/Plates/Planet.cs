using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class Planet : MonoBehaviour {

    /*
    PoissonRandom poisson;
    public GameObject pointPrefab;
    public List<GameObject> randomPoints;
    Octree tree;

    public float Radius = 10f;
    public float MinimumDistance = 0.5f;
    public int MaxAttemptsPerPoint = 10;

    public int NumberOfPoints = 100;
    public float jitter = 0f;

    // Start is called before the first frame update
    void Initialize ( ) {
        if (this.poisson == null)
            this.poisson = new PoissonRandom();

        if (this.randomPoints == null)
            this.randomPoints = new List<GameObject>();
        else {
            this.randomPoints.Clear();
            while (this.transform.childCount > 0) {
                Transform point = this.transform.GetChild(0);
                DestroyImmediate(point.gameObject);
            }
        }
    }

    public void GeneratePlanet ( ) {
        this.Initialize();

        //List<Vector3> points = this.poisson.PoissonInsideSphere(this.MinimumDistance, this.MaxAttemptsPerPoint, this.transform.position, this.Radius, out this.tree);
        List<Vector3> points = FibonacciSphere.GenerateFibonacciSmoother(this.NumberOfPoints, 0, 0);

        foreach (Vector3 point in points) {
            // Go through each point and push it to the edge of the sphere.
            Vector3 unitPoint = point.normalized * this.Radius;

            GameObject newRandom = Instantiate(this.pointPrefab);
            newRandom.transform.parent = this.transform;
            newRandom.transform.localPosition = unitPoint;
            newRandom.name = "Point #" + (this.randomPoints.Count + 1);
            this.randomPoints.Add(newRandom);
        }
    }

    */

    public GameObject MantleObject;
    public Mantle mantle;

    public GameObject CoreObject;

    public int SubDivisions = 2;
    public float Jitter = 0f;
    public float PlanetRadius = 10f;

    public float CoreRadius = 1f;
    public float MantleRadius = 9.5f;
    public float MantlePointDensity = 1f;

    public int MinPlateCount = 5;
    public int MaxPlateCount = 10;

    private List<TectonicPoint> tectonicPoints;
    private List<TectonicTriangle> tectonicTriangles;
    private List<TectonicPlate> tectonicPlates;

    public GameObject PlatePrefab;

    public bool GenerateRandomAxis = true;
    public Vector3 RotationAxis = Vector3.up;
    public float RotationSpeed = 15f;           // 15 degrees per second.

    // For generation purposes.
    private List<Vector3> tempPositions;
    private List<Triangle> tempTriangles;

    private void Initialize () {
        this.tempTriangles = new List<Triangle>();
        this.tempPositions = new List<Vector3>();

        this.tectonicPoints = new List<TectonicPoint>();
        this.tectonicTriangles = new List<TectonicTriangle>();
        this.tectonicPlates = new List<TectonicPlate>();

        if (this.GenerateRandomAxis) {
            this.RotationAxis = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        }

        while(this.transform.childCount > 0) {
            Transform child = this.transform.GetChild(0);
            DestroyImmediate(child.gameObject);
        }
    }

    #region IntialGeneration

    // This information is from the tutorial here:
    // https://medium.com/@peter_winslow/creating-procedural-planets-in-unity-part-1-df83ecb12e91


    private void AddIntialPoints ( ) {
        // An icosohedron is 3 orthogonal rectangles, thus we can share point information.
        float t = (1.0f + Mathf.Sqrt(5.0f)) / 2.0f;

        // Generate intial triangles vertex locations.
        this.tempPositions.Add(new Vector3(-1, t, 0).normalized );
        this.tempPositions.Add(new Vector3(1, t, 0).normalized  );
        this.tempPositions.Add(new Vector3(-1, -t, 0).normalized);
        this.tempPositions.Add(new Vector3(1, -t, 0).normalized );
        this.tempPositions.Add(new Vector3(0, -1, t).normalized );
        this.tempPositions.Add(new Vector3(0, 1, t).normalized  );
        this.tempPositions.Add(new Vector3(0, -1, -t).normalized);
        this.tempPositions.Add(new Vector3(0, 1, -t).normalized );
        this.tempPositions.Add(new Vector3(t, 0, -1).normalized );
        this.tempPositions.Add(new Vector3(t, 0, 1).normalized  );
        this.tempPositions.Add(new Vector3(-t, 0, -1).normalized);
        this.tempPositions.Add(new Vector3(-t, 0, 1).normalized );

        // Generate intial triangles and indices.
        this.tempTriangles.Add(new Triangle(0, 11, 5));
        this.tempTriangles.Add(new Triangle(0, 5, 1));
        this.tempTriangles.Add(new Triangle(0, 1, 7));
        this.tempTriangles.Add(new Triangle(0, 7, 10));
        this.tempTriangles.Add(new Triangle(0, 10, 11));
        this.tempTriangles.Add(new Triangle(1, 5, 9));
        this.tempTriangles.Add(new Triangle(5, 11, 4));
        this.tempTriangles.Add(new Triangle(11, 10, 2));
        this.tempTriangles.Add(new Triangle(10, 7, 6));
        this.tempTriangles.Add(new Triangle(7, 1, 8));
        this.tempTriangles.Add(new Triangle(3, 9, 4));
        this.tempTriangles.Add(new Triangle(3, 4, 2));
        this.tempTriangles.Add(new Triangle(3, 2, 6));
        this.tempTriangles.Add(new Triangle(3, 6, 8));
        this.tempTriangles.Add(new Triangle(3, 8, 9));
        this.tempTriangles.Add(new Triangle(4, 9, 5));
        this.tempTriangles.Add(new Triangle(2, 4, 11));
        this.tempTriangles.Add(new Triangle(6, 2, 10));
        this.tempTriangles.Add(new Triangle(8, 6, 7));
        this.tempTriangles.Add(new Triangle(9, 8, 1));
    }

    private void Subdivide ( int _divisions ) {
        var midPointCache = new Dictionary<int, int>();

        for (int i = 0; i < _divisions; i++) {
            var newTris = new List<Triangle>();
            foreach (var tri in this.tempTriangles) {
                // Get the indices of the triangle.
                int a = tri.Indices[0];
                int b = tri.Indices[1];
                int c = tri.Indices[2];

                // Get the midpoints of the triangles, creating them if needed.
                int ab = this.GetMidPointIndex(midPointCache, a, b);
                int bc = this.GetMidPointIndex(midPointCache, b, c);
                int ca = this.GetMidPointIndex(midPointCache, c, a);

                // Create new triangles from the original triangle and the midpoints.               
                newTris.Add(new Triangle(a, ab, ca));
                newTris.Add(new Triangle(b, bc, ab));
                newTris.Add(new Triangle(c, ca, bc));
                newTris.Add(new Triangle(ab, bc, ca));
            }

            // Replace all our old polygons with the new set of subdivided ones.
            this.tempTriangles = newTris;
        }
    }

    private int GetMidPointIndex ( Dictionary<int, int> cache, int indexA, int indexB ) {
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
        int smallerIndex = Mathf.Min (indexA, indexB);
        int greaterIndex = Mathf.Max(indexA, indexB);
        int key = (smallerIndex << 16) + greaterIndex;

        // If a midpoint is already defined, return it.        
        if (cache.TryGetValue(key, out int index)) {
            return index;
        }

        // If no midpoint already exists, create a new one.
        Vector3 p1 = this.tempPositions[indexA];
        Vector3 p2 = this.tempPositions[indexB];
        Vector3 middle = Vector3.Lerp(p1, p2, 0.5f).normalized;

        // Add the new point to our location map and to the cache.
        index = this.tempPositions.Count;
        this.tempPositions.Add(middle);

        cache.Add(key, index);
        return index;
    }

    #endregion

    private Vector3 RotateVector(Vector3 _original, Vector3 _axis, float _angle) {
        // Find the cross and dot products from the original vector and the rotation axis.
        Vector3 cross = Vector3.Cross(_axis, _original);
        float dot = Vector3.Dot(_axis, _original);

        // Rotate based on Rodrigues' Rotation Formula.
        Vector3 rotatedVector = (_original * Mathf.Cos(_angle)) 
            + (cross * Mathf.Sin(_angle)) 
            + (_axis * dot * (1 - Mathf.Cos(_angle)));
        return rotatedVector;
    }

    private float SpheretoSphereIntersectionPlane ( float _originRadius, float _minorRadius ) {
        return this.SpheretoSphereIntersectionPlane(_originRadius, _minorRadius, _originRadius);
    }

    private float SpheretoSphereIntersectionPlane (float _originRadius, float _minorRadius, float _distanceFromOrigin) {
        float distance = (Mathf.Pow(_distanceFromOrigin, 2) + Mathf.Pow(_originRadius, 2) - Mathf.Pow(_minorRadius, 2)) / (2f * _distanceFromOrigin);
        return distance;
    }

    private void JitterPoints () {
        float adjustedJitterAmount = this.Jitter / Mathf.Max(Mathf.Pow(this.SubDivisions, 2), 1);
        foreach (TectonicPoint point in this.tectonicPoints) {
            float jitterDistance = Random.Range(0, adjustedJitterAmount);
            float jitterDirection = Random.Range(-Mathf.PI, Mathf.PI);
            point.MovePoint(jitterDirection, jitterDistance);
        }
    }

    private Mesh GenerateMesh () {
        Mesh newMesh = new Mesh();
        // Create the list of indices from triangles first.
        List<int> indices = new List<int>();
        for (int i = 0; i < this.tempTriangles.Count; i++) {
            indices.AddRange(this.tempTriangles[i].Indices);
        }

        // Get the positions for the mesh.
        List<Vector3> points = new List<Vector3>();
        for (int i = 0; i < this.tectonicPoints.Count; i++) {
            points.Add(this.tectonicPoints[i].Position);
        }

        // Set the mesh information.
        newMesh.SetVertices(points);
        newMesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
        newMesh.RecalculateNormals();

        return newMesh;
    }

    private void UpdateMesh () {
        // Get the positions for the mesh.
        List<Vector3> points = new List<Vector3>();
        for (int i = 0; i < this.tectonicPoints.Count; i++) {
            points.Add(this.tectonicPoints[i].Position);
        }

        // Set the mesh information.
        //this.mesh.SetVertices(points);
    }

    public void GeneratePlanet () {
        this.Initialize();

        //this.mantle = this.MantleObject.GetComponent<Mantle>();
        //this.mantle.CreateMantle(this);

        this.AddIntialPoints();
        this.Subdivide(this.SubDivisions);

        // Create all the TectonicPoints from the tempPositions.
        foreach (Vector3 point in this.tempPositions) {
            this.tectonicPoints.Add(new TectonicPoint(point, this.PlanetRadius));
        }

        // Create all the TectonicTriangles from the tempTriangles.
        foreach (Triangle tri in this.tempTriangles) {
            this.tectonicTriangles.Add(new TectonicTriangle(this.tectonicPoints[tri.Indices[0]], this.tectonicPoints[tri.Indices[1]], this.tectonicPoints[tri.Indices[2]]));
        }

        // Go through all the triangles and calculate their neighbors.
        foreach (TectonicTriangle triangle in this.tectonicTriangles) {
            triangle.CalculateNeighbors();
        }


        this.JitterPoints();

        //this.mesh = this.GenerateMesh();
        //this.filter.mesh = this.mesh;
        Debug.Log("Triangle count: " + this.tectonicTriangles.Count);

        int plateCount = Random.Range(this.MinPlateCount, this.MaxPlateCount);
        Debug.Log("Plate count: " + plateCount);

        for (int i = 0; i < plateCount; i++) {
            int tries = 0;
            int randomTriangle = Random.Range(0, this.tectonicTriangles.Count);

            while (tries < 100) {
                if (this.tectonicTriangles[randomTriangle].parentPlate == null) {
                    GameObject plateObject = Instantiate<GameObject>(this.PlatePrefab);
                    plateObject.transform.parent = this.transform;

                    TectonicPlate plate = plateObject.GetComponent<TectonicPlate>();
                    plate.Initialize(this.tectonicTriangles[randomTriangle]);
                    this.tectonicPlates.Add(plate);
                    break;
                }

                randomTriangle = Random.Range(0, this.tectonicTriangles.Count);
                tries++;
            }
        }

        List<TectonicPlate> queue = new List<TectonicPlate>(this.tectonicPlates);

        while(queue.Count > 0) {
            int nextPlate = Random.Range(0, queue.Count);

            bool tryToGrow = queue[nextPlate].GrowPlate();

            if (!tryToGrow) {
                queue.RemoveAt(nextPlate);
            }
        }

        for (int i = 0; i < this.tectonicPlates.Count; i++) {
            this.tectonicPlates[i].UpdateMesh();
        }
    }

    void Start ( ) {
        this.GeneratePlanet();
    }

    void Update ( ) {
        this.mantle.UpdateMantle();

        this.CoreObject.transform.Rotate(this.RotationAxis, this.RotationSpeed * Time.deltaTime);
    }
}

