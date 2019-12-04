using UnityEngine;
using UnityEngine.Profiling;
using System.Collections;
using System.Collections.Generic;

public class Mantle : MonoBehaviour {

    public GameObject pointPrefab;

    public int PointCount = 0;

    private PoissonRandom poisson;

    private List<MantlePoint> mantlePoints;

    private Octree mantleOct;

    private Planet parentPlanet;


    public float coreTemperature = 4500;
    public float surfaceTemperature = 1000;


    public int GridCellsWide = 16;

    public ComputeShader compute;

    private ComputeBuffer particleBuffer;
    private ComputeBuffer gridPlacementBuffer;
    private ComputeBuffer gridContainedBuffer;

    private Dictionary<int, GridCell> grid;
    private int gridCellCount;

    public struct GridCell {
        public int index;
        public int particleCount;
        public int insertCount;
        public int[] particles;
        public int[] particlesToInsert;
        public CubeBoundary boundary;
    }
    public int maxParticlesPerGrid = 15;


    // Lists and arrays for updating/reading from GPU.
    ParticlePoint[] updatedParticles;
    List<int> keys;
    int[] gridContained;
    int[] gridPlacement;

    public void CreateMantle (Planet _parent) {
        this.parentPlanet = _parent;

        this.Initialize();
    }

    public void Initialize () {

        // Initialize parameters.
        this.poisson = new PoissonRandom();
        this.mantlePoints = new List<MantlePoint>();
        this.grid = new Dictionary<int, GridCell>();

        // Knock the number of particles to the nearest multiple of 16.
        int keptParticles;

        do {
            // Clear Mantle's children from previous generation attempts.
            this.ClearChildren();

            // Initialize parameters.
            this.poisson = new PoissonRandom();
            this.mantlePoints = new List<MantlePoint>();
            this.grid = new Dictionary<int, GridCell>();

            this.mantlePoints = this.poisson.PoissonMantle(this.parentPlanet.MantlePointDensity, 15, this.parentPlanet.transform.position, this.parentPlanet.MantleRadius, this.surfaceTemperature, this.coreTemperature,
                out this.mantleOct, this.pointPrefab, this.parentPlanet.MantlePointDensity / 2f, 2.5f, this.parentPlanet.CoreRadius);

            foreach (MantlePoint point in this.mantlePoints) {
                point.transform.parent = this.transform;
            }

            keptParticles = ((int) (this.mantlePoints.Count / 16)) * 16;
        } while (keptParticles == 0);

        if (this.mantlePoints.Count == 0) {
            Debug.Log("No Points");
        }

        this.mantlePoints.RemoveRange(keptParticles, this.mantlePoints.Count % 16);
        this.PointCount = this.mantlePoints.Count;


        // Initialize the grid.
        this.gridCellCount = 0;
        this.InitializeGrid();


        // Prepare lists/arrays for GPU reading/writing.
        this.updatedParticles = new ParticlePoint[this.PointCount];
        this.keys = new List<int>(this.grid.Keys);
        int bufferLength = this.maxParticlesPerGrid + 1;
        this.gridContained = new int[this.grid.Count * bufferLength];
        this.gridPlacement = new int[this.PointCount * 7];


        // Insert all the points for the first time.
        this.InsertGridPoints();


        // Initialize the mantle shader.
        float gridWidth = this.parentPlanet.MantleRadius;
        float cellWidth = gridWidth * 2f / this.GridCellsWide;
        
        this.particleBuffer = new ComputeBuffer(this.PointCount, 64);
        this.gridPlacementBuffer = new ComputeBuffer(this.PointCount * 7, sizeof(int));
        this.gridContainedBuffer = new ComputeBuffer(this.grid.Count * (this.maxParticlesPerGrid + 1), sizeof(int));
        this.compute.SetFloat("simSpeed", 0.01667f);

        this.compute.SetFloat("gridWidth", gridWidth);
        this.compute.SetFloat("cellWidth", cellWidth);
        this.compute.SetInt("cellsWide", this.GridCellsWide);

        this.compute.SetFloat("coreRadius", this.parentPlanet.CoreRadius);
        this.compute.SetFloat("mantleRadius", this.parentPlanet.MantleRadius);

        this.compute.SetFloat("interactionDistance", 2);
        this.compute.SetFloat("forceMod", 10f);
        this.compute.SetFloat("forceExponential", 5f);
        this.compute.SetFloat("frictionMod", 0.2f);

        this.compute.SetFloat("convectionMod", 0.5f);
        this.compute.SetFloat("heatingMod", 2f);
        this.compute.SetFloat("sinkingMod", 5f);
        this.compute.SetFloat("coreTemperature", this.coreTemperature);
        this.compute.SetFloat("surfaceTemperature", this.surfaceTemperature);

        this.compute.SetFloat("coreDensity", 7);
        this.compute.SetFloat("surfaceDensity", 2.5f);

        this.compute.SetInt("F_TO_I", 2 << 17);
        this.compute.SetFloat("I_TO_F", 1f / (2 << 17));

        int findGridCells = this.compute.FindKernel("findGridCells");
        this.compute.SetBuffer(findGridCells, "particleBuffer", this.particleBuffer);
        this.compute.SetBuffer(findGridCells, "gridPlacementBuffer", this.gridPlacementBuffer);

        int calculateParticleInteractions = this.compute.FindKernel("calculateParticleInteractions");
        this.compute.SetBuffer(calculateParticleInteractions, "particleBuffer", this.particleBuffer);
        this.compute.SetBuffer(calculateParticleInteractions, "gridPlacementBuffer", this.gridPlacementBuffer);
        this.compute.SetBuffer(calculateParticleInteractions, "gridContainedBuffer", this.gridContainedBuffer);

        int calculateIndividualParticles = this.compute.FindKernel("calculateIndividualParticles");
        this.compute.SetBuffer(calculateIndividualParticles, "particleBuffer", this.particleBuffer);

        int applyParticleForces = this.compute.FindKernel("applyParticleForces");
        this.compute.SetBuffer(applyParticleForces, "particleBuffer", this.particleBuffer);


        // Send the particle data to the GPU.
        ParticlePoint[] particles = new ParticlePoint[this.PointCount];
        for (int i = 0; i < this.PointCount; i++) {
            particles[i] = this.mantlePoints[i].point;
        }

        this.particleBuffer.SetData(particles);
    }

    public void ClearChildren () {
        // Make sure all the children of the mantle are dead.
        while (this.transform.childCount > 0) {
            GameObject point = this.transform.GetChild(0).gameObject;
            point.transform.parent = null;
            DestroyImmediate(point);
        }
    }

    public void InitializeGrid () {
        // Declare variables.
        Profiler.BeginSample("Grid: Construct Grid");
        CubeBoundary boundary;
        GridCell cell;
        int index;
        float cellWidth = this.parentPlanet.MantleRadius * 2f / this.GridCellsWide;
        float halfCell = cellWidth / 2f;

        // Go through every possible grid cell that could intersect with the planet's mantle.
        for (int i = 0; i < this.GridCellsWide; i++) {
            for (int j = 0; j < this.GridCellsWide; j++) {
                for (int k = 0; k < this.GridCellsWide; k++) {
                    // Calculate the center of the grid cell.
                    Vector3 center = new Vector3(-this.parentPlanet.MantleRadius + (cellWidth * i) + halfCell,
                        -this.parentPlanet.MantleRadius + (cellWidth * j) + halfCell,
                        -this.parentPlanet.MantleRadius + (cellWidth * k) + halfCell);
                    boundary = new CubeBoundary(center, cellWidth);

                    index = i + (j * this.GridCellsWide) + (k * this.GridCellsWide * this.GridCellsWide);
                    // See if the grid cell has any overlap with the mantle.
                    if (boundary.IntersectsSphere(this.parentPlanet.transform.position, this.parentPlanet.MantleRadius)) {
                        // If it does, this is a valid grid cell.
                        this.gridCellCount++;

                        cell = new GridCell {
                            index = index,
                            boundary = boundary,
                            particleCount = 0,
                            insertCount = 0,
                            particles = new int[this.maxParticlesPerGrid],
                            particlesToInsert = new int[this.maxParticlesPerGrid]
                        };

                        this.grid.Add(index, cell);
                    }
                }
            }
        }
        Debug.Log("Grid count: " + this.gridCellCount);
        Profiler.EndSample();

        Profiler.BeginSample("Grid: Octree to Grid");
        MantlePoint particle;
        int x, y, z;
        List<int> overlappedCells = new List<int>(); 
        // Convert the points from the octree to the grid.
        for (int i = 0; i < this.mantlePoints.Count; i++) {
            particle = this.mantlePoints[i];

            x = (int) ((particle.location.x + this.parentPlanet.MantleRadius) / cellWidth);
            y = (int) ((particle.location.y + this.parentPlanet.MantleRadius) / cellWidth);
            z = (int) ((particle.location.z + this.parentPlanet.MantleRadius) / cellWidth);
            index = x + (y * this.GridCellsWide) + (z * this.GridCellsWide * this.GridCellsWide);
            overlappedCells.Add(index);

            Vector3 center = new Vector3(-this.parentPlanet.MantleRadius + (cellWidth * x) + halfCell,
                -this.parentPlanet.MantleRadius + (cellWidth * y) + halfCell,
                -this.parentPlanet.MantleRadius + (cellWidth * z) + halfCell);

            // Calculate the x min/max cells.
            x = (int) ((particle.location.x + this.parentPlanet.MantleRadius + particle.radius) / cellWidth);
            index = x + (y * this.GridCellsWide) + (z * this.GridCellsWide * this.GridCellsWide);
            if (!overlappedCells.Contains(index))
                overlappedCells.Add(index);

            x = (int) ((particle.location.x + this.parentPlanet.MantleRadius - particle.radius) / cellWidth);
            index = x + (y * this.GridCellsWide) + (z * this.GridCellsWide * this.GridCellsWide);
            if (!overlappedCells.Contains(index))
                overlappedCells.Add(index);

            x = (int) ((particle.location.x + this.parentPlanet.MantleRadius) / cellWidth);

            // Calculate the y min/min cells.
            y = (int) ((particle.location.y + this.parentPlanet.MantleRadius + particle.radius) / cellWidth);
            index = x + (y * this.GridCellsWide) + (z * this.GridCellsWide * this.GridCellsWide);
            if (!overlappedCells.Contains(index))
                overlappedCells.Add(index);

            y = (int) ((particle.location.y + this.parentPlanet.MantleRadius - particle.radius) / cellWidth);
            index = x + (y * this.GridCellsWide) + (z * this.GridCellsWide * this.GridCellsWide);
            if (!overlappedCells.Contains(index))
                overlappedCells.Add(index);

            y = (int) ((particle.location.y + this.parentPlanet.MantleRadius) / cellWidth);

            // Calculate the z min/min cells.
            z = (int) ((particle.location.z + this.parentPlanet.MantleRadius + particle.radius) / cellWidth);
            index = x + (y * this.GridCellsWide) + (z * this.GridCellsWide * this.GridCellsWide);
            if (!overlappedCells.Contains(index))
                overlappedCells.Add(index);

            z = (int) ((particle.location.z + this.parentPlanet.MantleRadius - particle.radius) / cellWidth);
            index = x + (y * this.GridCellsWide) + (z * this.GridCellsWide * this.GridCellsWide);
            if (!overlappedCells.Contains(index))
                overlappedCells.Add(index);

            // Go through each overlapped cell and insert the point.
            for (int k = 0; k < overlappedCells.Count; k++) {
                try {
                    cell = this.grid[overlappedCells[k]];

                    cell.particlesToInsert[cell.insertCount] = i;
                    cell.insertCount++;

                    this.grid[overlappedCells[k]] = cell;
                }
                catch {
                    Debug.Log(overlappedCells[k]);
                    float dist = Vector3.Distance(this.parentPlanet.transform.position, particle.location);
                }
            }

            // Reset the list of overlapping cells.
            overlappedCells.Clear();
        }
        Profiler.EndSample();
    }

    public void InsertGridPoints () {
        // Declare variables.
        Profiler.BeginSample("Grid: Inserting Points");
        GridCell cell;
        int i;

        // Go through each grid cell and insert the particles into the main array.
        foreach (int key in this.keys) {
            cell = this.grid[key];

            // Swap arrays.
            for (i = 0; i < this.maxParticlesPerGrid; i++) {
                // If we're under the number of points to insert, swap normally.
                if (i < cell.insertCount) {
                    cell.particles[i] = cell.particlesToInsert[i];
                    cell.particlesToInsert[i] = -1;
                }
                else {
                    // Otherwise clear the values.
                    cell.particles[i] = -1;
                    cell.particlesToInsert[i] = -1;
                }
            }

            // Swap counts.
            cell.particleCount = cell.insertCount;
            cell.insertCount = 0;

            // Put updated cell back into grid.
            this.grid[key] = cell;
        }
        Profiler.EndSample();
    }

    public void UpdateGridPoints () {
        Profiler.BeginSample("Grid: GPU to Grid");

        // Initialize parameters.
        MantlePoint particle;
        GridCell cell;
        List<int> overlappedCells = new List<int>();
        this.gridPlacementBuffer.GetData(this.gridPlacement);

        // Convert the points from the gpu to the grid.
        for (int i = 0; i < this.PointCount; i++) {
            particle = this.mantlePoints[i];

            for (int j = 0; j < 7; j++) {
                int index = this.gridPlacement[j + (i * 7)];
                if (!overlappedCells.Contains(index)) {
                    overlappedCells.Add(index);
                }
            }

            // Go through each overlapped cell and insert the point.
            for (int k = 0; k < overlappedCells.Count; k++) {
                try {
                    cell = this.grid[overlappedCells[k]];

                    cell.particlesToInsert[cell.insertCount] = i;
                    cell.insertCount++;

                    this.grid[overlappedCells[k]] = cell;
                }
                catch {
                    Debug.Log(overlappedCells[k]);
                }
            }

            // Reset the list of overlapping cells.
            overlappedCells.Clear();
        }
        Profiler.EndSample();

        // Insert all the points for the first time.
        this.InsertGridPoints();
    }

    public void UpdateParticles () {
        Profiler.BeginSample("Grid: GPU to Particles");

        // Initialize parameters.
        MantlePoint particle;
        ParticlePoint gpuParticle;
        this.particleBuffer.GetData(this.updatedParticles);

        // Convert the points from the gpu to the grid.
        for (int i = 0; i < this.PointCount; i++) {
            gpuParticle = this.updatedParticles[i];
            particle = this.mantlePoints[i];
            particle.UpdatePoint(gpuParticle);
        }
        Profiler.EndSample();
    }

    public void UpdateMantle () {
        //float rotationSpeed = this.parentPlanet.RotationSpeed * Time.deltaTime;
        //foreach (MantlePoint point in this.mantlePoints) {
        //    point.RotatePoint(this.parentPlanet.RotationAxis, rotationSpeed);
        //}

        //Profiler.BeginSample("Updating Octree");
        //this.mantleOct.UpdateOctree();
        //Profiler.EndSample();

        Profiler.BeginSample("Compute Shader: Update");
        Profiler.BeginSample("Compute Shader: Set Data");

        // Send the grid data to the GPU.
        int bufferLength = this.maxParticlesPerGrid + 1;
        for (int i = 0; i < this.grid.Count; i++) {
            this.gridContained[i * bufferLength] = this.grid[this.keys[i]].particleCount;
            for (int j = 0; j < this.maxParticlesPerGrid; j++) {
                this.gridContained[(i * bufferLength) + 1 + j] = this.grid[this.keys[i]].particles[j];
            }
        }

        this.gridContainedBuffer.SetData(this.gridContained);
        Profiler.EndSample();

        Profiler.BeginSample("Compute Shader: Calculate Forces");
        // Run the kernal to calculate the forces on the particle interactions.
        int calculateParticleInteractions = this.compute.FindKernel("calculateParticleInteractions");
        this.compute.Dispatch(calculateParticleInteractions, (this.grid.Count / 16) + 1, 1, 1);

        // Run the kernal to calculate the forces on each particle.
        int calculateIndividualParticles = this.compute.FindKernel("calculateIndividualParticles");
        this.compute.Dispatch(calculateIndividualParticles, (this.grid.Count / 16) + 1, 1, 1);
        Profiler.EndSample();

        Profiler.BeginSample("Compute Shader: Apply Forces");
        // Run the kernal to calculate the forces on the particles.
        int applyParticleForces = this.compute.FindKernel("applyParticleForces");
        this.compute.Dispatch(applyParticleForces, Mathf.CeilToInt(this.PointCount / 16f), 1, 1);
        Profiler.EndSample();

        // Update our local representation of the particles.
        this.UpdateParticles();

        // Run the kernal to find the new grid points.
        int findGridCells = this.compute.FindKernel("findGridCells");
        this.compute.Dispatch(findGridCells, Mathf.CeilToInt(this.PointCount / 16f), 1, 1);

        // Update them on the CPU.
        this.UpdateGridPoints();

        Profiler.EndSample();
    }

    void OnDestroy ( ) {
        // We need to explicitly release the buffers, otherwise Unity will not be satisfied.
        this.particleBuffer.Release();
        this.gridPlacementBuffer.Release();
        this.gridContainedBuffer.Release();
    }
}