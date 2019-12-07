using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class TectonicPlate : MonoBehaviour {

    private Planet parentPlanet;

    private List<TectonicTriangle> triangles;

    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    public float age;

    public bool CanGrow;

    private List<TectonicTriangle> edgeTriangles;

    public void Initialize (Planet _parent, TectonicTriangle _rootTriangle) {
        this.parentPlanet = _parent;
        this.triangles = new List<TectonicTriangle>();
        this.edgeTriangles = new List<TectonicTriangle>();
        this.triangles.Add(_rootTriangle);
        this.edgeTriangles.Add(_rootTriangle);
        _rootTriangle.SetParentPlate(this);

        this.age = 0;
        this.CanGrow = true;

        this.meshFilter = this.GetComponent<MeshFilter>();
        this.meshRenderer = this.GetComponent<MeshRenderer>();
        this.mesh = new Mesh();
        this.meshFilter.mesh = this.mesh;

        Material tempMaterial = new Material(Shader.Find("Diffuse"));
        tempMaterial.SetColor("_Color", new Color(Random.Range(100, 255) / 255f, Random.Range(100, 255) / 255f, Random.Range(100, 255) / 255f));
        this.meshRenderer.material = tempMaterial;
    }

    public bool GrowPlate ( ) {
        // Set up a queue of all the triangles to go through.
        List<TectonicTriangle> queue = new List<TectonicTriangle>(this.edgeTriangles);

        // Go through each edge triangle to see if we can grow to it.
        while(queue.Count > 0) {
            int nextTriangle = Random.Range(0, queue.Count);

            // Get the neighbors of the triangle to grow from.
            List<TectonicTriangle> trianglesToCheck = new List<TectonicTriangle>(queue[nextTriangle].GetNeighborTriangles());

            // Go through each neighbor triangle and see if we can grow.
            while(trianglesToCheck.Count > 0) {
                int randomNeighbor = Random.Range(0, trianglesToCheck.Count);

                // If the neighbor isn't set to a plate yet, add it.
                if (trianglesToCheck[randomNeighbor].parentPlate == null) {
                    trianglesToCheck[randomNeighbor].SetParentPlate(this);

                    // Add the newly added triangle to our lists.
                    this.edgeTriangles.Add(trianglesToCheck[randomNeighbor]);
                    this.triangles.Add(trianglesToCheck[randomNeighbor]);

                    // Check if the root triangle is now completely surrounded.
                    bool surrounded = true;
                    for (int i = 0; i < 2; i++) {
                        if (trianglesToCheck[((i + randomNeighbor + 1) % trianglesToCheck.Count)].parentPlate == null) {
                            surrounded = false;
                        }
                    }

                    // If it is, remove it from future growth.
                    if (surrounded) {
                        this.edgeTriangles.Remove(queue[nextTriangle]);
                    }

                    return true;
                }
                else {
                    trianglesToCheck.RemoveAt(randomNeighbor);
                }
            }

            // If we can't grow, remove it from the list of edge triangles (it's no longer on an incomplete edge) and the queue.
            this.edgeTriangles.Remove(queue[nextTriangle]);
            queue.RemoveAt(nextTriangle);
        }

        this.CanGrow = false;
        return false;
    }

    public void UpdateMesh () {
        // Create a dictionary for storing the vector position of the points based on the planet index.
        Dictionary<int, Vector3> meshPoints = new Dictionary<int, Vector3>();
        // Create a dictionary for storing the index of the vector in the meshPoints based on the planet index.
        Dictionary<int, int> meshIndices = new Dictionary<int, int>();
        // How many unique points we've added.
        int values = 0;

        Profiler.BeginSample("Plate: Sample Triangle Positions/Indices");
        // Go through all of the triangles and add their positions.
        //  Add the index to a list so we can get accurate indices later.
        for (int i = 0; i < this.triangles.Count; i++) {
            int[] array = this.triangles[i].GetPoints();
            for (int j = 0; j < 3; j++) {
                if (!meshPoints.ContainsKey(array[j])) {
                    meshPoints[array[j]] = this.parentPlanet.TectonicPoints[array[j]].Position;
                    meshIndices[array[j]] = values;
                    values++;
                }
            }
        }
        // Send all the vector positions to the mesh.   
        List<Vector3> points = new List<Vector3>(meshPoints.Values);
        Profiler.EndSample();

        Profiler.BeginSample("Plate: Get Indices");
        List<int> indices = new List<int>();

        for (int i = 0; i < this.triangles.Count; i++) {
            int[] array = this.triangles[i].GetPoints();
            for (int j = 0; j < 3; j++) {
                indices.Add(meshIndices[array[j]]);
            }
        }
        Profiler.EndSample();

        this.mesh.SetVertices(points);
        this.mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
    }
}

//  Plate:
//      Variables:
//          Triangles (List of Triangles): the triangles that make up the plate.
//          Age (float): how old the plate is.
//      Methods:
//          GrowPlate () {}: grows the plate one outwards to an unclaimed triangle.
//