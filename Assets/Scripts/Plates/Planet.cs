using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class Planet : MonoBehaviour {

    public GameObject PlatePrefab;

    public ComputeShader PlateCompute;

    [Range(1, 10)]
    public int SubDivisions = 2;
    [Range(0, 4f)]
    public float Jitter = 0f;
    [Range(1, 100)]
    public float PlanetRadius = 10f;

    [Range(1, 10)]
    public int MinSeedPlateCount = 5;
    [Range(5, 20)]
    public int MaxSeedPlateCount = 10;
    [Range(0, 0.1f)]
    public float NewSeedPlateChance = 0.005f;

    private List<TectonicTriangle> tectonicTriangles;
    private List<TectonicPlate> tectonicPlates;

    // Array of all possible TectonicPoints including extra space.
    public TectonicPoint[] TectonicPoints { get; private set; }
    // List of all empty places in the tectonicPoints array.
    public int[] UnusedIndices { get; private set; }

    public int CurrentPointCount;
    public int MaxPointCount;

    public bool GenerateRandomAxis = true;
    public Vector3 RotationAxis = Vector3.up;
    public float RotationSpeed = 15f;           // 15 degrees per second.

    public bool GrowOverTime = false;
    public int StepsPerFrame = 5;

    // For generation purposes.
    private List<Vector3> tempPositions;
    private List<Triangle> tempTriangles;

    private void Initialize () {
        this.tempTriangles = new List<Triangle>();
        this.tempPositions = new List<Vector3>();

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
        TectonicPoint point;

        Debug.Log(this.MaxPointCount);
        Debug.Log(this.TectonicPoints.Length);

        for (int i = 0; i < this.CurrentPointCount; i++) {
            point = this.TectonicPoints[i];
            float jitterDistance = Random.Range(0, adjustedJitterAmount);
            float jitterDirection = Random.Range(-Mathf.PI, Mathf.PI);
            point.MovePoint(jitterDirection, jitterDistance);
        }
    }

    /*
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
    */

    /*
    private void UpdateMesh () {
        // Get the positions for the mesh.
        List<Vector3> points = new List<Vector3>();
        for (int i = 0; i < this.tectonicPoints.Count; i++) {
            points.Add(this.tectonicPoints[i].Position);
        }

        // Set the mesh information.
        //this.mesh.SetVertices(points);
    }
    */

    public TectonicPlate SeedNewPlate () {
        int tries = 0;
        int randomTriangle;

        do {
            randomTriangle = Random.Range(0, this.tectonicTriangles.Count);

            if (this.tectonicTriangles[randomTriangle].parentPlate == null) {
                GameObject plateObject = Instantiate<GameObject>(this.PlatePrefab);
                plateObject.transform.parent = this.transform;

                TectonicPlate plate = plateObject.GetComponent<TectonicPlate>();
                plate.Initialize(this, this.tectonicTriangles[randomTriangle]);
                this.tectonicPlates.Add(plate);
                return plate;
            }
            tries++;
        } while (tries < 100);

        return null;
    }

    public void SeedPlates () {
        int plateCount = Random.Range(this.MinSeedPlateCount, this.MaxSeedPlateCount);
        Debug.Log("Plate count: " + plateCount);

        for (int i = 0; i < plateCount; i++) {
            this.SeedNewPlate();
        }
    }

    public void GrowStartingPlates (bool _singleStep, int _stepsPerFrame) {

        List<TectonicPlate> queue = new List<TectonicPlate>(this.tectonicPlates);
        int j = 0;
        while (j < queue.Count) {
            if (!queue[j].CanGrow) {
                queue.RemoveAt(j);
            }
            else {
                j++;
            }
        }

        // Set up how many steps can be done per call. If we're not stepping, have infinite "steps".
        int stepCount = 0;
        if (!_singleStep) {
            _stepsPerFrame = int.MaxValue;
        }

        Profiler.BeginSample("Growing Plates");
        while (queue.Count > 0 && stepCount < _stepsPerFrame) {
            // See if we're generating a new plate.
            float chance = Random.Range(0f, 1f);

            if (chance < this.NewSeedPlateChance) {
                TectonicPlate seededPlate = this.SeedNewPlate();
                if (seededPlate != null) {
                    queue.Add(seededPlate);
                    Debug.Log("Added new plate");
                }
            }

            int nextPlate = Random.Range(0, queue.Count);

            bool tryToGrow = queue[nextPlate].GrowPlate();

            if (!tryToGrow) {
                queue.RemoveAt(nextPlate);
            }

            stepCount++;
        }
        Profiler.EndSample();

        Profiler.BeginSample("Updating Plate Meshes");
        for (int i = 0; i < this.tectonicPlates.Count; i++) {
            this.tectonicPlates[i].UpdateMesh();
        }
        Profiler.EndSample();
    }

    public void GeneratePlanet () {
        this.Initialize();

        //this.mantle = this.MantleObject.GetComponent<Mantle>();
        //this.mantle.CreateMantle(this);

        this.AddIntialPoints();
        this.Subdivide(this.SubDivisions);

        // Get the number of points currently.
        this.CurrentPointCount = this.tempPositions.Count;

        // Get the number of points the array should be.
        this.MaxPointCount = (this.tempPositions.Count * 2);
        this.MaxPointCount -= this.MaxPointCount % 16;
        // Create the array for all the TectonicPoints.
        this.TectonicPoints = new TectonicPoint[this.MaxPointCount];
        this.UnusedIndices = new int[this.MaxPointCount];

        // Create all the TectonicPoints from the tempPositions.
        for (int i = 0; i < this.tempPositions.Count; i++) {
            this.TectonicPoints[i] = new TectonicPoint(this, this.tempPositions[i], i);
        }
        // Add all the unused indices to the list.
        for (int i = 0; i < (this.MaxPointCount - this.tempPositions.Count); i++) {
            this.UnusedIndices[i] = (this.tempPositions.Count + i);
        }

        // Create all the TectonicTriangles from the tempTriangles.
        foreach (Triangle tri in this.tempTriangles) {
            this.tectonicTriangles.Add(new TectonicTriangle(this, tri.Indices[0], tri.Indices[1], tri.Indices[2]));
        }

        // Go through all the triangles and calculate their neighbors.
        foreach (TectonicTriangle triangle in this.tectonicTriangles) {
            triangle.CalculateNeighbors();
        }

        this.JitterPoints();

        //this.mesh = this.GenerateMesh();
        //this.filter.mesh = this.mesh;
        Debug.Log("Triangle count: " + this.tectonicTriangles.Count);

        // Seed the initial plates for planet generation.
        this.SeedPlates();
        // If we're not growing seed plates over time, generate the plates now.
        if (!this.GrowOverTime) {
            this.GrowStartingPlates(this.GrowOverTime, this.StepsPerFrame);

            Debug.Log("Plate Count: " + this.tectonicPlates.Count);
        }
    }

    void Start ( ) {
        this.GeneratePlanet();
    }

    void Update ( ) {
        if (this.GrowOverTime && Input.GetKey(KeyCode.Space)) {
            this.GrowStartingPlates(this.GrowOverTime, this.StepsPerFrame);
        }
    }
}

