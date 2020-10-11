using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;


[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Planet : MonoBehaviour {


    public GenerationPhase currentPhase;

    public ComputeShader PlateCompute;

    public bool autoUpdate = true;

    public PlanetSettings planetSettings;

    [HideInInspector]
    public bool planetSettingsFoldout = true;

    private List<TectonicTriangle> tectonicTriangles;

    // Array of all possible TectonicPlates including extra space.
    public TectonicPlate[] TectonicPlates { get; private set; }
    // List of all empty plate indices.
    private List<int> UnusedPlateIndices;

    public int CurrentPlateCount;
    public int PlateTextureWidth = 8;
    public int MaxPlateCount;

    // Array of all possible TectonicPoints including extra space.
    public TectonicPoint[] TectonicPoints { get; private set; }
    // List of all empty places in the tectonicPoints array.
    public List<int> UnusedPointIndices { get; private set; }

    public int CurrentPointCount;
    public int MaxPointCount;

    public Dictionary<int, HalfSide> triangleSides;
    public Dictionary<int, HalfSidePair> triangleSidePairs;

    public Vector3 RotationAxis = Vector3.up;

    public bool GrowOverTime = false;

    public int halfsides;


    /// ---- Mesh Parameters ---- ///
    private MeshRenderer mRenderer;
    private MeshFilter mFilter;
    private Mesh mesh;
    public Material heightmapMaterial;
    private List<Material> submeshMaterials;

    private List<int> MeshIndices;

    /// ---- General Generation Parameters ---- ///


    /// ---- Initial Generation ---- ///
    private List<Vector3> tempPositions;
    private List<Triangle> tempTriangles;


    /// ---- Plate Generation ---- ///
    private bool platesGrown;
    public int TrianglesGrown = 0;


    /// ---- Plate Simulation ---- ///
    public float averageTriangleArea = 0;
    public float averageSideLength = 0;


    private void Initialize() {
        this.tempTriangles = new List<Triangle>();
        this.tempPositions = new List<Vector3>();

        this.triangleSides = new Dictionary<int, HalfSide>();
        this.triangleSidePairs = new Dictionary<int, HalfSidePair>();

        this.tectonicTriangles = new List<TectonicTriangle>();
        this.TectonicPlates = new TectonicPlate[this.MaxPlateCount];
        this.UnusedPlateIndices = new List<int>(this.MaxPlateCount);

        this.MaxPlateCount = this.PlateTextureWidth * this.PlateTextureWidth;

        if (this.planetSettings.GenerateRandomAxis) {
            this.RotationAxis = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        }

        this.platesGrown = false;
        this.currentPhase = GenerationPhase.InitialPlateGeneration;

        this.averageTriangleArea = 0;
        this.averageSideLength = 0;

        this.CurrentPlateCount = 0;
        this.CurrentPointCount = 0;
        this.TrianglesGrown = 0;


        this.halfsides = 0;
    }

    private void InitializeGameObject() {
        this.mRenderer = this.GetComponent<MeshRenderer>();
        this.mFilter = this.GetComponent<MeshFilter>();

        this.mesh = new Mesh();
        this.mFilter.mesh = this.mesh;
        this.submeshMaterials = new List<Material>();

        // Remove all the previous plates and children from the planet.
        while (this.transform.childCount > 0) {
            Transform child = this.transform.GetChild(0);
            DestroyImmediate(child.gameObject);
        }
    }

    public void ChangePhase(GenerationPhase _nextPhase) {
        this.currentPhase = _nextPhase;
    }

    public void OnPlanetSettingsUpdated()
    {
        if (this.autoUpdate)
        {
            this.GeneratePlanet();
        }
    }

    #region IntialGeneration

    // This information is from the tutorial here:
    // https://medium.com/@peter_winslow/creating-procedural-planets-in-unity-part-1-df83ecb12e91


    private void AddIntialPoints() {
        // An icosohedron is 3 orthogonal rectangles, thus we can share point information.
        float t = (1.0f + Mathf.Sqrt(5.0f)) / 2.0f;

        // Generate intial triangles vertex locations.
        this.tempPositions.Add(new Vector3(-1, t, 0).normalized);
        this.tempPositions.Add(new Vector3(1, t, 0).normalized);
        this.tempPositions.Add(new Vector3(-1, -t, 0).normalized);
        this.tempPositions.Add(new Vector3(1, -t, 0).normalized);
        this.tempPositions.Add(new Vector3(0, -1, t).normalized);
        this.tempPositions.Add(new Vector3(0, 1, t).normalized);
        this.tempPositions.Add(new Vector3(0, -1, -t).normalized);
        this.tempPositions.Add(new Vector3(0, 1, -t).normalized);
        this.tempPositions.Add(new Vector3(t, 0, -1).normalized);
        this.tempPositions.Add(new Vector3(t, 0, 1).normalized);
        this.tempPositions.Add(new Vector3(-t, 0, -1).normalized);
        this.tempPositions.Add(new Vector3(-t, 0, 1).normalized);

        // Generate intial triangles and indices.
        this.tempTriangles.Add(new Triangle(0, 11, 5));
        this.tempTriangles.Add(new Triangle(0, 5, 1));
        this.tempTriangles.Add(new Triangle(0, 1, 7));
        this.tempTriangles.Add(new Triangle(0, 7, 10));
        this.tempTriangles.Add(new Triangle(0, 10, 11));
        this.tempTriangles.Add(new Triangle(1, 5, 9));
        this.tempTriangles.Add(new Triangle(2, 4, 11));
        this.tempTriangles.Add(new Triangle(3, 9, 4));
        this.tempTriangles.Add(new Triangle(3, 4, 2));
        this.tempTriangles.Add(new Triangle(3, 2, 6));
        this.tempTriangles.Add(new Triangle(3, 6, 8));
        this.tempTriangles.Add(new Triangle(3, 8, 9));
        this.tempTriangles.Add(new Triangle(4, 9, 5));
        this.tempTriangles.Add(new Triangle(5, 11, 4));
        this.tempTriangles.Add(new Triangle(6, 2, 10));
        this.tempTriangles.Add(new Triangle(7, 1, 8));
        this.tempTriangles.Add(new Triangle(8, 6, 7));
        this.tempTriangles.Add(new Triangle(9, 8, 1));
        this.tempTriangles.Add(new Triangle(10, 7, 6));
        this.tempTriangles.Add(new Triangle(11, 10, 2));
    }

    private void Subdivide(int _divisions) {
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

    private int GetMidPointIndex(Dictionary<int, int> cache, int indexA, int indexB) {
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

    /*
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

    private float SpheretoSphereIntersectionPlane(float _originRadius, float _minorRadius) {
        return this.SpheretoSphereIntersectionPlane(_originRadius, _minorRadius, _originRadius);
    }

    private float SpheretoSphereIntersectionPlane(float _originRadius, float _minorRadius, float _distanceFromOrigin) {
        float distance = (Mathf.Pow(_distanceFromOrigin, 2) + Mathf.Pow(_originRadius, 2) - Mathf.Pow(_minorRadius, 2)) / (2f * _distanceFromOrigin);
        return distance;
    }
    */

    #region General Planet Functions

    public bool SetHalfSide(HalfSide _newSide)
    {
        return this.SetHalfSide(_newSide.Index, _newSide);
    }

    public bool SetHalfSide (int _key, HalfSide _newSide) {

        this.halfsides++;

        if (this.triangleSides.ContainsKey(_key)) {
            return false;
        }

        this.triangleSides.Add(_key, _newSide);

        // Add the HalfSidePair if it doesn't exist.
        int pairIndex = (Mathf.Max(_newSide.StartIndex, _newSide.EndIndex) << 16) + Mathf.Min(_newSide.StartIndex, _newSide.EndIndex);
        if (!this.triangleSidePairs.ContainsKey(pairIndex)) {
            HalfSidePair newPair = new HalfSidePair(this, pairIndex, _newSide.Index, _newSide.Opposite);

            // Add it in both locations for now for quick access.
            this.triangleSidePairs.Add(pairIndex, newPair);
        }

        return true;
    }

    public bool UpdateHalfSide (HalfSide _side) {
        if (this.triangleSides.ContainsKey(_side.Index)) {
            this.triangleSides[_side.Index] = _side;

            return true;
        }
        else {
            return this.SetHalfSide(_side.Index, _side);
        }
    }

    private void UpdatePlateMeshes () {
        Profiler.BeginSample("Updating Plate Meshes");

        //if (this.currentPhase == GenerationPhase.InitialPlateGeneration) {
        //    this.mesh.subMeshCount = this.CurrentPlateCount;
        //}

        Profiler.BeginSample("Plate Meshes: Setting Shader");
        // Get all the points for the mesh.
        Vector3[] verts = new Vector3[this.CurrentPointCount];
        Color[] colors = new Color[this.CurrentPointCount];
        for (int i = 0; i < this.CurrentPointCount; i++) {
            verts[i] = this.TectonicPoints[i].SpherePosition;
            colors[i].r = this.TectonicPoints[i].density;
            colors[i].g = this.TectonicPoints[i].thickness;
        }

        // Set the mesh vertices.
        this.mesh.SetVertices(verts);
        this.mesh.SetColors(colors);

        Profiler.EndSample();

        // Go through all the plates and have them update their individual submeshes.
        /*for (int i = 0; i < this.CurrentPlateCount; i++) {
            if (this.TectonicPlates[i] != null) {
                this.TectonicPlates[i].UpdateMesh();
            }
        }*/

        int plateTriangleRequirement = Mathf.Max(Mathf.RoundToInt(this.planetSettings.SubDivisions * this.planetSettings.SubDivisions), 2);
        Dictionary<int, List<int>> plateMeshIndices = new Dictionary<int, List<int>>();
        // Add the base submesh.
        plateMeshIndices.Add(0, new List<int>());

        foreach (TectonicTriangle triangle in this.tectonicTriangles) {
            if (triangle.parentPlate.TriangleCount > plateTriangleRequirement) {
                if (!plateMeshIndices.ContainsKey(triangle.parentPlate.PlateIndex + 1)) {
                    plateMeshIndices.Add(triangle.parentPlate.PlateIndex + 1, new List<int>());
                }
                for (int i = 0; i < 3; i++) {
                    plateMeshIndices[triangle.parentPlate.PlateIndex + 1].Add(triangle.Points[i].Index);
                }
            }
            else {
                for (int i = 0; i < 3; i++) {
                    plateMeshIndices[0].Add(triangle.Points[i].Index);
                }
            }
        }

        this.mesh.subMeshCount = plateMeshIndices.Count;

        int currentSubmesh = 0;
        foreach(KeyValuePair<int, List<int>> submesh in plateMeshIndices) {
            this.mesh.SetIndices(submesh.Value, MeshTopology.Triangles, currentSubmesh);

            Material subMeshMat = new Material(this.heightmapMaterial);
            if (submesh.Key > 0) {
                subMeshMat.SetColor("_Color", this.TectonicPlates[submesh.Key - 1].PlateColor);
            }

            if (currentSubmesh >= this.submeshMaterials.Count) {
                this.submeshMaterials.Insert(currentSubmesh, subMeshMat);
            }
            else {
                this.submeshMaterials[currentSubmesh] = subMeshMat;
            }

            currentSubmesh++;
        }
        this.mRenderer.materials = this.submeshMaterials.ToArray();

        Profiler.EndSample();
    }

    public void UpdateTectonicPlateMesh (List<int> _triangleIndices, int _plateIndex) {
        for (int i = 0; i < 3; i++) {
            if (this.MeshIndices.Count <= (_plateIndex * 3) + i) {
                this.MeshIndices.Insert((_plateIndex * 3) + i, _triangleIndices[i]);
            }
            else {
                this.MeshIndices[(_plateIndex * 3) + i] = _triangleIndices[i];
            }
        }

        this.mesh.SetIndices(this.MeshIndices, MeshTopology.Triangles, 0);// _submesh);
    }

    public void UpdateTectonicPlateMaterial (Color _plateColor, int _submesh) {
        Material subMeshMat = new Material(this.heightmapMaterial);
        subMeshMat.SetColor("_Color", _plateColor);

        if (this.submeshMaterials.Count <= _submesh) {
            this.submeshMaterials.Add(subMeshMat);
        }
        else {
            this.submeshMaterials[_submesh] = subMeshMat;
        }
        //this.mRenderer.materials = this.submeshMaterials.ToArray();
    }

    public bool RemoveHalfSide (HalfSide _side)
    {
        if (this.triangleSides.ContainsKey(_side.Index))
        {
            this.triangleSides.Remove(_side.Index);

            return true;
        }
        else
        {
            return false;
        }
    }

    public bool RemovePlate (TectonicPlate _plate) {
        if (_plate.TriangleCount > 0) {
            return false;
        }

        // Remove the plate from our list and add the index to the unused list.
        this.TectonicPlates[_plate.PlateIndex] = null;
        this.UnusedPlateIndices.Add(_plate.PlateIndex);
        this.CurrentPlateCount--;

        return true;
    }

    public bool RemovePlate (int _plateIndex) {
        return this.RemovePlate(this.TectonicPlates[_plateIndex]);
    }

    #endregion


    #region Planet Generation Functions

    private void JitterPlanet () {
        float adjustedJitterAmount = this.planetSettings.Jitter / Mathf.Max(Mathf.Pow(this.planetSettings.SubDivisions, 2), 1);
        TectonicPoint point;

        for (int i = 0; i < this.CurrentPointCount; i++) {
            point = this.TectonicPoints[i];
            float jitterDistance = Random.Range(0, adjustedJitterAmount);
            float jitterDirection = Random.Range(-Mathf.PI, Mathf.PI);
            point.MovePoint(new Vector2(Mathf.Cos(jitterDirection), Mathf.Sin(jitterDirection)), jitterDistance);
            point.SetDensity(Random.Range(5 - this.planetSettings.plateSettings.PlateStartingDensityVariance,
                5 + this.planetSettings.plateSettings.PlateStartingDensityVariance));
            point.SetThickness(Random.Range(7f - this.planetSettings.plateSettings.PlateStartingThicknessVariance,
                7f + this.planetSettings.plateSettings.PlateStartingThicknessVariance));
        }

        float adjustedJitterVelocity = this.planetSettings.initalGenerationSettings.TriangleIntialVelocity / Mathf.Max(Mathf.Pow(this.planetSettings.SubDivisions, 2), 1);
        for (int i = 0; i < this.tectonicTriangles.Count; i++) {
            float jitterVelocity = Random.Range(0, adjustedJitterVelocity);
            float jitterDirection = Random.Range(-Mathf.PI, Mathf.PI);

            this.tectonicTriangles[i].SetInitialVelocity(new Vector2(Mathf.Cos(jitterDirection), Mathf.Sin(jitterDirection)), jitterVelocity);
        }
    }

    private TectonicPlate SeedNewPlate () {
        int tries = 0;
        int randomTriangle;

        // Try creating a new plate at a random triangle on the planet.
        do {
            randomTriangle = Random.Range(0, this.tectonicTriangles.Count);

            // If the triangle is not claimed by a plate, add a plate to it.
            if (this.tectonicTriangles[randomTriangle].parentPlate == null) {
                TectonicPlate plate = new TectonicPlate();
                plate.Initialize(this, this.tectonicTriangles[randomTriangle], this.CurrentPlateCount);
                this.TectonicPlates[this.CurrentPlateCount] = plate;

                this.CurrentPlateCount++;
                return plate;
            }
            // If we didn't create a plate, make sure we can escape.
            tries++;
        } while (tries < 100);

        return null;
    }

    private void SeedPlates () {
        int plateCount = Random.Range(this.planetSettings.plateSettings.MinSeedPlateCount, Mathf.Min(this.planetSettings.plateSettings.MaxSeedPlateCount, this.MaxPlateCount));
        Debug.Log("Seed Plate count: " + plateCount);

        for (int i = 0; i < plateCount; i++) {
            this.SeedNewPlate();
        }
    }

    private void GrowStartingPlates (bool _singleStep, int _stepsPerFrame) {

        // Add all the plates that can grow to the current queue of plates.
        List<TectonicPlate> queue = new List<TectonicPlate>(this.CurrentPlateCount);
        for (int i = 0; i < this.CurrentPlateCount; i++) {
            if (this.TectonicPlates[i].CanGrow) {
                queue.Add(this.TectonicPlates[i]);
            }
        }

        // Set up how many steps can be done per call. If we're not stepping, have infinite "steps".
        int stepCount = 0;
        if (!_singleStep) {
            _stepsPerFrame = int.MaxValue;
        }

        Profiler.BeginSample("Growing Plates");
        while (queue.Count > 0 && stepCount < _stepsPerFrame) {
            for (int i = 0; i < this.planetSettings.generationSettings.TrianglesPerStep && queue.Count > 0; i++) {
                // See if we're generating a new plate.
                float chance = Random.Range(0f, 1f);

                if (chance < this.planetSettings.plateSettings.NewSeedPlateChance && this.CurrentPlateCount < this.MaxPlateCount - 1) {
                    TectonicPlate seededPlate = this.SeedNewPlate();
                    if (seededPlate != null) {
                        queue.Add(seededPlate);
                    }
                }

                int nextPlate = Random.Range(0, queue.Count);

                bool tryToGrow = queue[nextPlate].GrowPlate();

                if (!tryToGrow) {
                    queue.RemoveAt(nextPlate);
                }
                else {
                    this.TrianglesGrown++;
                }
            }
            stepCount++;
        }
        Profiler.EndSample();
        
        if (queue.Count == 0) {
            this.platesGrown = true;

            // Add all the unused plate indices to the list.
            for (int i = 0; i < (this.MaxPlateCount - this.CurrentPlateCount); i++) {
                this.UnusedPlateIndices.Add(this.CurrentPlateCount + i);
            }

            // Go through each triangle and see if it's internal or not.
            foreach (TectonicTriangle triangle in this.tectonicTriangles) {
                triangle.CalculateInternalStatus();
            }

            this.mesh.subMeshCount = this.CurrentPlateCount;

            this.ChangePhase(GenerationPhase.PlateSimulation);
            return;
        }
    }

    private void GeneratePlates () {
        this.TectonicPlates = new TectonicPlate[this.tectonicTriangles.Count];

        foreach (TectonicTriangle triangle in this.tectonicTriangles) {
            TectonicPlate plate = new TectonicPlate();
            plate.Initialize(this, triangle, this.CurrentPlateCount);
            this.TectonicPlates[this.CurrentPlateCount] = plate;

            this.CurrentPlateCount++;
        }

        this.platesGrown = true;

        // Add all the unused plate indices to the list.
        for (int i = 0; i < (this.MaxPlateCount - this.CurrentPlateCount); i++) {
            this.UnusedPlateIndices.Add(this.CurrentPlateCount + i);
        }

        // Go through each triangle and see if it's internal or not.
        foreach (TectonicTriangle triangle in this.tectonicTriangles) {
            triangle.CalculateInternalStatus();
        }

        this.ChangePhase(GenerationPhase.PlateSimulation);
    }

    public void GeneratePlanet () {

        this.Initialize();
        this.InitializeGameObject();


        this.icosahedron = new Icosahedron();

        this.icosahedron.Initialize(this.planetSettings.SubDivisions, false);

        this.icosahedron.GenerateSphere();


        this.points = new List<PTPoint>();
        this.triangles = new List<PTTriangle>();
        this.sides = new List<PTHalfSide>();
        this.boundaries = new List<PTBoundary>();
        this.boundaryCaps = new List<PTBoundaryGapCap>();

        Dictionary<int, PTHalfSide[]> sidePairs = new Dictionary<int, PTHalfSide[]>();
        Dictionary<int, List<PTHalfSide>> cornerGroups = new Dictionary<int, List<PTHalfSide>>();

        for (int i = 0; i < this.icosahedron.Triangles.Count; i++) {
            // Create the points.
            for (int j = 0; j < 3; j++) {
                this.points.Add(new PTPoint(this, this.icosahedron.Points[this.icosahedron.Triangles[i].Indices[j]]));
                this.points[(i * 3) + j].RenderIndex = this.icosahedron.Triangles[i].Indices[j];
            }

            // Create the sides and assign them to their triangles.
            for (int j = 0; j < 3; j++) {
                // Create the side.
                this.sides.Add(new PTHalfSide(this, this.points[(i * 3) + j], this.points[((i * 3) + (j + 1) % 3)]));

                // Get the index that this side will use to identify its pairing.
                int greater = Mathf.Max(this.icosahedron.Triangles[i].Indices[j], this.icosahedron.Triangles[i].Indices[(j + 1) % 3]);
                int lesser = Mathf.Min(this.icosahedron.Triangles[i].Indices[j], this.icosahedron.Triangles[i].Indices[(j + 1) % 3]);
                int sideIndex = (greater << 16) + lesser;

                // Add it to the sidePairs dictionary, creating a new list if needed.
                if (sidePairs.ContainsKey(sideIndex)) {
                    sidePairs[sideIndex][1] = this.sides[this.sides.Count - 1];
                }
                else {
                    sidePairs[sideIndex] = new PTHalfSide[2];
                    sidePairs[sideIndex][0] = this.sides[this.sides.Count - 1];
                }

                // Add it to the cornerGroups dictionary, creating a new list if needed.
                //  First add at the greater index.
                if (cornerGroups.ContainsKey(greater)) {
                    cornerGroups[greater].Add(this.sides[this.sides.Count - 1]);
                }
                else {
                    cornerGroups[greater] = new List<PTHalfSide>();
                    cornerGroups[greater].Add(this.sides[this.sides.Count - 1]);
                }
                // Add at the lesser index.
                if (cornerGroups.ContainsKey(lesser)) {
                    cornerGroups[lesser].Add(this.sides[this.sides.Count - 1]);
                }
                else {
                    cornerGroups[lesser] = new List<PTHalfSide>();
                    cornerGroups[lesser].Add(this.sides[this.sides.Count - 1]);
                }
            }

            // Add the new sides to the triangle.
            this.triangles.Add(new PTTriangle(this, this.sides.GetRange(i * 3, 3).ToArray()));
        }

        Debug.Log("Points: " + this.points.Count + " | Tris: " + this.triangles.Count);

        // Go through each side pairing and create its boundary.
        foreach (KeyValuePair<int, PTHalfSide[]> sidePair in sidePairs) {
            this.boundaries.Add(new PTBoundary(this, sidePair.Value[0], sidePair.Value[1]));
        }

        // Go through each cornerGroup and create a boundary cap.
        foreach (KeyValuePair<int, List<PTHalfSide>> cornerGroup in cornerGroups) {
            List<PTHalfSide> sorted = new List<PTHalfSide>();
            bool end = true;

            // Loop through to sort the list of PTHalfSides.
            PTHalfSide checkSide = null;

            // Get a starting checkSide that has the index as its end point.
            for (int i = 0; i < cornerGroup.Value.Count; i++) {
                if (cornerGroup.Value[i].End.RenderIndex == cornerGroup.Key) {
                    checkSide = cornerGroup.Value[i];
                    break;
                }
            }

            for (int i = 0; i < cornerGroup.Value.Count; i++) {
                // Add this side to the list.
                sorted.Add(checkSide);

                // If we're at the end of the HalfSide, we need to get the next HalfSide on the shared triangle.
                if (end) {
                    PTHalfSide nextSide = checkSide.ParentTriangle.GetNextHalfSide(checkSide);

                    if (nextSide != null) {
                        checkSide = nextSide;
                        end = false;
                    }
                }
                else {
                    // If we're at the start of the HalfSide, we need to get the side across the boundary.
                    //  The next side should always have this index as its end if this side had it as the start.
                    if (checkSide.ParentBoundary.FirstSide == checkSide) {
                        checkSide = checkSide.ParentBoundary.SecondSide;
                    }
                    else {
                        checkSide = checkSide.ParentBoundary.FirstSide;
                    }

                    end = true;
                }
            }

            this.boundaryCaps.Add(new PTBoundaryGapCap(this, sorted));
        }

        for (int i = 0; i < this.triangles.Count; i++) {
            this.triangles[i].GetCenter();
        }

        for (int i = 0; i < this.triangles.Count; i++) {
            this.triangles[i].ContractPointsTest(Random.Range(0, 0.5f));
        }

        for (int i = 0; i < this.boundaries.Count; i++) {
            this.boundaries[i].CalculateBoundaryInformation();
            this.boundaries[i].SetCornerDesired();
        }

        this.TestRender();
    }

    List<PTPoint> points;
    List<PTTriangle> triangles;
    List<PTHalfSide> sides;
    List<PTBoundary> boundaries;
    List<PTBoundaryGapCap> boundaryCaps;
    Icosahedron icosahedron;

    public int selectedPoint;

    private void TestRender () {
        List<Vector3> verts = new List<Vector3>();
        List<int> indices = new List<int>();

        Matrix4x4 rotMat = this.points[Mathf.Abs(this.selectedPoint) % this.points.Count].RotationMatrix.inverse;
        Vector3 rotated;
        for (int i = 0; i < this.points.Count; i++) {
            rotated = rotMat.MultiplyPoint3x4(this.points[i].SphereLocation);
            verts.Add(rotated);
            //verts.Add(this.points[i].SphereLocation);
            this.points[i].RenderIndex = i;
        }

        for (int i = 0; i < this.triangles.Count; i++) {
            for (int j = 0; j < 3; j++) {
                indices.Add(this.triangles[i].Points[j].RenderIndex);
            }
        }

        for (int i = 0; i < this.boundaries.Count; i++) {
            indices.AddRange(this.boundaries[i].GetBoundaryIndices());
        }

        for (int i = 0; i < this.boundaryCaps.Count; i++) {
            indices.AddRange(this.boundaryCaps[i].GetBoundaryCapIndices());
        }

        this.mesh.SetVertices(verts);
        this.mesh.SetIndices(indices, MeshTopology.Triangles, 0);
        this.mesh.RecalculateBounds();
        this.mesh.RecalculateNormals();
    }

    #endregion


    #region Plate Simulation Functions



    #endregion

    /// ---- Unity Functions ---- ///

    void Start ( ) {
        this.GeneratePlanet();
    }

    void Update () {

        for (int i = 0; i < this.boundaries.Count; i++) {
            this.boundaries[i].CalculateBoundaryInformation();
            this.boundaries[i].CalculateBoundaryForces(Time.deltaTime);
        }

        for (int i = 0; i < this.points.Count; i++) {
            this.points[i].CalculateMovement(Time.deltaTime);
        }

//        Debug.Log(this.boundaries[this.selectedPoint].FirstCorner.CornerAngle * 180f / Mathf.PI);

        this.TestRender();
    }
}

